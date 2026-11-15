namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verarbeitet gepufferte Audioblöcke eines kontinuierlichen Segmentversuchs,
/// damit dessen Ergebnis auch ohne einen weiteren neu eintreffenden Block verfügbar wird.
/// </summary>
internal sealed class ContinuousSegmentCursorBacklogDrainer
{
    private readonly ActiveSegmentSlotStore _slotStore;
    private readonly SpeechSampleRing _sampleRing;
    private readonly IBrowserDebugConsole _browserDebugConsole;

    internal ContinuousSegmentCursorBacklogDrainer(
        ActiveSegmentSlotStore slotStore,
        SpeechSampleRing sampleRing,
        IBrowserDebugConsole browserDebugConsole)
    {
        _slotStore = slotStore ?? throw new ArgumentNullException(nameof(slotStore));
        _sampleRing = sampleRing ?? throw new ArgumentNullException(nameof(sampleRing));
        _browserDebugConsole = browserDebugConsole ?? throw new ArgumentNullException(nameof(browserDebugConsole));
    }

    internal async Task DrainAsync(
        ActiveSegmentSlot activeSlot,
        CancellationToken cancellationToken)
    {
        var speechStartedDispatched = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            ActiveSegmentBacklogReadResult result;

            try
            {
                result = _slotStore.ReadFrom(activeSlot);
            }
            catch (RingCursorStaleException)
            {
                _slotStore.ResetToLiveEdge(_sampleRing);
                return;
            }

            if (!result.IsActive)
            {
                return;
            }

            if (result.Update.SpeechStarted && !speechStartedDispatched)
            {
                speechStartedDispatched = true;
                _browserDebugConsole.WriteOptional("speech-capture", "cursor backlog speech onset");
            }

            if (result.Update.CompletionException is not null)
            {
                _slotStore.Clear(activeSlot);
                activeSlot.TryFail(result.Update.CompletionException);
                return;
            }

            if (result.Update.CompletedUtterance is not null)
            {
                _slotStore.Clear(activeSlot);
                activeSlot.TryComplete(result.Update.CompletedUtterance);
                return;
            }

            if (!result.Advanced)
            {
                return;
            }
        }
    }
}
