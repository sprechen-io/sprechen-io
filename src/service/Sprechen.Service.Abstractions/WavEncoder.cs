using System.Text;

namespace Sprechen.Service.Abstractions;

/// <summary>
/// Ergänzt PCM-Daten um einen 44 Byte langen RIFF/WAV-Kopf mit den angegebenen Audioeigenschaften.
/// </summary>
public static class WavEncoder
{
    public static byte[] WrapPcmAsWav(byte[] pcm, int sampleRate, short channels, short bitsPerSample)
    {
        using var ms = new MemoryStream(44 + pcm.Length);
        using var writer = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + pcm.Length);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        writer.Write(byteRate);
        var blockAlign = (short)(channels * bitsPerSample / 8);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);

        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(pcm.Length);
        writer.Write(pcm);

        writer.Flush();
        return ms.ToArray();
    }
}
