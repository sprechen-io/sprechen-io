using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Hält den fachlichen Abschlussstand einer Aufgabe getrennt von der Absendeaktion.
/// So kann die Auswertung den Abschluss auch für Aufgaben beurteilen, die nicht
/// selbst das Absenden des gesamten Formulars auslösen.
/// </summary>
/// <param name="RequiredFieldsTotal">Pflichtfelder im Umfang der Aufgabe.</param>
/// <param name="RequiredFieldsCompleted">Davon abgeschlossene Pflichtfelder.</param>
/// <param name="OpenBlockingValidationCount">Noch offene Prüfungen, die den Zielzustand blockieren.</param>
/// <param name="TerminationClassification">Modellierter Grund für das Ende der Aufgabe.</param>
/// <param name="MismatchCode">Aufgabenweiter Abweichungscode des systemseitigen Abgleichs.</param>
public sealed record EvaluationTaskCompletion(
    int RequiredFieldsTotal,
    int RequiredFieldsCompleted,
    int OpenBlockingValidationCount,
    ParticipantTaskTerminationClassification TerminationClassification,
    string MismatchCode)
{
    /// <summary>
    /// Abschlussstand ohne verfügbaren Aufgabenstatus. Alle Zähler sind 0.
    /// Aus diesen Zählern wird keine Zielerreichung abgeleitet.
    /// </summary>
    public static EvaluationTaskCompletion Unknown { get; } = new(
        0,
        0,
        0,
        ParticipantTaskTerminationClassification.SessionAbandoned,
        TelemetryMismatchCodes.NotAssessed);
}
