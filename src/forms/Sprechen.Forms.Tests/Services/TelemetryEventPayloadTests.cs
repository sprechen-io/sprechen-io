using Sprechen.EvaluationTelemetry.Ingestion;
using Sprechen.Forms.Services;
using Sprechen.Mda.Contracts.V1.Telemetry;
using System.Text.Json;
using Xunit;

namespace Sprechen.Forms.Tests.Services;

public sealed class TelemetryEventPayloadTests
{
    [Fact]
    public void RequiredProperties_MustBeSet()
    {
        var payload = new TelemetryEventPayload
        {
            EventId = "0123456789abcdef0123456789abcdef",
            SessionId = "abcdef0123456789abcdef0123456789",
            ParticipantId = "P001",
            Condition = "A",
            ApplicationId = "sample.checkout",
            TaskId = "T01",
            RecordProvenance = TelemetryRecordProvenances.Participant,
            Event = "task_start",
            StateVersion = 0,
        };

        Assert.Equal("P001", payload.ParticipantId);
        Assert.Equal("A", payload.Condition);
        Assert.Equal("sample.checkout", payload.ApplicationId);
        Assert.Equal("T01", payload.TaskId);
        Assert.Equal("task_start", payload.Event);
        Assert.Equal(0, payload.StateVersion);
    }

    [Fact]
    public void ClientBodyWithoutAReceiptTime_Deserializes()
    {
        // Der Server setzt den Empfangszeitpunkt erst nach dem Deserialisieren.
        // Eine gültige Clientnutzlast muss deshalb ohne dieses Feld eingelesen werden können.
        var body = "{\"event_id\":\"0123456789abcdef0123456789abcdef\",\"session_id\":\"abcdef0123456789abcdef0123456789\",\"client_offset_ms\":0,\"participant_id\":\"P001\""
            + ",\"condition\":\"A\",\"application_id\":\"sample.checkout\",\"task_id\":\"T01\""
            + ",\"record_provenance\":\"participant\",\"event\":\"task_start\",\"state_version\":0"
            + ",\"data\":{\"canonical_transitions\":[]}}";

        var payload = JsonSerializer.Deserialize<TelemetryEventPayload>(body, TelemetryEventPayload.ReadOptions);

        Assert.NotNull(payload);
        Assert.Null(payload!.RecordReceivedAt);
    }

    [Fact]
    public void OptionalData_DefaultsToNull()
    {
        var payload = new TelemetryEventPayload
        {
            EventId = "0123456789abcdef0123456789abcdef",
            SessionId = "abcdef0123456789abcdef0123456789",
            ParticipantId = "P001",
            Condition = "A",
            ApplicationId = "sample.checkout",
            TaskId = "T01",
            RecordProvenance = TelemetryRecordProvenances.Participant,
            Event = "task_start",
            StateVersion = 0,
        };

        Assert.Null(payload.Data);
    }

    [Fact]
    public void CanSetDataObject()
    {
        var data = JsonDocument.Parse("{\"end_state\":\"process_submitted\",\"canonical_transitions\":[]}").RootElement;

        var payload = new TelemetryEventPayload
        {
            EventId = "0123456789abcdef0123456789abcdef",
            SessionId = "abcdef0123456789abcdef0123456789",
            ParticipantId = "P001",
            Condition = "B",
            ApplicationId = "sample.checkout",
            TaskId = "T02",
            RecordProvenance = TelemetryRecordProvenances.Participant,
            Event = "task_end",
            StateVersion = 9,
            Data = data,
        };

        Assert.True(payload.Data.HasValue);
        Assert.Equal("process_submitted", payload.Data.Value.GetProperty("end_state").GetString());
    }

    [Fact]
    public void Deserialize_WithUnknownProperty_IsRefusedRatherThanCollectedBeside()
    {
        const string json = """
            {
              "event_id": "0123456789abcdef0123456789abcdef",
              "session_id": "abcdef0123456789abcdef0123456789",
              "client_offset_ms": 0,
              "participant_id": "P001",
              "condition": "A",
              "application_id": "sample.checkout",
              "task_id": "T01",

              "record_provenance": "participant",
              "event": "task_start",
              "state_version": 0,
              "unexpected": "value"
            }
            """;

        // Unbekannte Eigenschaften werden beim Deserialisieren abgelehnt,
        // damit sie vor der Speicherung nicht unbemerkt verloren gehen.
        var exception = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<TelemetryEventPayload>(json, TelemetryEventPayload.ReadOptions));

        Assert.Contains("unexpected", exception.Message, StringComparison.Ordinal);
    }
}
