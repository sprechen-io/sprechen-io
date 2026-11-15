namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Kennzeichnet ein Feld mit Position und Fokusstatus für Orientierungsansagen.
/// </summary>
public sealed record FormFieldDiscoveryItem(
    string FieldId,
    string FieldGroupId,
    string Label,
    int Index,
    bool IsCurrent);
