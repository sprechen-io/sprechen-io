namespace Sprechen.Service.Abstractions;

/// <summary>
/// Audioblock der Sprachsynthese mit Format und optional angegebener Abtastrate.
/// </summary>
public readonly record struct AudioSynthesisChunk
{
    private const int DEFAULT_SAMPLE_RATE_HZ = 0;

    public AudioSynthesisChunk(byte[] data, AudioMimeType mime)
        : this(data, mime, DEFAULT_SAMPLE_RATE_HZ)
    {
    }

    public AudioSynthesisChunk(byte[] data, AudioMimeType mime, int sampleRateHz)
    {
        ArgumentNullException.ThrowIfNull(data);

        Data = data;
        Mime = mime;
        SampleRateHz = sampleRateHz;
    }

    public byte[] Data { get; }
    public AudioMimeType Mime { get; }
    public int SampleRateHz { get; }
}
