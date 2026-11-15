using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Gemeinsamer Ereignisumschlag für die Studientelemetrie.
/// </summary>
public sealed class TelemetryEvent
{
    /// <summary>
    /// Unveränderliche Transport-ID dieses Umschlags.
    /// </summary>
    [JsonPropertyName(TelemetryEnvelopeFieldNames.EventId)]
    public required string EventId { get; set; }

    /// <summary>
    /// Sitzungskennung der begonnenen Evaluationsaufgabe. Aufgabengebundene
    /// Umschläge tragen sie. Für modellierte Diagnosen außerhalb der Aufgabe ist sie unzulässig.
    /// </summary>
    [JsonPropertyName(TelemetryEnvelopeFieldNames.SessionId)]
    public string? SessionId { get; set; }

    /// <summary>
    /// Monotone Millisekunden seit Aufgabenbeginn auf dem Client.
    /// Aufgabengebundene Umschläge tragen sie zusammen mit der Sitzungskennung.
    /// </summary>
    [JsonPropertyName(TelemetryEnvelopeFieldNames.ClientOffsetMs)]
    public int? ClientOffsetMs { get; set; }

    /// <summary>
    /// Pseudonymisierte Teilnehmenden-ID.
    /// </summary>
    [JsonPropertyName(TelemetryEnvelopeFieldNames.ParticipantId)]
    public required string ParticipantId { get; set; }

    /// <summary>
    /// Studienbedingung A oder B.
    /// </summary>
    [JsonPropertyName(TelemetryEnvelopeFieldNames.Condition)]
    public required StudyCondition Condition { get; set; }

    /// <summary>
    /// Anwendungskennung der Studienanwendung.
    /// </summary>
    [JsonPropertyName(TelemetryEnvelopeFieldNames.ApplicationId)]
    public required string ApplicationId { get; set; }

    /// <summary>
    /// Formularkennung des Studienformulars.
    /// </summary>
    [JsonPropertyName(TelemetryEnvelopeFieldNames.TaskId)]
    public required string TaskId { get; set; }

    /// <summary>
    /// Herkunftsklasse der Datensatzerzeugung.
    /// </summary>
    [JsonPropertyName(TelemetryEnvelopeFieldNames.RecordProvenance)]
    public required string RecordProvenance { get; set; }

    /// <summary>
    /// Ereignisname als Diskriminator.
    /// </summary>
    [JsonPropertyName(TelemetryEnvelopeFieldNames.Event)]
    public required string Event { get; set; }

    /// <summary>
    /// Monoton steigende Zustandsversion innerhalb der Aufgabe.
    /// </summary>
    [JsonPropertyName(TelemetryEnvelopeFieldNames.StateVersion)]
    public required int StateVersion { get; set; }

    /// <summary>
    /// Ereignisspezifische, nicht-sensible Metadaten.
    /// </summary>
    [JsonPropertyName(TelemetryEnvelopeFieldNames.Data)]
    public JsonElement? Data { get; set; }

    /// <summary>
    /// Serverseitig gemessener Empfangszeitpunkt. Nur die gespeicherte Zeile trägt ihn.
    /// Ein gesendeter Umschlag, der ihn nennt, wird abgewiesen.
    /// </summary>
    [JsonPropertyName(TelemetryRecordReceivedAt.MemberName)]
    public string? RecordReceivedAt { get; set; }
}
