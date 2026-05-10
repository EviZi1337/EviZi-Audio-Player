namespace EviAudio.API;

public sealed class AudioClipData
{
    public AudioClipData(string name, int sampleRate, int channels, float[] samples)
    {
        Name = name;
        SampleRate = sampleRate;
        Channels = channels;
        Samples = samples;
    }

    public string Name { get; }
    public int SampleRate { get; }
    public int Channels { get; }
    public float[] Samples { get; }
}
