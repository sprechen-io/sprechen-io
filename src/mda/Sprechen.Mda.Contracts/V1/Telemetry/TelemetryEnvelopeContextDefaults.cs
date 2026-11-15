namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Platzhalter im Telemetrieumschlag für Ereignisse ohne zugeordnete Person,
/// Aufgabe oder Anwendung. Da diese Felder immer erforderlich sind, verwenden
/// alle Aufrufstellen dieselben benannten Werte.
/// </summary>
public static class TelemetryEnvelopeContextDefaults
{
    /// <summary>
    /// Umschlagwert ohne Studien-Teilnehmendenidentität.
    /// </summary>
    public const string UnknownParticipantId = "P000";

    /// <summary>
    /// Umschlagwert ohne Studienaufgabe.
    /// </summary>
    public const string UnknownTaskId = "T00";

    /// <summary>
    /// Umschlagwert, bevor eine Anwendung gewählt wurde.
    /// </summary>
    public const string UnknownApplicationId = "A00";

    /// <summary>
    /// Zustandsversion außerhalb jeder Aufgabe. <c>state_version</c> steigt innerhalb einer
    /// Aufgabe. Ohne Aufgabe gilt das Schema-Minimum, da noch kein Aufgabenfortschritt vorliegt.
    /// </summary>
    public const int OutsideTaskStateVersion = 0;
}
