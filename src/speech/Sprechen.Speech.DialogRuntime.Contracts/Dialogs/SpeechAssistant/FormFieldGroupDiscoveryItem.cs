namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Kennzeichnet eine Feldgruppe mit Position und Fokusstatus für Orientierungsansagen.
/// </summary>
public sealed record FormFieldGroupDiscoveryItem(
    string FieldGroupId,
    string Title,
    int Index,
    bool IsCurrent);
