using Exiled.API.Features;
using NVorbis;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EviAudio.API;

public static class PcmDecoder
{
    public const int TargetSampleRate = 48000;
    public const int TargetChannels = 1;

    public static Task<AudioClipData> DecodeFileAsync(string path, string name)
        => Task.Run(() => DecodeFile(path, name));

    public static AudioClipData DecodeFile(string path, string name, float pitchShift = 0f)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();

        var (samples, sampleRate, channels) = ext switch
        {
            ".ogg" => DecodeOgg(path),
            ".wav" => DecodeWav(path),
            _ => DecodeFfmpeg(path),
        };

        if (channels != TargetChannels || sampleRate != TargetSampleRate)
            samples = Resample(samples, sampleRate, channels, TargetSampleRate, TargetChannels);

        if (Math.Abs(pitchShift) >= 0.01f)
            samples = PitchShifter.Shift(samples, pitchShift);

        return new AudioClipData(name, TargetSampleRate, TargetChannels, samples);
    }

    private static (float[] samples, int rate, int channels) DecodeOgg(string path)
    {
        using var reader = new VorbisReader(path);
        int rate = reader.SampleRate;
        int ch = reader.Channels;
        var buf = new float[reader.TotalSamples * ch];
        reader.ReadSamples(buf, 0, buf.Length);
        return (buf, rate, ch);
    }

    private static (float[] samples, int rate, int channels) DecodeWav(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs, Encoding.ASCII, leaveOpen: true);

        Span<byte> tag = stackalloc byte[4];
        fs.Read(tag);
        if (tag[0] != 'R' || tag[1] != 'I' || tag[2] != 'F' || tag[3] != 'F')
            throw new InvalidDataException("Not a RIFF WAV file.");
        br.ReadInt32();
        fs.Read(tag);
        if (tag[0] != 'W' || tag[1] != 'A' || tag[2] != 'V' || tag[3] != 'E')
            throw new InvalidDataException("Not a WAVE file.");

        int audioFormat = 0, ch = 0, rate = 0, bitsPerSample = 0;
        long dataStart = 0;
        int dataLength = 0;

        Span<byte> chunkId = stackalloc byte[4];
        while (fs.Position < fs.Length - 8)
        {
            fs.Read(chunkId);
            int chunkSize = br.ReadInt32();
            long chunkEnd = fs.Position + chunkSize;

            if (chunkId[0] == 'f' && chunkId[1] == 'm' && chunkId[2] == 't' && chunkId[3] == ' ')
            {
                audioFormat = br.ReadInt16();
                ch = br.ReadInt16();
                rate = br.ReadInt32();
                br.ReadInt32();
                br.ReadInt16();
                bitsPerSample = br.ReadInt16();
                if (fs.Position < chunkEnd)
                    fs.Seek(chunkEnd - fs.Position, SeekOrigin.Current);
            }
            else if (chunkId[0] == 'd' && chunkId[1] == 'a' && chunkId[2] == 't' && chunkId[3] == 'a')
            {
                dataStart = fs.Position;
                dataLength = chunkSize;
                break;
            }
            else
            {
                fs.Seek(chunkSize, SeekOrigin.Current);
            }
        }

        if (dataStart == 0) throw new InvalidDataException("WAV: no data chunk found.");
        if (audioFormat != 1 && audioFormat != 3)
            throw new NotSupportedException($"WAV: format {audioFormat} not supported (need PCM=1 or IEEE_FLOAT=3).");

        fs.Seek(dataStart, SeekOrigin.Begin);

        byte[] raw = ArrayPool<byte>.Shared.Rent(dataLength);
        try
        {
            int bytesRead = 0;
            while (bytesRead < dataLength)
            {
                int n = fs.Read(raw, bytesRead, dataLength - bytesRead);
                if (n == 0) break;
                bytesRead += n;
            }

            int bytesPerSample = bitsPerSample / 8;
            int sampleCount = bytesRead / bytesPerSample;
            var samples = new float[sampleCount];
            ConvertToFloat(raw, samples, sampleCount, audioFormat, bitsPerSample);
            return (samples, rate, ch);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(raw);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ConvertToFloat(byte[] raw, float[] samples, int count, int audioFormat, int bitsPerSample)
    {
        if (audioFormat == 3)
        {
            Buffer.BlockCopy(raw, 0, samples, 0, count * 4);
            return;
        }

        switch (bitsPerSample)
        {
            case 8:
                for (int i = 0; i < count; i++)
                    samples[i] = (raw[i] - 128) * (1f / 128f);
                break;

            case 16:
                var shorts = MemoryMarshal.Cast<byte, short>(raw.AsSpan(0, count * 2));
                const float inv16 = 1f / 32768f;
                for (int i = 0; i < count; i++)
                    samples[i] = shorts[i] * inv16;
                break;

            case 24:
                for (int i = 0; i < count; i++)
                {
                    int o = i * 3;
                    samples[i] = (raw[o] | (raw[o + 1] << 8) | ((sbyte)raw[o + 2] << 16)) * (1f / 8388608f);
                }
                break;

            case 32:
                var ints = MemoryMarshal.Cast<byte, int>(raw.AsSpan(0, count * 4));
                const float inv32 = 1f / 2147483648f;
                for (int i = 0; i < count; i++)
                    samples[i] = ints[i] * inv32;
                break;
        }
    }

    private static (float[] samples, int rate, int channels) DecodeFfmpeg(string path)
    {
        string ffmpegPath = FindFfmpeg() ?? throw new FileNotFoundException(
            "FFmpeg not found. Place ffmpeg in EXILED/Plugins/EviAudio/ffmpeg or in PATH.");

        string tmpPath = Path.Combine(Path.GetTempPath(), $"eviaud_{Guid.NewGuid():N}.wav");
        try
        {
            var psi = new ProcessStartInfo(ffmpegPath,
                $"-y -i \"{path}\" -ac 1 -ar {TargetSampleRate} -f wav \"{tmpPath}\"")
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var proc = Process.Start(psi))
            {
                if (!proc!.WaitForExit(30_000))
                {
                    try { proc.Kill(); } catch { }
                    throw new InvalidOperationException("FFmpeg timed out after 30 seconds.");
                }

                if (proc.ExitCode != 0)
                {
                    string err = proc.StandardError.ReadToEnd();
                    throw new InvalidOperationException($"FFmpeg exited with code {proc.ExitCode}: {err}");
                }
            }

            return DecodeWav(tmpPath);
        }
        finally
        {
            if (File.Exists(tmpPath))
                try { File.Delete(tmpPath); } catch { }
        }
    }

    private static string FindFfmpeg()
    {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        string exe = isWindows ? "ffmpeg.exe" : "ffmpeg";

        if (Plugin.Instance != null)
        {
            string pluginBin = Path.Combine(Plugin.Instance.PluginFolder, "ffmpeg", exe);
            if (File.Exists(pluginBin)) return pluginBin;
        }

        string serverBin = Path.Combine(AppContext.BaseDirectory, "ffmpeg", exe);
        if (File.Exists(serverBin)) return serverBin;

        try
        {
            var psi = new ProcessStartInfo(isWindows ? "where" : "which", "ffmpeg")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            string result = p?.StandardOutput.ReadLine()?.Trim();
            p?.WaitForExit(3_000);
            if (!string.IsNullOrEmpty(result) && File.Exists(result))
                return result;
        }
        catch { }

        return null;
    }

    private static float[] Resample(float[] input, int fromRate, int fromChannels, int toRate, int toChannels)
    {
        float[] mono = input;
        float[] monoRented = null;
        int monoLen = input.Length / fromChannels;

        if (fromChannels > 1)
        {
            monoRented = ArrayPool<float>.Shared.Rent(monoLen);
            mono = monoRented;

            float invCh = 1f / fromChannels;
            for (int i = 0; i < monoLen; i++)
            {
                float sum = 0f;
                int baseIdx = i * fromChannels;
                for (int c = 0; c < fromChannels; c++)
                    sum += input[baseIdx + c];
                mono[i] = sum * invCh;
            }
        }

        try
        {
            if (fromRate == toRate)
            {
                if (monoRented == null) return mono;
                var exact = new float[monoLen];
                Array.Copy(mono, exact, monoLen);
                return exact;
            }

            double ratio = (double)fromRate / toRate;
            int outLen = (int)(monoLen / ratio);
            var output = new float[outLen];

            for (int i = 0; i < outLen; i++)
            {
                double srcIdx = i * ratio;
                int lo = (int)srcIdx;
                int hi = lo + 1 < monoLen ? lo + 1 : monoLen - 1;
                double t = srcIdx - lo;
                output[i] = (float)(mono[lo] * (1.0 - t) + mono[hi] * t);
            }

            return output;
        }
        finally
        {
            if (monoRented != null)
                ArrayPool<float>.Shared.Return(monoRented);
        }
    }
}
