using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verarbeitet neue Audioblöcke für den aktiven Aufnahmeversuch und meldet Sprachbeginn,
/// damit Unterbrechung der Ausgabe und Abschluss aus demselben Verarbeitungsergebnis folgen.
/// </summary>
internal sealed class ActiveCaptureChunkFlow
{
    private readonly ActiveCaptureSlotStore _slotStore;
    private readonly IUserSpeechBargeInHandler _bargeInHandler;
    private readonly ILogger _logger;
    private readonly IBrowserDebugConsole _browserDebugConsole;
    private readonly CaptureChunkLivenessGuard _chunkLiveness;

    internal ActiveCaptureChunkFlow(
        ActiveCaptureSlotStore slotStore,
        IUserSpeechBargeInHandler bargeInHandler,
        ILogger logger,
        IBrowserDebugConsole browserDebugConsole,
        CaptureChunkLivenessGuard chunkLiveness)
    {
        _slotStore = slotStore ?? throw new ArgumentNullException(nameof(slotStore));
        _bargeInHandler = bargeInHandler ?? throw new ArgumentNullException(nameof(bargeInHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _browserDebugConsole = browserDebugConsole ?? throw new ArgumentNullException(nameof(browserDebugConsole));
        _chunkLiveness = chunkLiveness ?? throw new ArgumentNullException(nameof(chunkLiveness));
    }

    internal async Task ProcessChunkAsync(byte[] chunk, CancellationToken cancellationToken)
    {
        _chunkLiveness.ObserveChunk();
        var result = _slotStore.ProcessChunk(chunk);

        if (result.SpeechStarted)
        {
            _browserDebugConsole.WriteOptional("speech-capture", result.ModeBehavior.SpeechOnsetDescription);

            if (result.ModeBehavior.BargeInEnabled)
            {
                await NotifyBargeInAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        if (result.CompletedSlot is null)
        {
            return;
        }

        if (result.CompletionException is not null)
        {
            result.CompletedSlot.TryFail(result.CompletionException);
            return;
        }

        if (result.CompletedUtterance is not null)
        {
            result.CompletedSlot.TryComplete(result.CompletedUtterance);
        }
    }

    private async ValueTask NotifyBargeInAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _bargeInHandler.OnSpeechCaptureStartingAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Barge-in handler failed.");
        }
    }
}
