namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Bereitet Audiodaten für Deepgram vor. Roh-PCM erhält einen WAV-Kopf.
/// Seine Dauer wird unter Annahme von 16 kHz, Mono und 16 Bit pro Sample berechnet.
/// </summary>
internal static class DeepgramAudioPayload
{
    private const int PcmSampleRateHz = 16_000;
    private const short PcmChannels = 1;
    private const short PcmBitsPerSample = 16;
    private const int MillisecondsPerSecond = 1_000;
    private const int BitsPerByte = 8;

    public static async Task<byte[]> ReadAllAsync(IAsyncEnumerable<byte[]> chunks, CancellationToken ct)
    {
        return await AudioPayloadReader.ReadAllAsync(chunks, ct).ConfigureAwait(false);
    }

    public static byte[] Prepare(byte[] audio, AudioMimeType mime) => mime switch
    {
        AudioMimeType.Wav => audio,
        AudioMimeType.Mp3 => audio,
        AudioMimeType.Flac => audio,
        AudioMimeType.OggOpus => audio,
        AudioMimeType.WebmOpus => audio,
        AudioMimeType.Pcm => WavEncoder.WrapPcmAsWav(audio, PcmSampleRateHz, PcmChannels, PcmBitsPerSample),
        AudioMimeType.Unspecified => throw new InvalidOperationException("Deepgram requires an explicit audio mime type."),
        _ => throw new InvalidOperationException($"Deepgram does not support audio mime '{mime}'.")
    };

    public static int GetPcmDurationMs(int audioByteCount)
    {
        if (audioByteCount <= 0)
        {
            return 0;
        }

        var bytesPerSecond = PcmSampleRateHz * PcmChannels * PcmBitsPerSample / BitsPerByte;
        return (int)((long)audioByteCount * MillisecondsPerSecond / bytesPerSecond);
    }
}
