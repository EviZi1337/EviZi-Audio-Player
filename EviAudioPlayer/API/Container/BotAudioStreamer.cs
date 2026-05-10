using Exiled.API.Features;
using MEC;
using RelativePositioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VoiceChat;
using VoiceChat.Codec;
using VoiceChat.Codec.Enums;
using VoiceChat.Networking;
using VoiceChat.Playbacks;

namespace EviAudio.API.Container;

public sealed class BotAudioStreamer : MonoBehaviour
{
    private readonly Queue<string> _queue = new();
    private readonly object _queueLock = new();
    private float[] _currentSamples = Array.Empty<float>();
    private readonly float[] _pcmBuffer = new float[AudioClipPlayback.PacketSize];
    private readonly byte[] _encodedBuffer = new byte[512];
    private int _sampleOffset;
    private double _lastSendTime = -1;
    private double _lastPositionSendTime = -1;
    private volatile PendingLoad _pendingLoad;
    private volatile int _loadVersion;
    private OpusEncoder _encoder;
    private CoroutineHandle _fadeHandle;
    private float _baseDuckVolume;
    private bool _isDucked;
    private float _pitchShift;

    public ReferenceHub Hub { get; private set; }
    public VoiceChatChannel Channel { get; set; } = VoiceChatChannel.Intercom;
    public float Volume { get; set; } = 100f;
    public bool Loop { get; set; }
    public bool ContinueAfterTrack { get; set; }
    public bool Shuffle { get; set; }
    public bool IsPaused { get; set; }
    public float PitchShift { get => _pitchShift; set => _pitchShift = value; }
    public string CurrentPlay { get; private set; } = string.Empty;
    public bool IsPlaying => CurrentPlay.Length > 0;
    public HashSet<int> BroadcastTo { get; } = new();
    public event Action<string> OnTrackFinished;

    public void Init(ReferenceHub hub)
    {
        Hub = hub;
        _encoder = new OpusEncoder(OpusApplicationType.Voip);
    }

    public void Enqueue(string path, int position = -1)
    {
        lock (_queueLock)
        {
            if (position < 0 || position >= _queue.Count)
            {
                _queue.Enqueue(path);
                return;
            }
            var list = new List<string>(_queue);
            list.Insert(position, path);
            _queue.Clear();
            foreach (var p in list) _queue.Enqueue(p);
        }
    }

    public void EnqueueFolder(string folderPath, bool shuffle = false)
    {
        if (!Directory.Exists(folderPath))
        {
            Log.Warn($"EnqueueFolder: folder not found '{folderPath}'.");
            return;
        }

        var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".ogg", ".wav", ".mp3", ".flac", ".aac", ".opus", ".m4a" };

        var files = Directory.GetFiles(folderPath)
            .Where(f => supported.Contains(Path.GetExtension(f)))
            .ToList();

        if (shuffle)
        {
            for (int i = files.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (files[i], files[j]) = (files[j], files[i]);
            }
        }
        else
        {
            files.Sort(StringComparer.OrdinalIgnoreCase);
        }

        lock (_queueLock)
            foreach (var f in files)
                _queue.Enqueue(f);

        Log.Debug($"EnqueueFolder: queued {files.Count} file(s) from '{folderPath}'.");
    }

    public void EnqueueM3U(string m3uPath)
    {
        if (!File.Exists(m3uPath))
        {
            Log.Warn($"EnqueueM3U: file not found '{m3uPath}'.");
            return;
        }

        string dir = Path.GetDirectoryName(m3uPath) ?? string.Empty;
        int count = 0;

        lock (_queueLock)
        {
            foreach (var raw in File.ReadAllLines(m3uPath))
            {
                string line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;

                string resolved = Path.IsPathRooted(line) ? line : Path.Combine(dir, line);
                if (File.Exists(resolved))
                {
                    _queue.Enqueue(resolved);
                    count++;
                }
                else
                {
                    Log.Warn($"EnqueueM3U: track not found '{resolved}'.");
                }
            }
        }

        Log.Debug($"EnqueueM3U: queued {count} track(s) from '{m3uPath}'.");
    }

    public void Play()
    {
        bool hasItems;
        lock (_queueLock) hasItems = _queue.Count > 0;
        if (hasItems) LoadNextAsync();
    }

    public void Stop(bool clearQueue)
    {
        Interlocked.Increment(ref _loadVersion);
        _pendingLoad = null;
        CurrentPlay = string.Empty;
        _currentSamples = Array.Empty<float>();
        _sampleOffset = 0;
        _lastSendTime = -1;
        _lastPositionSendTime = -1;
        IsPaused = false;
        _isDucked = false;
        if (clearQueue)
            lock (_queueLock) _queue.Clear();
    }

    public void Skip()
    {
        bool hasNext;
        lock (_queueLock) hasNext = _queue.Count > 0;

        string finished = CurrentPlay;
        _currentSamples = Array.Empty<float>();
        _sampleOffset = 0;
        _lastSendTime = -1;

        if (hasNext)
            LoadNextAsync();
        else
            CurrentPlay = string.Empty;

        if (!string.IsNullOrEmpty(finished))
            OnTrackFinished?.Invoke(finished);
    }

    public List<string> GetQueue()
    {
        lock (_queueLock)
            return new List<string>(_queue);
    }

    public void FadeTo(float targetVolume, float duration)
    {
        if (_fadeHandle.IsRunning)
            Timing.KillCoroutines(_fadeHandle);
        _fadeHandle = Timing.RunCoroutine(FadeCoroutine(Volume, targetVolume, duration));
    }

    internal void Duck(float duckVolume, float fadeTime)
    {
        if (_isDucked) return;
        _isDucked = true;
        _baseDuckVolume = Volume;
        FadeTo(duckVolume, fadeTime);
    }

    internal void Unduck(float fadeTime)
    {
        if (!_isDucked) return;
        _isDucked = false;
        FadeTo(_baseDuckVolume, fadeTime);
    }

    private IEnumerator<float> FadeCoroutine(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return Timing.WaitForOneFrame;
        }
        Volume = to;
    }

    private void Update()
    {
        var pending = _pendingLoad;
        if (pending != null)
        {
            _pendingLoad = null;
            _currentSamples = pending.Samples;
            CurrentPlay = pending.Path;
            _sampleOffset = 0;
            _lastSendTime = -1;
            _lastPositionSendTime = -1;
        }

        if (Hub == null || IsPaused || CurrentPlay.Length == 0) return;

        double now = (double)Time.unscaledTime;
        double interval = (double)AudioClipPlayback.PacketSize / AudioClipPlayback.SamplingRate;
        if (_lastSendTime < 0)
            _lastSendTime = now;

        while (now - _lastSendTime >= interval)
        {
            if (!SendPacket()) break;
            _lastSendTime += interval;
        }
    }

    private bool SendPacket()
    {
        if (_sampleOffset >= _currentSamples.Length)
        {
            if (Loop)
            {
                _sampleOffset = 0;
            }
            else
            {
                bool hasNext;
                lock (_queueLock) hasNext = ContinueAfterTrack && _queue.Count > 0;
                string finished = CurrentPlay;
                if (hasNext)
                {
                    LoadNextAsync();
                    OnTrackFinished?.Invoke(finished);
                }
                else
                {
                    CurrentPlay = string.Empty;
                    _currentSamples = Array.Empty<float>();
                    _sampleOffset = 0;
                    _lastSendTime = -1;
                    _lastPositionSendTime = -1;
                    IsPaused = false;
                    OnTrackFinished?.Invoke(finished);
                    return false;
                }
            }
        }

        int count = Math.Min(AudioClipPlayback.PacketSize, _currentSamples.Length - _sampleOffset);
        Array.Copy(_currentSamples, _sampleOffset, _pcmBuffer, 0, count);
        if (count < AudioClipPlayback.PacketSize)
            Array.Clear(_pcmBuffer, count, AudioClipPlayback.PacketSize - count);

        float vol = Math.Clamp(Volume * 0.01f, 0f, 1f);
        if (vol != 1f)
            for (int i = 0; i < AudioClipPlayback.PacketSize; i++)
                _pcmBuffer[i] *= vol;

        _sampleOffset += count;
        int encodedLen = _encoder.Encode(_pcmBuffer, _encodedBuffer);
        if (encodedLen <= 0) return true;

        var msg = new VoiceMessage(Hub, Channel, _encodedBuffer, encodedLen, false);
        bool isRadio = Channel == VoiceChatChannel.Radio;
        double now = (double)Time.unscaledTime;
        bool sendPos = isRadio && (_lastPositionSendTime < 0 || now - _lastPositionSendTime >= 0.5);
        bool hasFilter = BroadcastTo.Count > 0;

        foreach (ReferenceHub hub in ReferenceHub.AllHubs)
        {
            if (hub.connectionToClient == null || hub == Hub) continue;
            if (hasFilter && !BroadcastTo.Contains(hub.PlayerId)) continue;

            if (sendPos)
            {
                var posMsg = new PersonalRadioPlayback.TransmitterPositionMessage
                {
                    Transmitter = new RecyclablePlayerId(Hub),
                    WaypointId = new RelativePosition(hub.transform.position).WaypointId
                };
                hub.connectionToClient.Send(posMsg);
            }
            hub.connectionToClient.Send(msg);
        }

        if (sendPos)
            _lastPositionSendTime = now;

        return true;
    }

    private void LoadNextAsync()
    {
        string path;
        lock (_queueLock)
        {
            if (_queue.Count == 0) { CurrentPlay = string.Empty; return; }

            if (Shuffle && _queue.Count > 1)
            {
                var list = new List<string>(_queue);
                int idx = UnityEngine.Random.Range(0, list.Count);
                path = list[idx];
                list.RemoveAt(idx);
                _queue.Clear();
                foreach (var p in list) _queue.Enqueue(p);
            }
            else
            {
                path = _queue.Dequeue();
            }
        }

        CurrentPlay = path;
        _currentSamples = Array.Empty<float>();
        _sampleOffset = 0;
        int version = _loadVersion;
        float pitch = _pitchShift;

        Task.Run(() =>
        {
            float[] samples;
            try
            {
                var data = PcmDecoder.DecodeFile(path, path, pitch);
                samples = data.Samples;
            }
            catch (Exception ex)
            {
                Log.Error($"BotAudioStreamer: failed to decode '{path}': {ex.Message}");
                samples = Array.Empty<float>();
            }
            if (_loadVersion == version)
                _pendingLoad = new PendingLoad(path, samples);
        });
    }

    private void OnDestroy()
    {
        Interlocked.Increment(ref _loadVersion);
        _pendingLoad = null;
        _encoder?.Dispose();
        _encoder = null;

        if (_fadeHandle.IsRunning)
            Timing.KillCoroutines(_fadeHandle);
    }

    private sealed class PendingLoad
    {
        public readonly string Path;
        public readonly float[] Samples;
        public PendingLoad(string path, float[] samples) { Path = path; Samples = samples; }
    }
}
