namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Unterstützte ISO-639-1-Sprachcodes als Grundlage sprachbezogener Kataloge.
/// Die Menge folgt der Modelldeklaration und entspricht dem dienstseitigen Sprachangebot
/// in SupportedLanguagesOptions. Sie wird hier gespiegelt, weil die Dialogschicht
/// die Dienstschicht nicht referenzieren darf. Die Reihenfolge ist kein Vertrag.
/// </summary>
public static class LanguageDescriptors
{
    public static IReadOnlyList<string> AllIsoCodes { get; } = ["de", "en", "fr", "es", "it"];
}
