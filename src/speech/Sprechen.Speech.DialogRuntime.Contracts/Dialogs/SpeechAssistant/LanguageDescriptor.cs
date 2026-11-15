namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Kultur und Regeln zur Zeitinterpretation einer unterstützten Sprache, zugeordnet
/// über ihren ISO-639-1-Code. Das Ausgabevokabular des Buchstabierens bleibt im
/// Formular-Transkript-Lexikon, damit Eingabe- und Ausgaberollen getrennt bleiben.
/// </summary>
public sealed record LanguageDescriptor(
    string IsoCode,
    string CultureName,
    bool UsesRomanceTemporalParsing);
