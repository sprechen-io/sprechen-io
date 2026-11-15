namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Beschreibt einen Wake-Word-Fehlschlag im modellierten Auswertungswortschatz für die
/// <c>wake_word_miss</c>-Nutzlast. Die Werte sind
/// modellierte Phasen-, Ereigniscode- und Statuszeichenketten statt Laufzeit-Enums,
/// damit die Auswertungstelemetrie keine Wake-Word-Laufzeittypen benötigt. Fehlende optionale
/// Werte bleiben <see langword="null"/> und werden in der Nutzlast ausgelassen,
/// statt einen Messwert 0 vorzutäuschen.
/// </summary>
/// <param name="Phase">Modellierte Fehlschlag-Phase.</param>
/// <param name="EventCode">Modellierter Fehlschlag-Ereigniscode.</param>
/// <param name="Status">Modellierter Wake-Word-Aktivierungsstatus.</param>
/// <param name="ConfidencePercent">Erkenner-Konfidenz in Prozent, wenn der Erkenner eine lieferte.</param>
/// <param name="TranscriptTextLength">Länge des abgelehnten Transkripts, falls vorhanden.</param>
/// <param name="ActivationFormCount">Zahl der modellierten Aktivierungsformen im betrachteten Kontext.</param>
/// <param name="RecognitionOnlyActivationFormCount">Zahl zusätzlich erkannter, nicht angebotener Aktivierungsvarianten im betrachteten Kontext.</param>
/// <param name="ExceptionType">Typname des Fehlers, der den Fehlschlag erzeugte, wenn einer vorlag.</param>
public sealed record EvaluationWakeWordMiss(
    string Phase,
    string EventCode,
    string Status,
    int? ConfidencePercent,
    int? TranscriptTextLength,
    int? ActivationFormCount,
    int? RecognitionOnlyActivationFormCount,
    string? ExceptionType);
