using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Serialisiert ein Telemetrieereignis und prüft die fertige Hülle gegen das kanonische Schema. Identitätsfelder werden dabei nicht gekürzt oder getrimmt. Die Sitzungskennung wird gesondert in ihr Übertragungsformat umgewandelt.
/// </summary>
internal sealed class EvaluationTelemetryEnvelopeSerializer(TelemetryEnvelopeSchemaValidator envelopeValidator)
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    private readonly TelemetryEnvelopeSchemaValidator _envelopeValidator =
        envelopeValidator ?? throw new ArgumentNullException(nameof(envelopeValidator));

    internal EvaluationTelemetrySerializedEnvelope Serialize(
        string eventId,
        EvaluationTelemetryEnvelopeIdentity identity,
        string eventName,
        int stateVersion,
        object? data,
        string? sessionId = null,
        int? clientOffsetMs = null)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var dataElement = data is null
            ? JsonSerializer.SerializeToElement(new { }, SerializerOptions)
            : JsonSerializer.SerializeToElement(data, SerializerOptions);

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString(TelemetryEnvelopeFieldNames.EventId, eventId);
            if (!string.IsNullOrEmpty(sessionId))
            {
                if (clientOffsetMs is not int offset || offset < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(clientOffsetMs));
                }

                writer.WriteString(TelemetryEnvelopeFieldNames.SessionId, ToWireSessionId(sessionId));
                writer.WriteNumber(TelemetryEnvelopeFieldNames.ClientOffsetMs, offset);
            }
            else if (clientOffsetMs is not null)
            {
                throw new ArgumentException(
                    "Outside-task envelopes refuse client_offset_ms.",
                    nameof(clientOffsetMs));
            }

            writer.WriteString(TelemetryEnvelopeFieldNames.ParticipantId, identity.ParticipantId);
            writer.WriteString(TelemetryEnvelopeFieldNames.Condition, identity.Condition);
            writer.WriteString(TelemetryEnvelopeFieldNames.ApplicationId, identity.ApplicationId);
            writer.WriteString(TelemetryEnvelopeFieldNames.TaskId, identity.TaskId);
            writer.WriteString(TelemetryEnvelopeFieldNames.RecordProvenance, identity.RecordProvenance);
            writer.WriteString(TelemetryEnvelopeFieldNames.Event, eventName);
            writer.WriteNumber(TelemetryEnvelopeFieldNames.StateVersion, stateVersion);
            writer.WritePropertyName(TelemetryEnvelopeFieldNames.Data);
            dataElement.WriteTo(writer);
            writer.WriteEndObject();
        }

        var body = Encoding.UTF8.GetString(buffer.ToArray());
        _envelopeValidator.Validate(body);

        return new EvaluationTelemetrySerializedEnvelope(eventId, eventName, stateVersion, body);
    }

    /// <summary>
    /// Bildet die Sitzungskennung auf 32 kleine Hexadezimalzeichen ab. Gültige Kennungen bleiben erhalten, GUIDs werden im N-Format geschrieben. Andere Zeichenketten ergeben ihren MD5-Hash.
    /// </summary>
    private static string ToWireSessionId(string sessionId)
    {
        if (sessionId.Length == 32)
        {
            var alreadyCanonical = true;
            foreach (var character in sessionId)
            {
                var isHex = character is >= '0' and <= '9' or >= 'a' and <= 'f';
                if (!isHex)
                {
                    alreadyCanonical = false;
                    break;
                }
            }

            if (alreadyCanonical)
            {
                return sessionId;
            }
        }

        if (Guid.TryParse(sessionId, out var guid))
        {
            return guid.ToString("N");
        }

        return Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(sessionId)));
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
