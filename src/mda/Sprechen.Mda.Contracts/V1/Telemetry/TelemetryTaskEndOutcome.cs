namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Aufgezeichneter Endzustand, Beendigungsgrund und systemseitiger Abgleich
/// einer Aufgabe. Damit lässt sich das Anwendungsergebnis nach dauerhafter
/// Annahme wiederherstellen.
/// </summary>
/// <param name="EndState">Gesendeter Endzustand.</param>
/// <param name="TerminationClassification">Im Modell festgelegter Grund für die Beendigung durch die Anwendung.</param>
/// <param name="MismatchCode">Aufgabenweiter Abweichungscode des systemseitigen Abgleichs.</param>
/// <param name="CorrectnessBasis">Evidenzart des Abgleichs.</param>
public sealed record TelemetryTaskEndOutcome(
    string EndState,
    string TerminationClassification,
    string MismatchCode,
    string CorrectnessBasis);
