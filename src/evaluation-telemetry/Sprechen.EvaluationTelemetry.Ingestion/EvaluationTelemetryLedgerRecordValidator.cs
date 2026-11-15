using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Prüft gespeicherte JSONL-Zeilen auf gültigen Inhalt und kanonische Serialisierung.
/// Ablehnungen nennen die betroffene Stelle und Regel für Archivierung und Wiederherstellung.
/// </summary>
internal sealed class EvaluationTelemetryLedgerRecordValidator : IEvaluationTelemetryLedgerRecordValidator
{
    private const string RootInstanceLocation = "$";

    /// <summary>
    /// Fehlerkategorie für einen Zeilenumbruch innerhalb eines JSONL-Datensatzes.
    /// </summary>
    private const string LineKeyword = "line";

    /// <summary>
    /// Fehlerkategorie für eine Nutzlast, die der Server nicht verarbeiten kann.
    /// </summary>
    private const string PayloadKeyword = "payload";

    /// <summary>Fehlerkategorie für Abweichungen von der kanonischen Serialisierung des Servers.</summary>
    private const string SerializationKeyword = "serialization";

    private readonly TelemetryEnvelopeSchemaValidator _schemaValidator;
    private readonly EvaluationTelemetryEnvelopeBuilder _envelopeBuilder;

    public EvaluationTelemetryLedgerRecordValidator(
        TelemetryEnvelopeSchemaValidator schemaValidator,
        EvaluationTelemetryEnvelopeBuilder envelopeBuilder)
    {
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        _envelopeBuilder = envelopeBuilder ?? throw new ArgumentNullException(nameof(envelopeBuilder));
    }

    public EvaluationTelemetryLedgerLineReading ReadCanonicalLine(string jsonLine)
    {
        ArgumentNullException.ThrowIfNull(jsonLine);

        var lineBreak = jsonLine.AsSpan().IndexOfAny('\n', '\r');
        if (lineBreak >= 0)
        {
            return EvaluationTelemetryLedgerLineReading.Refused(
                RootInstanceLocation,
                LineKeyword,
                $"The evaluation telemetry ledger line carries a line break at index {lineBreak}.");
        }

        // Die Schemaprüfung liefert auch für leeren oder ungültigen JSON-Text einen konkreten Fehler.
        var schemaRefusal = _schemaValidator.DescribeRecordRefusal(jsonLine);
        if (schemaRefusal.Length > 0)
        {
            return EvaluationTelemetryLedgerLineReading.Refused(schemaRefusal);
        }

        TelemetryEventPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TelemetryEventPayload>(jsonLine, TelemetryEventPayload.ReadOptions);
        }
        catch (JsonException exception)
        {
            return EvaluationTelemetryLedgerLineReading.Refused(
                RootInstanceLocation,
                PayloadKeyword,
                exception.Message);
        }

        if (payload is null)
        {
            return EvaluationTelemetryLedgerLineReading.Refused(
                RootInstanceLocation,
                PayloadKeyword,
                "The evaluation telemetry ledger line reads as no payload though the model accepted it as an object.");
        }

        EvaluationTelemetryEnvelopeBuildResult envelope;
        try
        {
            envelope = _envelopeBuilder.Build(payload);
        }
        catch (JsonException exception)
        {
            return EvaluationTelemetryLedgerLineReading.Refused(
                RootInstanceLocation,
                SerializationKeyword,
                exception.Message);
        }

        if (!envelope.Accepted)
        {
            // Die zusätzliche Übergangsprüfung hat einen eigenen Fehlercode,
            // der die Ablehnung eines schema-gültigen Datensatzes erklärt.
            return EvaluationTelemetryLedgerLineReading.Refused(
                RootInstanceLocation,
                PayloadKeyword,
                $"The evaluation telemetry ledger line is canonical for the model and refused by '{envelope.ErrorCode}'.");
        }

        if (!string.Equals(envelope.JsonLine, jsonLine, StringComparison.Ordinal))
        {
            return EvaluationTelemetryLedgerLineReading.Refused(
                RootInstanceLocation,
                SerializationKeyword,
                $"The evaluation telemetry ledger line differs from the canonical serialization of what it carries at index {FirstDifferenceIndex(envelope.JsonLine, jsonLine)}.");
        }

        return EvaluationTelemetryLedgerLineReading.Read(
            new EvaluationTelemetryLedgerRecord(
                envelope.EventId,
                envelope.ParticipantId,
                envelope.TaskId,
                envelope.Condition,
                envelope.JsonLine),
            () => EvaluationTelemetryEnvelopeBuilder.SerializeWithoutRecordReceivedAt(payload));
    }

    private static int FirstDifferenceIndex(string canonical, string stored)
    {
        var shared = Math.Min(canonical.Length, stored.Length);
        for (var index = 0; index < shared; index++)
        {
            if (canonical[index] != stored[index])
            {
                return index;
            }
        }

        return shared;
    }
}
