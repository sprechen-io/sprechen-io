using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bereitet die Ankündigung des Auswahldialogs vor und wechselt in die Aufnahmephase.
/// Die Ansage wird zur späteren Wiedergabe vorgemerkt.
/// </summary>
internal sealed class SelectionDialogAnnouncingPhase<TSelection, TResult>
    where TSelection : class
{
    private readonly SelectionDialogExecutionDefinition<TSelection, TResult> _definition;
    private readonly Func<string, SpeechAssistantTraceLevel, CancellationToken, ValueTask> _traceAsync;

    public SelectionDialogAnnouncingPhase(
        SelectionDialogExecutionDefinition<TSelection, TResult> definition,
        Func<string, SpeechAssistantTraceLevel, CancellationToken, ValueTask> traceAsync)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _traceAsync = traceAsync ?? throw new ArgumentNullException(nameof(traceAsync));
    }

    public async Task RunAsync(
        SelectionDialogRunState<TSelection> state,
        CancellationToken cancellationToken)
    {
        await _traceAsync(
                "Selection runtime is announcing available items.",
                SpeechAssistantTraceLevel.Information,
                cancellationToken)
            .ConfigureAwait(false);

        state.PendingPrompt = await _definition.PromptProviders
            .GetAnnouncingPromptAsync(state.CreateTurnSnapshot(), cancellationToken)
            .ConfigureAwait(false);
        await _definition.EvidenceTrace
            .AnnouncementPromptScheduledAsync(state.PendingPrompt, cancellationToken)
            .ConfigureAwait(false);

        state.CompleteAnnouncement();
        state.ResetTurnDiagnostics();
    }
}
