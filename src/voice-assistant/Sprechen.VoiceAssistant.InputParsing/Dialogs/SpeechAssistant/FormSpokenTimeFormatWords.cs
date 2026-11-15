namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Sprachbezogene Wörter für die Ausgabe von Uhrzeiten aus dem Transkriptlexikon. Artikel, Stundenwörter und Bindewörter werden getrennt gespeichert.
/// </summary>
internal sealed class FormSpokenTimeFormatWords
{
    // Deutsche Uhrmarkierung, etwa „Uhr“ in „14 Uhr 30“.
    public string? ClockMarker { get; init; }

    // Englische Zusätze der Zwölfstundenuhr: „AM“ und „PM“.
    public string? AnteMeridiem { get; init; }

    public string? PostMeridiem { get; init; }

    // Französische Stundenwörter in Einzahl und Mehrzahl: „heure“ und „heures“.
    public string? HourWordSingular { get; init; }

    public string? HourWordPlural { get; init; }

    // Spanischer Artikel für ein Uhr, etwa „la“ in „la 1“.
    public string? ArticleSingular { get; init; }

    // Spanische und italienische Artikel der Mehrzahl, etwa „las“ oder „le“.
    public string? ArticlePlural { get; init; }

    // Italienische Gesamtform für ein Uhr, etwa „l’una“.
    public string? OneOClockForm { get; init; }

    // Spanisches oder italienisches Bindewort zwischen Stunde und Minute: „y“ oder „e“.
    public string? MinuteConnector { get; init; }
}
