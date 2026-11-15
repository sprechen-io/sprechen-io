using Sprechen.Common.Cqrs;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormFocus;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormRules;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Enthält Zustand, Definitionen, Dienste, Fokusregeln und Befehlsausführer eines Formularlaufs. FormInterpreterRuntimeOwner übernimmt diese Komponenten nach dem Eintragen der Anfangswerte.
/// </summary>
internal sealed record FormRuntimeComposition(
    FormMutableStateStore MutableState,
    FormDialogDefinitionState DefinitionState,
    FormDialogDependencies Services,
    FormTranscriptApplicator TranscriptApplicationService,
    FormValidationErrorFocusPolicy ValidationErrorFocusService,
    FormOpenRequiredFieldFocusPolicy OpenRequiredFieldFocusService,
    FormAdditionalFieldProposalDependencies AdditionalFieldProposal,
    ICqrsFormNavigationExecutor NavigationExecutor,
    ICqrsFormRulesExecutor RulesExecutor,
    ICqrsFormEditingExecutor EditingExecutor,
    ICqrsFormFocusExecutor FocusExecutor,
    ICqrsFormPrivacyExecutor PrivacyExecutor,
    ICqrsFormStatusExecutor StatusExecutor,
    IEventStore EventStore);
