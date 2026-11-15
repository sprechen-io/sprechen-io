using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

using EvaluationTelemetryImpl = Sprechen.VoiceAssistant.EvaluationTelemetry.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Telemetry;

/// <summary>
/// Prüft ausgewählte ungültige Identitätswerte gegen das kanonische Hüllschema beim Wiederanlauf. Für operational_probe kommt die Bindung an einen aktuell akzeptierten Prüfkontext hinzu.
/// </summary>
public sealed class EveryCheckpointIdentityRuleIsTheModelsTests
{
    private static readonly JsonSerializerOptions CheckpointSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    [Theory]
    [InlineData("participant_id", "pattern", " P123 ")]
    [InlineData("participant_id", "minLength", "")]
    [InlineData("task_id", "pattern", " T01 ")]
    [InlineData("application_id", "pattern", " test-app ")]
    [InlineData("record_provenance", "enum", " participant ")]
    [InlineData("condition", "enum", "b")]
    [InlineData("condition", "enum", "C")]
    [InlineData("participant_id", "maxLength", "PPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPP")]
    [InlineData("task_id", "maxLength", "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT")]
    [InlineData("application_id", "pattern", "9app")]
    // Zwei Segmente halten das Muster gültig, damit dieser Fall gezielt die Längenregel verletzt.
    [InlineData("application_id", "maxLength", "a.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("application_id", "pattern", "onesegment")]
    [InlineData("application_id", "pattern", "Sample.Complex")]
    public async Task AnIdentityMemberTheModelRefuses_IsRefusedAtThatMemberAndKeyword(
        string member,
        string keyword,
        string value)
    {
        var (validator, storageKey, checkpoint) = await RestoreWithIdentityMemberAsync(member, value);

        var exception = Assert.Throws<JsonException>(() => validator.Validate(storageKey, checkpoint));

        Assert.Contains("/" + member + " " + keyword, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheProbeOriginClass_WithoutContextBinding_IsRefused()
    {
        var (validator, storageKey, checkpoint) = await RestoreWithIdentityMemberAsync(
            TelemetryEnvelopeFieldNames.RecordProvenance,
            TelemetryRecordProvenances.OperationalProbe);

        var exception = Assert.Throws<JsonException>(() => validator.Validate(storageKey, checkpoint));

        Assert.Contains("operational identity is not currently bound", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheCanonicalCheckpoint_IsRestoredUnchanged()
    {
        var (validator, storageKey, checkpoint) = await RestoreWithIdentityMemberAsync(null, null);

        validator.Validate(storageKey, checkpoint);

        Assert.Equal("P123", checkpoint.Identity.ParticipantId);
    }

    private static async Task<(EvaluationTelemetryCheckpointValidator Validator, string StorageKey, EvaluationTelemetryOutboxCheckpoint Checkpoint)>
        RestoreWithIdentityMemberAsync(string? member, string? value)
    {
        var serialized = await PersistActiveCheckpointAsync();
        var checkpoint = JsonNode.Parse(serialized)!.AsObject();
        if (member is not null)
        {
            var identity = checkpoint["identity"]!.AsObject();
            identity[member] = value;
            foreach (var collectionName in new[] { "acknowledged_events", "pending_events" })
            {
                foreach (var storedEvent in checkpoint[collectionName]!.AsArray())
                {
                    RewriteBodyMember(storedEvent!.AsObject(), member, value!);
                }
            }
        }

        var storageKey = EvaluationTelemetryOutboxKeys.BuildTaskStorageKey(
            checkpoint["session_id"]!.GetValue<string>(),
            ReadIdentity(checkpoint));
        checkpoint["storage_key"] = storageKey;

        return (
            new EvaluationTelemetryCheckpointValidator(
                new EvaluationSessionTelemetryPayloads(),
                new EvaluationTelemetryCheckpointEventValidator(new TelemetryEnvelopeSchemaValidator()), new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession()),
            storageKey,
            JsonSerializer.Deserialize<EvaluationTelemetryOutboxCheckpoint>(
                checkpoint.ToJsonString(),
                CheckpointSerializerOptions)!);
    }

    private static EvaluationTelemetryEnvelopeIdentity ReadIdentity(JsonObject checkpoint)
    {
        var identity = checkpoint["identity"]!.AsObject();
        return new EvaluationTelemetryEnvelopeIdentity(
            identity[TelemetryEnvelopeFieldNames.ParticipantId]!.GetValue<string>(),
            identity[TelemetryEnvelopeFieldNames.Condition]!.GetValue<string>(),
            identity[TelemetryEnvelopeFieldNames.ApplicationId]!.GetValue<string>(),
            identity[TelemetryEnvelopeFieldNames.TaskId]!.GetValue<string>(),
            identity[TelemetryEnvelopeFieldNames.RecordProvenance]!.GetValue<string>());
    }

    private static void RewriteBodyMember(JsonObject storedEvent, string member, string value)
    {
        var envelope = JsonNode.Parse(storedEvent["body"]!.GetValue<string>())!.AsObject();
        envelope[member] = value;
        var body = envelope.ToJsonString();
        storedEvent["body"] = body;
        storedEvent["body_sha256"] = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
    }

    private static async Task<string> PersistActiveCheckpointAsync()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var outbox = EvaluationTelemetryTestOutboxFactory.Create(
            new StudyTelemetryContext(),
            new CapturingLogger<EvaluationTelemetryImpl>(),
            store,
            new RecordingEvaluationTelemetryDeliveryTransport());
        _ = await outbox.StartTaskAsync(
            new EvaluationSession("run-1", EvaluationChannel.Voice, "test-app", "form-1"),
            new EvaluationTelemetryEnvelopeIdentity("P123", "B", "test-app", "T01", TelemetryRecordProvenances.Participant),
            new Dictionary<string, object?>
            {
                [TelemetryPayloadProperties.CanonicalTransitions] = Array.Empty<object>(),
            },
            CancellationToken.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (JsonNode.Parse(Assert.Single(store.Checkpoints.Values))!.AsObject()["acknowledged_events"]!.AsArray().Count != 1)
        {
            await Task.Delay(10, timeout.Token);
        }

        outbox.Dispose();
        return Assert.Single(store.Checkpoints.Values);
    }
}
