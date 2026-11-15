namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Aggregierte Diagnosedaten zu Fehlschlägen und Wiederherstellung während eines Aktivierungsversuchs.
/// </summary>
/// <remarks>
/// Der Bericht ist für Textlängen, Tokenzahl, Konfidenz, Auswertungsstatus und modellierte
/// Aktivierungsformen vorgesehen. Das erkannte Transkript gehört nicht in seine Stringfelder.
/// </remarks>
/// <param name="TranscriptLength">Länge des Erkennerergebnisses.</param>
/// <param name="NormalizedTranscriptLength">Länge nach Matcher-Normalisierung.</param>
/// <param name="TokenCount">Zahl der vom Matcher ausgewerteten Tokens.</param>
/// <param name="EvaluationStatus">Urteil der Aktivierungsregel, etwa <c>MissingLeadingWakeWord</c>.</param>
/// <param name="ConfidencePercent">Erkenner-Konfidenz der Äußerung, wenn er eine meldete.</param>
/// <param name="ActivationForms">Modellierte Formen, mit denen das Transkript verglichen wurde.</param>
/// <param name="RecognitionOnlyActivationForms">Für die Erkennung zugelassene, aber nicht als Eingabe angebotene Formen.</param>
public sealed record WakeMissDiagnosticsReport(
    int TranscriptLength,
    int NormalizedTranscriptLength,
    int TokenCount,
    string EvaluationStatus,
    int? ConfidencePercent,
    IReadOnlyList<string> ActivationForms,
    IReadOnlyList<string> RecognitionOnlyActivationForms);
