namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Verknüpft den gespeicherten Auswahlcode mit seiner lesbaren Bezeichnung für Sprachauswahl und Wertansagen.
/// </summary>
public sealed record FormChoiceOption(string Code, string DisplayName);

