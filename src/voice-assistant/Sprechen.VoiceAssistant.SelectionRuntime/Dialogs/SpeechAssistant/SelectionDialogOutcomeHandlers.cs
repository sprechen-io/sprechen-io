namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Funktionen zur Auswertung von Auswahl- und Bestätigungsantworten
/// sowie zur Ergebnisbildung. Die gemeinsame Laufzeit ruft darüber die
/// fachliche Verarbeitung des jeweiligen Dialogs auf.
/// </summary>
public sealed class SelectionDialogOutcomeHandlers<TSelection, TResult>
    where TSelection : class
{
    public SelectionDialogOutcomeHandlers(
        Func<string, SelectionDialogTurnSnapshot<TSelection>, SelectionDialogPrompt?, CancellationToken, Task<SelectionDialogIntentStep<TSelection>>> onListeningInputAsync,
        Func<string, CancellationToken, Task<SelectionDialogCommonIntentParseResult>> onConfirmInputAsync,
        SelectionDialogLaunchDefinition<TSelection, TResult> selectedLaunch,
        Func<SelectionDialogCompletionReason, SelectionDialogTurnSnapshot<TSelection>, TResult> createCancelledResult,
        SelectionDialogLaunchDefinition<TSelection, TResult>? singleItemLaunch = null)
    {
        OnListeningInputAsync = onListeningInputAsync ?? throw new ArgumentNullException(nameof(onListeningInputAsync));
        OnConfirmInputAsync = onConfirmInputAsync ?? throw new ArgumentNullException(nameof(onConfirmInputAsync));
        SelectedLaunch = selectedLaunch ?? throw new ArgumentNullException(nameof(selectedLaunch));
        CreateCancelledResult = createCancelledResult ?? throw new ArgumentNullException(nameof(createCancelledResult));
        SingleItemLaunch = singleItemLaunch;
    }

    public Func<string, SelectionDialogTurnSnapshot<TSelection>, SelectionDialogPrompt?, CancellationToken, Task<SelectionDialogIntentStep<TSelection>>> OnListeningInputAsync { get; }

    public Func<string, CancellationToken, Task<SelectionDialogCommonIntentParseResult>> OnConfirmInputAsync { get; }

    public SelectionDialogLaunchDefinition<TSelection, TResult> SelectedLaunch { get; }

    public Func<SelectionDialogCompletionReason, SelectionDialogTurnSnapshot<TSelection>, TResult> CreateCancelledResult { get; }

    public SelectionDialogLaunchDefinition<TSelection, TResult>? SingleItemLaunch { get; }
}
