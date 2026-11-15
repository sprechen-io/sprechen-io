namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Leitet Sprachbeginn und Abschluss eines kontinuierlich erfassten Segments weiter,
/// damit das Ergebnis der Blockverarbeitung den wartenden Segmentaufruf erreicht.
/// </summary>
internal sealed class ActiveSegmentChunkFlow
{
    private readonly ActiveSegmentSlotStore _slotStore;
    private readonly IBrowserDebugConsole _browserDebugConsole;

    internal ActiveSegmentChunkFlow(
        ActiveSegmentSlotStore slotStore,
        IBrowserDebugConsole browserDebugConsole)
    {
        _slotStore = slotStore ?? throw new ArgumentNullException(nameof(slotStore));
        _browserDebugConsole = browserDebugConsole ?? throw new ArgumentNullException(nameof(browserDebugConsole));
    }

    internal Task ProcessChunkAsync(byte[] chunk, CancellationToken cancellationToken)
    {
        var result = _slotStore.ProcessChunk(chunk);

        if (result.SpeechStarted)
        {
            _browserDebugConsole.WriteOptional("speech-capture", "speech onset confirmed");
        }

        if (result.CompletedSlot is null)
        {
            return Task.CompletedTask;
        }

        if (result.CompletionException is not null)
        {
            result.CompletedSlot.TryFail(result.CompletionException);
            return Task.CompletedTask;
        }

        if (result.CompletedUtterance is not null)
        {
            result.CompletedSlot.TryComplete(result.CompletedUtterance);
        }

        return Task.CompletedTask;
    }
}
