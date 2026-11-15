using System.Text;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Prüft die aufgezeichneten Übergänge und erzeugt die kanonische JSONL-Zeile
/// für Speicherung und Protokollierung.
/// </summary>
internal sealed class EvaluationTelemetryEnvelopeBuilder
{
    private readonly TelemetryEnvelopeSchemaValidator _schemaValidator;
    private readonly EvaluationTelemetryCanonicalTransitionValidator _canonicalTransitionValidator = new();

    public EvaluationTelemetryEnvelopeBuilder(TelemetryEnvelopeSchemaValidator schemaValidator)
    {
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
    }

    /// <summary>
    /// Erzeugt die gespeicherte Zeile aus einer bereits gegen das Schema geprüften Nutzlast.
    /// Prüft zusätzlich, ob die aufgezeichneten Übergänge zu den Ereignisdaten passen.
    /// </summary>
    public EvaluationTelemetryEnvelopeBuildResult Build(TelemetryEventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (!_canonicalTransitionValidator.Validate(payload.Event, payload.Data))
        {
            return EvaluationTelemetryEnvelopeBuildResult.Reject(
                EvaluationTelemetryValidationErrorCodes.UnprojectedCanonicalTransitions);
        }


        ArgumentException.ThrowIfNullOrEmpty(payload.RecordReceivedAt, nameof(payload));

        var jsonLine = SerializeToJsonLine(payload, withRecordReceivedAt: true);

        // Auch die erzeugte Zeile wird geprüft, damit Fehler bei der Serialisierung
        // nicht als gültige Datensätze gespeichert werden.
        _schemaValidator.ValidateRecord(jsonLine);

        return EvaluationTelemetryEnvelopeBuildResult.Accept(
            jsonLine,
            payload.EventId,
            payload.ParticipantId,
            payload.TaskId,
            payload.Condition,
            EvaluationTelemetryLogRecord.For(payload));
    }

    /// <summary>
    /// Serialisiert die Ereignisdaten ohne serverseitigen Empfangszeitpunkt,
    /// damit wiederholte Zustellungen inhaltlich verglichen werden können.
    /// </summary>
    internal static string SerializeWithoutRecordReceivedAt(TelemetryEventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return SerializeToJsonLine(payload, withRecordReceivedAt: false);
    }

    private static string SerializeToJsonLine(TelemetryEventPayload payload, bool withRecordReceivedAt)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString(TelemetryEnvelopeFieldNames.EventId, payload.EventId);
            if (!string.IsNullOrEmpty(payload.SessionId))
            {
                if (payload.ClientOffsetMs is not int clientOffsetMs || clientOffsetMs < 0)
                {
                    throw new ArgumentException(
                        "Task-bound envelopes require client_offset_ms.",
                        nameof(payload));
                }

                writer.WriteString(TelemetryEnvelopeFieldNames.SessionId, payload.SessionId);
                writer.WriteNumber(TelemetryEnvelopeFieldNames.ClientOffsetMs, clientOffsetMs);
            }
            else if (payload.ClientOffsetMs is not null)
            {
                throw new ArgumentException(
                    "Outside-task envelopes refuse client_offset_ms.",
                    nameof(payload));
            }

            writer.WriteString(TelemetryEnvelopeFieldNames.ParticipantId, payload.ParticipantId);
            writer.WriteString(TelemetryEnvelopeFieldNames.Condition, payload.Condition);
            writer.WriteString(TelemetryEnvelopeFieldNames.ApplicationId, payload.ApplicationId);
            writer.WriteString(TelemetryEnvelopeFieldNames.TaskId, payload.TaskId);
            writer.WriteString(TelemetryEnvelopeFieldNames.RecordProvenance, payload.RecordProvenance);
            writer.WriteString(TelemetryEnvelopeFieldNames.Event, payload.Event);
            writer.WriteNumber(TelemetryEnvelopeFieldNames.StateVersion, payload.StateVersion);
            writer.WritePropertyName(TelemetryEnvelopeFieldNames.Data);
            payload.Data!.Value.WriteTo(writer);
            if (withRecordReceivedAt)
            {
                writer.WriteString(TelemetryRecordReceivedAt.MemberName, payload.RecordReceivedAt);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
