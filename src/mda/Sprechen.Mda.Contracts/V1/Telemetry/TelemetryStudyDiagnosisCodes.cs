namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Gemeinsame Diagnosearten der Fehlerereignisse beider Studienbedingungen.
/// Sie fassen unterschiedliche interne Fehlercodes zusammen, damit der Vergleich
/// nicht von der Zahl und Aufteilung anwendungsspezifischer Codes abhängt.
/// </summary>
public static class TelemetryStudyDiagnosisCodes
{
    /// <summary>
    /// Verletzung einer Validierungsregel.
    /// </summary>
    public const string Validation = "D-VALIDATION";

    /// <summary>
    /// Fehlgeschlagener Navigations- oder Formularlogikschritt.
    /// </summary>
    public const string Navigation = "D-NAVIGATION";

    /// <summary>
    /// Ausfall der Spracherkennung, der unabhängig vom Transkript belegt ist.
    /// </summary>
    public const string SpeechRecognition = "D-STT";

    /// <summary>
    /// Alle zulässigen Diagnosekennungen für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Navigation,
        SpeechRecognition,
        Validation,
    };
}
