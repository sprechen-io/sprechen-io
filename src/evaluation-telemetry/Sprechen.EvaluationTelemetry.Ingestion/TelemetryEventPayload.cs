using System.Text.Json;
using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Enthält die modellierten Felder eines Auswertungsereignisses.
/// </summary>
public sealed record TelemetryEventPayload
{
    /// <summary>
    /// Lehnt unbekannte JSON-Mitglieder ab, damit sie bei der erneuten Serialisierung
    /// nicht unbemerkt verloren gehen.
    /// </summary>
    public static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    [JsonPropertyName(TelemetryEnvelopeFieldNames.EventId)]
    public required string EventId { get; init; }

    /// <summary>
    /// Durchlaufkennung der begonnenen Aufgabe. Fehlt bei modellierten Diagnosen
    /// außerhalb einer Aufgabe, weil das Modell das Mitglied dort abweist.
    /// </summary>
    [JsonPropertyName(TelemetryEnvelopeFieldNames.SessionId)]
    public string? SessionId { get; init; }

    /// <summary>
    /// Monotone Clientmillisekunden seit Aufgabenbeginn. Fehlt bei Diagnosen
    /// außerhalb einer Aufgabe, weil das Modell das Mitglied dort abweist.
    /// </summary>
    [JsonPropertyName(TelemetryEnvelopeFieldNames.ClientOffsetMs)]
    public int? ClientOffsetMs { get; init; }

    [JsonPropertyName(TelemetryEnvelopeFieldNames.ParticipantId)]
    public required string ParticipantId { get; init; }

    [JsonPropertyName(TelemetryEnvelopeFieldNames.Condition)]
    public required string Condition { get; init; }

    [JsonPropertyName(TelemetryEnvelopeFieldNames.ApplicationId)]
    public required string ApplicationId { get; init; }

    [JsonPropertyName(TelemetryEnvelopeFieldNames.TaskId)]
    public required string TaskId { get; init; }

    [JsonPropertyName(TelemetryEnvelopeFieldNames.RecordProvenance)]
    public required string RecordProvenance { get; init; }

    [JsonPropertyName(TelemetryEnvelopeFieldNames.Event)]
    public required string Event { get; init; }

    [JsonPropertyName(TelemetryEnvelopeFieldNames.StateVersion)]
    public required int StateVersion { get; init; }

    /// <summary>
    /// Serverseitiger Empfangszeitpunkt. Beim Lesen eingehender Ereignisse fehlt er,
    /// weshalb er nicht als required deklariert ist. Mitgesendete Werte werden
    /// vor der Deserialisierung von der Schemaprüfung abgelehnt.
    /// </summary>
    [JsonPropertyName(TelemetryRecordReceivedAt.MemberName)]
    public string? RecordReceivedAt { get; init; }

    [JsonPropertyName(TelemetryEnvelopeFieldNames.Data)]
    public JsonElement? Data { get; init; }
}
