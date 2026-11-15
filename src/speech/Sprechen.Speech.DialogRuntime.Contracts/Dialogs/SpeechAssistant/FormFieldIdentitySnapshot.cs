namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Verknüpft das fokussierte Feld mit Variable und Feldgruppe zur Zuordnung von Wertverarbeitung und Navigation.
/// </summary>
public sealed record FormFieldIdentitySnapshot(
    string FieldId,
    string VariableId,
    string FieldGroupId);
