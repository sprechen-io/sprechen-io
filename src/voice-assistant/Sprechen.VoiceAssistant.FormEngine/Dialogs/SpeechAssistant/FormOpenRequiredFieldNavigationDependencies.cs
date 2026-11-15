namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt die Auswahl offener Pflichtfelder und die Regel für deren Fokussierung
/// als gemeinsame Abhängigkeit des Navigationsbefehls.
/// </summary>
internal sealed record FormOpenRequiredFieldNavigationDependencies(
    FormOpenRequiredFieldSelector Selector,
    FormOpenRequiredFieldFocusPolicy FocusPolicy);
