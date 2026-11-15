using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Schreibt Mikrofon-Chunks in den Sample-Ring und leitet sie an die Verarbeitung weiter.
/// Ein reguläres Streamende wird gemeldet, damit wartende Aufnahmen vom Ende erfahren.
/// </summary>
internal sealed class SpeechCaptureSessionPump
{
    private readonly SpeechSampleRing _sampleRing;
    private readonly ILogger _logger;

    internal SpeechCaptureSessionPump(SpeechSampleRing sampleRing, ILogger logger)
    {
        _sampleRing = sampleRing ?? throw new ArgumentNullException(nameof(sampleRing));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal async Task RunAsync(
        IMicrophoneRecording recording,
        Func<byte[], CancellationToken, Task> processChunkAsync,
        Action<Exception> onSessionFailed,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(recording);
        ArgumentNullException.ThrowIfNull(processChunkAsync);
        ArgumentNullException.ThrowIfNull(onSessionFailed);

        try
        {
            await foreach (var chunk in recording.Chunks.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (chunk.Length == 0)
                {
                    continue;
                }

                _sampleRing.WriteSamples(chunk);
                await processChunkAsync(chunk, cancellationToken).ConfigureAwait(false);
            }

            // Auch ein Streamende ohne Ausnahme muss wartende Aufnahmen benachrichtigen.
            onSessionFailed(new SpeechCaptureSessionEndedException());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Bewusster Stopp: der Abbruch ist das Ergebnis und kein Fehlschlag.
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Microphone session failed.");
            onSessionFailed(ex);
        }
    }
}
