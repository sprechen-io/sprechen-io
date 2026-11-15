using System.Diagnostics;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Berechnet Verzögerungen für den Versand von 16-Bit-Mono-PCM aus Bytezahl und Abtastrate.
/// Bereits verstrichene Zeit wird auf die berechnete Gesamtdauer angerechnet.
/// </summary>
internal sealed class PcmAudioStreamPacer
{
    private const int PcmBitsPerSample = 16;
    private const int PcmBytesPerSample = PcmBitsPerSample / 8;
    private const int PcmChannels = 1;

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private TimeSpan _expectedElapsed = TimeSpan.Zero;

    public async ValueTask PaceAsync(int bytesSent, int sampleRateHz, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (bytesSent <= 0)
        {
            return;
        }

        if (sampleRateHz <= 0)
        {
            return;
        }

        var bytesPerSecond = (double)sampleRateHz * PcmChannels * PcmBytesPerSample;
        if (bytesPerSecond <= 0)
        {
            return;
        }

        var seconds = bytesSent / bytesPerSecond;
        if (seconds <= 0)
        {
            return;
        }

        _expectedElapsed += TimeSpan.FromSeconds(seconds);
        var delay = _expectedElapsed - _stopwatch.Elapsed;
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        await Task.Delay(delay, ct).ConfigureAwait(false);
    }
}
