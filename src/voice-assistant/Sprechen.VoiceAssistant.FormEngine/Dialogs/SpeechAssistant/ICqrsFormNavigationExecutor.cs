using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt die Navigationsbefehle des Formularlaufs, damit Aufrufer Fokus-, Sprung- und Fehlernavigation über denselben Zugang anfordern.
/// </summary>
internal interface ICqrsFormNavigationExecutor
{
    Task<NavigationResult> NavigateNextAsync(string sessionId);

    Task<NavigationResult> NavigateBackAsync(string sessionId);

    Task<NavigationResult> NavigateToFieldGroupAsync(string sessionId, string fieldGroupId);

    Task<FormJumpExecution> JumpToTargetAsync(FormJumpTarget target);

    Task<FormConstraintViolation?> FocusNextErrorAsync();

    Task<FocusNextOpenRequiredFieldResult> FocusNextOpenRequiredFieldAsync();
}

