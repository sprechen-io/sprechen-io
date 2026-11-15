namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Name und Darstellung des serverseitig gemessenen Empfangszeitpunkts
/// in gespeicherten Telemetriedatensätzen. Das Mitglied gehört nicht zum gesendeten
/// Ereignisumschlag und wird deshalb getrennt von <see cref="TelemetryEnvelopeFieldNames"/> definiert.
/// </summary>
public static class TelemetryRecordReceivedAt
{
    /// <summary>Name des Mitglieds in der gespeicherten Zeile.</summary>
    public const string MemberName = "record_received_at";

    /// <summary>
    /// Formatbezeichner für die Roundtrip-Darstellung mit sieben Nachkommastellen
    /// und Zeitzonenversatz bei <c>DateTimeOffset</c>. Der Versatz unterscheidet lokale
    /// Uhrzeiten, die bei einer Zeitumstellung doppelt auftreten.
    /// </summary>
    public const string Representation = "O";
}
