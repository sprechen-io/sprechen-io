namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Wörter für die gesprochene Uhrzeit, nach sprachlicher Funktion getrennt.
/// Sie stammen aus dem Formular-Transkript-Lexikon des Modells. Getrennte Rollen
/// ermöglichen Pluralformen und Sonderformen für ein Uhr. Ungenutzte Rollen bleiben leer.
/// </summary>
internal sealed class FormSpokenTimeFormat
{
    internal static FormSpokenTimeFormat Empty { get; } = new();

    // Deutsch: Stundenwort Uhr, etwa in 14 Uhr 30.
    public string ClockMarker { get; init; } = string.Empty;

    // Englisch: Zusätze AM und PM für das Zwölfstundenformat.
    public string AnteMeridiem { get; init; } = string.Empty;

    public string PostMeridiem { get; init; } = string.Empty;

    // Französisch: heure für Stunde 0 und 1, heures für höhere Stunden.
    public string HourWordSingular { get; init; } = string.Empty;

    public string HourWordPlural { get; init; } = string.Empty;

    // Spanisch: weiblicher Artikel für ein Uhr, etwa la 1.
    public string ArticleSingular { get; init; } = string.Empty;

    // Spanisch und Italienisch: Stundenartikel las und le, auch bei zusätzlicher Minutenangabe.
    public string ArticlePlural { get; init; } = string.Empty;

    // Italienisch: l'una als eigene Form für ein Uhr statt Artikel und Zahl.
    public string OneOClockForm { get; init; } = string.Empty;

    // Verbindung von Stunde und Minute: y auf Spanisch, e auf Italienisch.
    public string MinuteConnector { get; init; } = string.Empty;
}
