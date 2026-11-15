namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Einstellungen, Ansagefunktionen und Ergebnisbehandlung eines Auswahllaufs.
/// Stellt dazu Bestätigungs- und Fehlerregeln, Diagnose und Abschlussablauf bereit.
/// </summary>
public sealed class SelectionDialogExecutionDefinition<TSelection, TResult>
    where TSelection : class
{
    public SelectionDialogExecutionDefinition(
        SelectionDialogExecutionSettings<TSelection> settings,
        SelectionDialogPromptProviders<TSelection> promptProviders,
        SelectionDialogOutcomeHandlers<TSelection, TResult> outcomeHandlers,
        SelectionConfirmationTransitionPolicy<TSelection> confirmationPolicy)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        PromptProviders = promptProviders ?? throw new ArgumentNullException(nameof(promptProviders));
        OutcomeHandlers = outcomeHandlers ?? throw new ArgumentNullException(nameof(outcomeHandlers));
        ConfirmationPolicy = confirmationPolicy ?? throw new ArgumentNullException(nameof(confirmationPolicy));
        EvidenceTrace = new SelectionDialogEvidenceTrace<TSelection>(Settings.Trace);
        LaunchFinalizer = new SelectionDialogLaunchFinalizer<TSelection, TResult>(Settings, EvidenceTrace);
        CaptureFailurePolicy = new SelectionCaptureFailureTransitionPolicy(Settings.MaxRetries);
    }

    public SelectionDialogExecutionSettings<TSelection> Settings { get; }

    public SelectionDialogPromptProviders<TSelection> PromptProviders { get; }

    public SelectionDialogOutcomeHandlers<TSelection, TResult> OutcomeHandlers { get; }

    public SelectionConfirmationTransitionPolicy<TSelection> ConfirmationPolicy { get; }

    internal SelectionDialogEvidenceTrace<TSelection> EvidenceTrace { get; }

    internal SelectionDialogLaunchFinalizer<TSelection, TResult> LaunchFinalizer { get; }

    public SelectionCaptureFailureTransitionPolicy CaptureFailurePolicy { get; }
}
