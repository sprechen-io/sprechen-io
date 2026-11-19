using MicroSharp.Processing;

namespace Sprechen.Service.Abstractions;

/// <summary>Wraps complete PCM16LE frames in a WAV file.</summary>
public static class WavEncoder
{
    public static byte[] WrapPcmAsWav(byte[] pcm, int sampleRate, short channels, short bitsPerSample)
    {
        ArgumentNullException.ThrowIfNull(pcm);
        if (bitsPerSample != 16)
        {
            throw new ArgumentOutOfRangeException(nameof(bitsPerSample), "The PCM sample width must be 16 bits.");
        }

        return Pcm16LeWavEncoder.Encode(pcm, sampleRate, channels);
    }
}
