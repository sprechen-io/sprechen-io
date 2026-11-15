using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Prüft Inhalt, SHA-256-Prüfsumme und Identität gespeicherter Ereignisse, bevor ihre Übergänge beim Wiederanlauf übernommen werden.
/// </summary>
internal sealed class EvaluationTelemetryCheckpointEventValidator(
    TelemetryEnvelopeSchemaValidator envelopeValidator)
{
    private readonly TelemetryEnvelopeSchemaValidator _envelopeValidator =
        envelopeValidator ?? throw new ArgumentNullException(nameof(envelopeValidator));

    internal void ValidateSerializedCheckpoint(string serialized)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serialized);

        using var document = JsonDocument.Parse(serialized);
        EnsureNoDuplicateObjectMembers(document.RootElement);
    }

    internal EvaluationTelemetryCheckpointEventProjectionValidation Validate(
        EvaluationTelemetryStoredEvent storedEvent,
        EvaluationTelemetryEnvelopeIdentity identity)
    {
        EnsureEventMetadataShape(storedEvent);
        EnsureBodyFingerprint(storedEvent);

        _envelopeValidator.Validate(storedEvent.Body);
        using var document = JsonDocument.Parse(storedEvent.Body);
        var root = document.RootElement;
        EnsureBodyMatchesMetadataAndIdentity(storedEvent, identity, root);

        return ReadAndProjectTransitions(
            storedEvent.EventName,
            root.GetProperty(TelemetryEnvelopeFieldNames.Data));
    }

    private static void EnsureEventMetadataShape(EvaluationTelemetryStoredEvent storedEvent)
    {
        if (storedEvent is null
            || storedEvent.DeliveryAttempts is < 0 or int.MaxValue
            || storedEvent.StateVersion < 0
            || !TelemetryEventNames.All.Contains(storedEvent.EventName)
            || storedEvent.EventId is not { Length: 32 }
            || string.IsNullOrWhiteSpace(storedEvent.Body)
            || !storedEvent.EventId.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f'))
        {
            throw new JsonException("Evaluation checkpoint event metadata is invalid.");
        }
    }

    private static void EnsureBodyFingerprint(EvaluationTelemetryStoredEvent storedEvent)
    {
        var expectedBodySha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(storedEvent.Body)));
        if (storedEvent.BodySha256 is not { Length: 64 }
            || !storedEvent.BodySha256.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f')
            || !string.Equals(storedEvent.BodySha256, expectedBodySha256, StringComparison.Ordinal))
        {
            throw new JsonException("Evaluation checkpoint immutable event body fingerprint is invalid.");
        }
    }

    private static void EnsureBodyMatchesMetadataAndIdentity(
        EvaluationTelemetryStoredEvent storedEvent,
        EvaluationTelemetryEnvelopeIdentity identity,
        JsonElement root)
    {
        if (!ReadString(root, TelemetryEnvelopeFieldNames.EventId).Equals(storedEvent.EventId, StringComparison.Ordinal)
            || !ReadString(root, TelemetryEnvelopeFieldNames.Event).Equals(storedEvent.EventName, StringComparison.Ordinal)
            || ReadInt32(root, TelemetryEnvelopeFieldNames.StateVersion) != storedEvent.StateVersion
            || !ReadString(root, TelemetryEnvelopeFieldNames.ParticipantId).Equals(identity.ParticipantId, StringComparison.Ordinal)
            || !ReadString(root, TelemetryEnvelopeFieldNames.Condition).Equals(identity.Condition, StringComparison.Ordinal)
            || !ReadString(root, TelemetryEnvelopeFieldNames.ApplicationId).Equals(identity.ApplicationId, StringComparison.Ordinal)
            || !ReadString(root, TelemetryEnvelopeFieldNames.TaskId).Equals(identity.TaskId, StringComparison.Ordinal)
            || !ReadString(root, TelemetryEnvelopeFieldNames.RecordProvenance).Equals(
                identity.RecordProvenance,
                StringComparison.Ordinal))
        {
            throw new JsonException("Evaluation checkpoint body does not match its event metadata and identity.");
        }
    }

    private static void EnsureNoDuplicateObjectMembers(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new JsonException($"Evaluation checkpoint JSON member '{property.Name}' is duplicated.");
                }

                EnsureNoDuplicateObjectMembers(property.Value);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                EnsureNoDuplicateObjectMembers(item);
            }
        }
    }

    private static EvaluationTelemetryCheckpointEventProjectionValidation ReadAndProjectTransitions(
        string eventName,
        JsonElement data)
    {
        var recorded = ReadTransitions(data);
        var payload = ToProjectionPayload(data, recorded);
        IReadOnlyList<TelemetryCanonicalTransition> projected;
        try
        {
            projected = TelemetryCanonicalTransitionProjection.ForEvent(eventName, payload);
        }
        catch (ArgumentException exception)
        {
            throw new JsonException("Evaluation payload canonical transition projection is invalid.", exception);
        }

        return new EvaluationTelemetryCheckpointEventProjectionValidation(recorded, projected);
    }

    private static IReadOnlyList<TelemetryCanonicalTransition> ReadTransitions(JsonElement data)
    {
        if (!data.TryGetProperty(TelemetryPayloadProperties.CanonicalTransitions, out var transitionElement)
            || transitionElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Evaluation payload must contain canonical_transitions.");
        }

        return transitionElement.EnumerateArray().Select(ReadTransition).ToArray();
    }

    private static IReadOnlyDictionary<string, object?> ToProjectionPayload(
        JsonElement data,
        IReadOnlyList<TelemetryCanonicalTransition> recordedTransitions)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in data.EnumerateObject())
        {
            payload[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when property.Value.TryGetInt32(out var integer) => integer,
                JsonValueKind.Null => null,
                _ => property.Value,
            };
        }

        payload[TelemetryPayloadProperties.CanonicalTransitions] =
            recordedTransitions.Cast<object?>().ToArray();
        return payload;
    }

    private static TelemetryCanonicalTransition ReadTransition(JsonElement transition)
    {
        if (transition.ValueKind != JsonValueKind.Object
            || transition.EnumerateObject().Count() != 4)
        {
            throw new JsonException("A canonical transition must contain exactly four members.");
        }

        return new TelemetryCanonicalTransition(
            ReadString(transition, TelemetryPayloadProperties.CanonicalClass),
            ReadString(transition, TelemetryPayloadProperties.TransitionScope),
            ReadString(transition, TelemetryPayloadProperties.ObjectId),
            ReadString(transition, TelemetryPayloadProperties.ResultingState));
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String
            || property.GetString() is not { } value)
        {
            throw new JsonException($"Evaluation JSON member '{propertyName}' must be a string.");
        }

        return value;
    }

    private static int ReadInt32(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || !property.TryGetInt32(out var value))
        {
            throw new JsonException($"Evaluation JSON member '{propertyName}' must be an integer.");
        }

        return value;
    }
}
