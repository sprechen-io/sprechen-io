namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt den anhand der Bestätigungsregeln vorgeschlagenen Zustandsübergang
/// samt erwartetem Kandidaten und Rückmeldeart für die anschließende Zustandsprüfung.
/// </summary>
internal sealed record SelectionConfirmationTransitionDecision<TSelection>(
    SelectionConfirmationTransitionKind Kind,
    SelectionDialogIntentStep<TSelection>? ProposedStep,
    SelectionCandidate<TSelection>? ExpectedCandidate,
    SelectionRevision? ExpectedRevision,
    SelectionConfirmationCaptureStage? NextStage,
    SelectionConfirmationPromptKind? PromptKind,
    bool ConsumesRepairFailure,
    bool ResetsRecoverableFailure)
    where TSelection : class;
