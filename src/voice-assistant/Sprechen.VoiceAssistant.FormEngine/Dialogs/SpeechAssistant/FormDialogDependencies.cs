using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt die Dienste eines Formularlaufs für dessen Ausführung.
/// </summary>
internal sealed class FormDialogDependencies
{
    public required IFormValueParser ValueParser { get; init; }

    public required FormRuleEngine RuleEngine { get; init; }

    public required FormConstraintValidator ConstraintValidator { get; init; }

    public required FormActionExecutor ActionExecutor { get; init; }

    public required FormNavigator Navigator { get; init; }

    public required FormJumpTargetResolver JumpTargetResolver { get; init; }

    public required FormPrivacyReadbackPolicy PrivacyReadbackPolicy { get; init; }

    public required FormFieldInputModeAvailabilityPolicy InputModeAvailabilityPolicy { get; init; }

    public required FormFocusedFieldSnapshotBuilder FocusedFieldSnapshotBuilder { get; init; }

    public required FormDiscoveryProjectionBuilder DiscoveryProjectionService { get; init; }

    public required FormStatusProjectionBuilder StatusProjectionService { get; init; }

    public required FormSummaryProjectionBuilder SummaryProjectionService { get; init; }

    public required FormDialogValidator ValidationService { get; init; }

    public required FormFieldVisibilityPolicy VisibilityService { get; init; }

    public required FormRuleOrchestrator RuleOrchestrationService { get; init; }

    public required FormCqrsProjectionAdapter CqrsProjectionService { get; init; }
}
