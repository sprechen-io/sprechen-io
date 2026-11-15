namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Gemeinsame Ableitung des aufzuzeichnenden Aufgabenendzustands für beide Anwendungen.
/// Das Erreichen eines Aufgabenziels und das Absenden des gesamten Prozesses werden
/// getrennt erfasst, da Aufgaben ihren Zielzustand auch ohne Absenden erreichen können.
/// </summary>
public static class TelemetryTaskEndProjection
{
    /// <summary>
    /// Leitet den aufzuzeichnenden Endzustand aus Abgabe, Pflichtfeldstand und Beendigungsgrund ab.
    /// </summary>
    /// <param name="processSubmitted">Die Person hat den ganzen Prozess abgeschickt. Das kann nur die abschließende Aufgabe.</param>
    /// <param name="requiredFieldsTotal">Pflichtfelder im Umfang der Aufgabe.</param>
    /// <param name="requiredFieldsCompleted">Davon, wie viele vollständig sind.</param>
    /// <param name="openBlockingValidationCount">Noch offene Validierungsfehler, die den Zielzustand sperren.</param>
    /// <param name="terminationClassification">Modelleigener Grund, warum der Kanal die Aufgabe beendet hat.</param>
    public static TelemetryTaskEndOutcome Project(
        bool processSubmitted,
        int requiredFieldsTotal,
        int requiredFieldsCompleted,
        int openBlockingValidationCount,
        string terminationClassification)
    {
        if (!TelemetryTaskTerminationClassifications.All.Contains(terminationClassification))
        {
            throw new ArgumentOutOfRangeException(
                nameof(terminationClassification),
                terminationClassification,
                "Unknown task termination classification.");
        }

        // Technische Fehler haben Vorrang vor einem angeordneten Studienabbruch. Beide
        // haben Vorrang vor Abgabe und Zielzustand. Den Studienabbruch legt die
        // Studienbeobachtung fest, nicht die Vollständigkeitsprüfung des Formulars.
        if (terminationClassification == TelemetryTaskTerminationClassifications.TechnicalFailure)
        {
            return new TelemetryTaskEndOutcome(
                TelemetryTaskEndStates.TechnicalFailure,
                terminationClassification,
                TelemetryMismatchCodes.NotAssessed,
                TelemetryCorrectnessBases.LogAndSystemCheck);
        }

        if (terminationClassification == TelemetryTaskTerminationClassifications.StudyDirectedAbort)
        {
            return new TelemetryTaskEndOutcome(
                TelemetryTaskEndStates.Aborted,
                terminationClassification,
                TelemetryMismatchCodes.NotAssessed,
                TelemetryCorrectnessBases.LogAndSystemCheck);
        }

        if (processSubmitted)
        {
            return new TelemetryTaskEndOutcome(
                TelemetryTaskEndStates.ProcessSubmitted,
                terminationClassification,
                TelemetryMismatchCodes.None,
                TelemetryCorrectnessBases.LogAndSystemCheck);
        }

        // Ohne Pflichtfelder lässt sich das Aufgabenziel mit dieser Prüfung nicht
        // feststellen. Deshalb wird in diesem Fall kein erreichter Zielzustand abgeleitet.
        var targetStateReached = requiredFieldsTotal > 0
            && requiredFieldsCompleted >= requiredFieldsTotal
            && openBlockingValidationCount == 0;

        return new TelemetryTaskEndOutcome(
            targetStateReached ? TelemetryTaskEndStates.TaskTargetReached : TelemetryTaskEndStates.NotSubmitted,
            terminationClassification,
            targetStateReached ? TelemetryMismatchCodes.None : TelemetryMismatchCodes.NotSubmitted,
            TelemetryCorrectnessBases.LogAndSystemCheck);
    }

    /// <summary>
    /// Hängt den systemseitigen Abgleich an einen bereits abgeleiteten Endzustand.
    /// </summary>
    public static TelemetryTaskEndOutcome WithComparison(
        string endState,
        string terminationClassification,
        string mismatchCode)
        => new(
            endState,
            terminationClassification,
            mismatchCode,
            TelemetryCorrectnessBases.LogAndSystemCheck);
}
