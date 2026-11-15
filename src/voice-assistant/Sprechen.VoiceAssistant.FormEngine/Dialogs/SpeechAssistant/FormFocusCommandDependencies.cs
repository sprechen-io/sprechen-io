namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Gemeinsame Dienste für Validierung, Sichtbarkeit, Navigation und Zustandsspeicherung der Fokusbefehle.
/// </summary>
internal sealed record FormFocusCommandDependencies(
    FormDialogValidator Validator,
    FormFieldVisibilityPolicy VisibilityPolicy,
    FormNavigator Navigator,
    FormMutableStateStore MutableState);
