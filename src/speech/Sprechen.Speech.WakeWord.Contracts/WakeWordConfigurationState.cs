namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Hält Vorgaben, Überschreibungen und Editorinhalt für eine Sprache zusammen, damit die Einstellungssicht den wirksamen Bestand erklären kann.
/// </summary>
public sealed record WakeWordConfigurationState(
    string SelectedIsoCode,
    string[] DefaultWakeWordActivationForms,
    string[] OverrideWakeWordActivationForms,
    string EditorText);
