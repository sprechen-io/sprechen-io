using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Telemetry;

public sealed class EvaluationTelemetryCheckpointEventValidatorTests
{

    [Fact]
    public void Validate_AcceptsACanonicalStoredEvent()
    {
        var validator = CreateValidator();
        var storedEvent = CreateStoredEvent(Identity());

        var validation = validator.Validate(storedEvent, Identity());

        Assert.Empty(validation.RecordedTransitions);
        Assert.Empty(validation.ProjectedTransitions);
    }

    [Theory]
    [InlineData("0123456789abcdef")]
    [InlineData("0123456789ABCDEF0123456789ABCDEF")]
    [InlineData("0123456789abcdef0123456789abcdeg")]
    public void Validate_RejectsMalformedEventMetadata(string eventId)
    {
        var validator = CreateValidator();
        var storedEvent = CreateStoredEvent(Identity()) with { EventId = eventId };

        var exception = Assert.Throws<JsonException>(() => validator.Validate(storedEvent, Identity()));

        Assert.Equal("Evaluation checkpoint event metadata is invalid.", exception.Message);
    }

    [Fact]
    public void Validate_RejectsAnUnknownEventName()
    {
        var validator = CreateValidator();
        var storedEvent = CreateStoredEvent(Identity()) with { EventName = "unknown_event" };

        var exception = Assert.Throws<JsonException>(() => validator.Validate(storedEvent, Identity()));

        Assert.Equal("Evaluation checkpoint event metadata is invalid.", exception.Message);
    }

    [Fact]
    public void Validate_RejectsAMismatchedBodyFingerprint()
    {
        var validator = CreateValidator();
        var storedEvent = CreateStoredEvent(Identity()) with
        {
            BodySha256 = new string('0', 64),
        };

        var exception = Assert.Throws<JsonException>(() => validator.Validate(storedEvent, Identity()));

        Assert.Equal(
            "Evaluation checkpoint immutable event body fingerprint is invalid.",
            exception.Message);
    }

    [Fact]
    public void Validate_RejectsABodyThatDoesNotMatchItsMetadataAndIdentity()
    {
        var validator = CreateValidator();
        var storedEvent = CreateStoredEvent(Identity());
        var otherIdentity = Identity() with { ParticipantId = "P999" };

        var exception = Assert.Throws<JsonException>(() => validator.Validate(storedEvent, otherIdentity));

        Assert.Equal(
            "Evaluation checkpoint body does not match its event metadata and identity.",
            exception.Message);
    }

    [Fact]
    public void Validate_RejectsAStoredBodyTheModelRefuses_NamingTheMemberAndTheRule()
    {
        var validator = CreateValidator();
        var storedEvent = CreateStoredEvent(Identity());
        var refusedBody = storedEvent.Body.Replace(
            "\"canonical_transitions\":[]",
            "\"canonical_transitions\":[],\"leftover\":1",
            StringComparison.Ordinal);
        var refused = storedEvent with
        {
            Body = refusedBody,
            BodySha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refusedBody))),
        };

        var exception = Assert.Throws<JsonException>(() => validator.Validate(refused, Identity()));

        // Bei einem gespeicherten Ereignis mit ungültigen Nutzdaten muss die Fehlermeldung
        // das betroffene Feld und die verletzte Schemaregel benennen.
        Assert.Contains("/data/leftover", exception.Message, StringComparison.Ordinal);
        Assert.Contains("additionalProperties", exception.Message, StringComparison.Ordinal);
    }

    private static EvaluationTelemetryCheckpointEventValidator CreateValidator()
        => new(new TelemetryEnvelopeSchemaValidator());

    private static EvaluationTelemetryEnvelopeIdentity Identity()
        => new("P123", "B", "test-app", "T01", TelemetryRecordProvenances.Participant);

    private static EvaluationTelemetryStoredEvent CreateStoredEvent(
        EvaluationTelemetryEnvelopeIdentity identity,
        int stateVersion = 1)
    {
        var serialized = new EvaluationTelemetryEnvelopeSerializer(new TelemetryEnvelopeSchemaValidator()).Serialize(
            "0123456789abcdef0123456789abcdef",
            identity,
            TelemetryEventNames.TaskStart,
            stateVersion,
            new Dictionary<string, object?>
            {
                [TelemetryPayloadProperties.CanonicalTransitions] = Array.Empty<object>(),
            },
            sessionId: "abcdef0123456789abcdef0123456789",
            clientOffsetMs: 0);
        return new EvaluationTelemetryStoredEvent(
            serialized.EventId,
            serialized.EventName,
            serialized.StateVersion,
            serialized.Body,
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(serialized.Body))),
            DeliveryAttempts: 0);
    }
}
