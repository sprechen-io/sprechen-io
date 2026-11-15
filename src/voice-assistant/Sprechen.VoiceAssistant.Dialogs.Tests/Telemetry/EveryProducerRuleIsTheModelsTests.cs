using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Telemetry;

/// <summary>
/// Prüft ausgewählte ungültige Hüllwerte direkt beim Serialisieren. Die Ablehnung muss Member und Schemaregel nennen; ungültige Werte dürfen nicht stillschweigend normalisiert werden.
/// </summary>
public sealed class EveryProducerRuleIsTheModelsTests
{
    private const string CanonicalEventId = "0123456789abcdef0123456789abcdef";

    [Theory]
    [InlineData("participant_id", "pattern", " P123 ", "B", "test-app", "T01", TelemetryRecordProvenances.Participant, TelemetryEventNames.TaskStart, CanonicalEventId, 1)]
    [InlineData("task_id", "pattern", "P123", "B", "test-app", " T01 ", TelemetryRecordProvenances.Participant, TelemetryEventNames.TaskStart, CanonicalEventId, 1)]
    [InlineData("application_id", "pattern", "P123", "B", " test-app ", "T01", TelemetryRecordProvenances.Participant, TelemetryEventNames.TaskStart, CanonicalEventId, 1)]
    [InlineData("record_provenance", "enum", "P123", "B", "test-app", "T01", " participant ", TelemetryEventNames.TaskStart, CanonicalEventId, 1)]
    [InlineData("event", "const", "P123", "B", "test-app", "T01", TelemetryRecordProvenances.Participant, " task_start ", CanonicalEventId, 1)]
    [InlineData("participant_id", "maxLength", "PPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPP", "B", "test-app", "T01", TelemetryRecordProvenances.Participant, TelemetryEventNames.TaskStart, CanonicalEventId, 1)]
    [InlineData("task_id", "maxLength", "P123", "B", "test-app", "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT", TelemetryRecordProvenances.Participant, TelemetryEventNames.TaskStart, CanonicalEventId, 1)]
    [InlineData("application_id", "pattern", "P123", "B", "9app", "T01", TelemetryRecordProvenances.Participant, TelemetryEventNames.TaskStart, CanonicalEventId, 1)]
    [InlineData("condition", "enum", "P123", "b", "test-app", "T01", TelemetryRecordProvenances.Participant, TelemetryEventNames.TaskStart, CanonicalEventId, 1)]
    [InlineData("condition", "enum", "P123", " B ", "test-app", "T01", TelemetryRecordProvenances.Participant, TelemetryEventNames.TaskStart, CanonicalEventId, 1)]
    [InlineData("condition", "enum", "P123", "C", "test-app", "T01", TelemetryRecordProvenances.Participant, TelemetryEventNames.TaskStart, CanonicalEventId, 1)]
    [InlineData("state_version", "minimum", "P123", "B", "test-app", "T01", TelemetryRecordProvenances.Participant, TelemetryEventNames.TaskStart, CanonicalEventId, -1)]
    [InlineData("event_id", "pattern", "P123", "B", "test-app", "T01", TelemetryRecordProvenances.Participant, TelemetryEventNames.TaskStart, "0123456789ABCDEF0123456789abcdef", 1)]
    [InlineData("event_id", "minLength", "P123", "B", "test-app", "T01", TelemetryRecordProvenances.Participant, TelemetryEventNames.TaskStart, "0123456789abcdef0123456789abcde", 1)]
    [InlineData("event_id", "maxLength", "P123", "B", "test-app", "T01", TelemetryRecordProvenances.Participant, TelemetryEventNames.TaskStart, CanonicalEventId + "a", 1)]
    public void AMemberTheModelRefuses_IsRefusedByTheWriterAtThatMemberAndKeyword(
        string member,
        string keyword,
        string participantId,
        string condition,
        string applicationId,
        string taskId,
        string recordProvenance,
        string eventName,
        string eventId,
        int stateVersion)
    {
        var exception = Assert.Throws<JsonException>(() => Serialize(
            eventId,
            new EvaluationTelemetryEnvelopeIdentity(participantId, condition, applicationId, taskId, recordProvenance),
            eventName,
            stateVersion,
            CanonicalData()));

        Assert.Contains("/" + member + " " + keyword, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnmodeledDataMember_IsRefusedBeforeTheLineCanBeStored()
    {
        var data = CanonicalData();
        data["leftover"] = "x";

        var exception = Assert.Throws<JsonException>(() => Serialize(
            CanonicalEventId,
            CanonicalIdentity(),
            TelemetryEventNames.TaskStart,
            1,
            data));

        Assert.Contains("/data/leftover additionalProperties", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEventNameTheModelDoesNotDefine_IsRefusedAndNamed()
    {
        var exception = Assert.Throws<JsonException>(() => Serialize(
            CanonicalEventId,
            CanonicalIdentity(),
            "not_an_event",
            1,
            CanonicalData()));

        Assert.Contains("not_an_event", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheCanonicalEnvelope_IsWrittenExactlyAsItWasHanded()
    {
        var serialized = Serialize(
            CanonicalEventId,
            CanonicalIdentity(),
            TelemetryEventNames.TaskStart,
            1,
            CanonicalData());

        using var document = JsonDocument.Parse(serialized.Body);
        Assert.Equal("P123", document.RootElement.GetProperty(TelemetryEnvelopeFieldNames.ParticipantId).GetString());
        Assert.Equal("T01", document.RootElement.GetProperty(TelemetryEnvelopeFieldNames.TaskId).GetString());
        Assert.Equal(CanonicalEventId, serialized.EventId);
        Assert.Equal(TelemetryEventNames.TaskStart, serialized.EventName);
    }

    private static EvaluationTelemetrySerializedEnvelope Serialize(
        string eventId,
        EvaluationTelemetryEnvelopeIdentity identity,
        string eventName,
        int stateVersion,
        object data)
    {
        return new EvaluationTelemetryEnvelopeSerializer(new TelemetryEnvelopeSchemaValidator()).Serialize(
            eventId,
            identity,
            eventName,
            stateVersion,
            data,
            sessionId: "abcdef0123456789abcdef0123456789",
            clientOffsetMs: 0);
    }

    private static EvaluationTelemetryEnvelopeIdentity CanonicalIdentity()
        => new("P123", "B", "test-app", "T01", TelemetryRecordProvenances.Participant);

    private static Dictionary<string, object?> CanonicalData()
        => new(StringComparer.Ordinal)
        {
            [TelemetryPayloadProperties.CanonicalTransitions] = Array.Empty<object>(),
        };
}
