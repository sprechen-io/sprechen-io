namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Liest den gepufferten Audiorückstand eines Aufnahmeversuchs bis zur aktuellen Position,
/// damit bereits eingetroffene Sprache vor dem Warten auf neue Blöcke ausgewertet wird.
/// </summary>
internal sealed class ActiveCaptureCursorBacklogDrainer
{
    private readonly ActiveCaptureSlotStore _slotStore;
    private readonly IBrowserDebugConsole _browserDebugConsole;

    internal ActiveCaptureCursorBacklogDrainer(
        ActiveCaptureSlotStore slotStore,
        IBrowserDebugConsole browserDebugConsole)
    {
        _slotStore = slotStore ?? throw new ArgumentNullException(nameof(slotStore));
        _browserDebugConsole = browserDebugConsole ?? throw new ArgumentNullException(nameof(browserDebugConsole));
    }

    internal async Task<int> DrainAsync(
        ActiveCaptureSlot activeSlot,
        CancellationToken cancellationToken)
    {
        var speechStartedDispatched = false;
        var lookbackBytes = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            ActiveCaptureBacklogReadResult result;

            try
            {
                result = _slotStore.ReadFrom(activeSlot);
            }
            catch (RingCursorStaleException)
            {
                _slotStore.ResetToLiveEdge();
                return lookbackBytes;
            }

            if (!result.IsActive)
            {
                return lookbackBytes;
            }

            lookbackBytes = lookbackBytes > int.MaxValue - result.LookbackBytes
                ? int.MaxValue
                : lookbackBytes + result.LookbackBytes;

            if (result.Update.SpeechStarted && !speechStartedDispatched)
            {
                speechStartedDispatched = true;
                _browserDebugConsole.WriteOptional("speech-capture", "cursor backlog speech onset");
            }

            if (result.Update.CompletionException is not null)
            {
                _slotStore.Clear(activeSlot);
                activeSlot.TryFail(result.Update.CompletionException);
                return lookbackBytes;
            }

            if (result.Update.CompletedUtterance is not null)
            {
                _slotStore.Clear(activeSlot);
                activeSlot.TryComplete(result.Update.CompletedUtterance);
                return lookbackBytes;
            }

            if (!result.Advanced)
            {
                return lookbackBytes;
            }
        }

        return lookbackBytes;
    }
}
