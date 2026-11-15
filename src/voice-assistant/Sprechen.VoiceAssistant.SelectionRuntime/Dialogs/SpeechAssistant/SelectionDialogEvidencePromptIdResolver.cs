namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet eingeplanten Auswahlansagen stabile Diagnosekennungen zu, damit ihre Rolle unabhängig vom
/// lokalisierten Text ausgewertet werden kann.
/// </summary>
internal sealed class SelectionDialogEvidencePromptIdResolver
{
    public string Announcement => "selection.announce";

    public string Listening => "selection.listening";

    public string Launch => "selection.launch";

    public string Cancelled => "selection.cancelled";

    public string ResolveAppliedPhase(
        SelectionDialogPhase appliedPhase,
        SelectionDialogCompletionReason? completionReason)
    {
        if (!Enum.IsDefined(appliedPhase))
        {
            throw new ArgumentOutOfRangeException(
                nameof(appliedPhase),
                appliedPhase,
                "Unknown applied selection-dialog phase.");
        }

        if (completionReason.HasValue && !Enum.IsDefined(completionReason.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(completionReason),
                completionReason,
                "Unknown applied selection-dialog completion reason.");
        }

        if (appliedPhase == SelectionDialogPhase.Completed)
        {
            if (!completionReason.HasValue)
            {
                throw new SelectionCandidateStateException(
                    "An applied completed phase requires one typed completion reason.");
            }

            return Cancelled;
        }

        if (completionReason.HasValue)
        {
            throw new SelectionCandidateStateException(
                "An applied non-terminal phase cannot carry a completion reason.");
        }

        return appliedPhase switch
        {
            SelectionDialogPhase.Announcing => Announcement,
            SelectionDialogPhase.Listening => Listening,
            SelectionDialogPhase.Confirming => Listening,
            SelectionDialogPhase.Launch => Launch,
            _ => throw new ArgumentOutOfRangeException(
                nameof(appliedPhase),
                appliedPhase,
                "Unknown applied selection-dialog phase."),
        };
    }

    public string ResolveConfirmation(
        SelectionConfirmationPromptKind promptKind,
        SelectionConfirmationCaptureStage stage)
        => promptKind switch
        {
            SelectionConfirmationPromptKind.InitialConfirmation => "selection.confirmation.initial",
            SelectionConfirmationPromptKind.LowConfidenceConfirmation => "selection.confirmation.initial",
            SelectionConfirmationPromptKind.ExplicitAuthorization => "selection.confirmation.explicitAuthorization",
            SelectionConfirmationPromptKind.VerificationUnavailable => "selection.confirmation.confidenceFaultRetry",
            SelectionConfirmationPromptKind.NotOpenedReselect => Listening,
            SelectionConfirmationPromptKind.Cancellation => Cancelled,
            SelectionConfirmationPromptKind.NoInputReminder => "selection.confirmation.noInput",
            SelectionConfirmationPromptKind.ActiveStage => ResolveActiveStage(stage),
            SelectionConfirmationPromptKind.ActiveStageList => ResolveActiveStage(stage),
            SelectionConfirmationPromptKind.VerificationFailedTerminal => Cancelled,
            SelectionConfirmationPromptKind.RepairExhaustedTerminal => Cancelled,
            SelectionConfirmationPromptKind.ContextualHelp => ResolveActiveStage(stage),
            _ => throw new ArgumentOutOfRangeException(
                nameof(promptKind),
                promptKind,
                "Unknown confirmation prompt kind."),
        };

    private static string ResolveActiveStage(SelectionConfirmationCaptureStage stage)
        => stage switch
        {
            SelectionConfirmationCaptureStage.Initial => "selection.confirmation.initial",
            SelectionConfirmationCaptureStage.ExplicitAuthorization => "selection.confirmation.explicitAuthorization",
            SelectionConfirmationCaptureStage.ConfidenceFaultRetry => "selection.confirmation.confidenceFaultRetry",
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown confirmation capture stage."),
        };
}
