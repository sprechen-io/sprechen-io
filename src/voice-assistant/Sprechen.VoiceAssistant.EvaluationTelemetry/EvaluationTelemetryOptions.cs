namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Steuert, ob neue Sprachsitzungen Ereignisse dauerhaft aufzeichnen und versenden.
/// Der flüchtige Sitzungszustand bleibt davon getrennt, weil etwa die Zuordnung
/// einer Unterbrechung zum fokussierten Feld auch ohne Aufzeichnung gebraucht wird.
/// Die Aufzeichnung ist standardmäßig aktiviert. Außerhalb der Studie kann
/// <see cref="IsEnabled"/> sie für neue Durchläufe abschalten.
/// </summary>
public sealed class EvaluationTelemetryOptions
{
    public const string SectionName = "EvaluationTelemetry";

    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Beschränkt gewöhnliche Aufzeichnungen auf Durchläufe mit gebundener Teilnehmendenkennung. Gesondert zugelassene Prüfläufe verwenden ihren eigenen Kontext.
    /// Diese Bedingung ergänzt <see cref="IsEnabled"/>.
    /// </summary>
    public bool RequireStudyContext { get; set; } = true;
}
