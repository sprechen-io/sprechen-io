using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Definition und Bezeichnung des ausgewählten Formularziels für die nachfolgenden Wechselprüfungen zusammen.
/// </summary>
internal sealed record FormVoiceCommandSwitchFormTarget(
    FormDescriptor Form,
    string DisplayName);
