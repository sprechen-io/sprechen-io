using Sprechen.EvaluationTelemetry.Ingestion;

namespace Sprechen.EvaluationTelemetry.JsonlStore;

/// <summary>
/// Prüft die kanonische JSONL-Zeile und gleicht ihre Identitätsfelder
/// mit den Angaben des zu speichernden Datensatzes ab.
/// </summary>
internal sealed class EvaluationTelemetryLedgerRecordConsistencyValidator
{
    private readonly IEvaluationTelemetryLedgerRecordValidator _ledgerRecordValidator;

    public EvaluationTelemetryLedgerRecordConsistencyValidator(
        IEvaluationTelemetryLedgerRecordValidator ledgerRecordValidator)
    {
        _ledgerRecordValidator = ledgerRecordValidator;
    }

    internal void Validate(EvaluationTelemetryLedgerRecord ledgerRecord)
    {
        ArgumentNullException.ThrowIfNull(ledgerRecord);

        if (!IsLowerHexEventId(ledgerRecord.EventId))
        {
            throw new ArgumentException("The event identifier must be 32 lowercase hexadecimal characters.", nameof(ledgerRecord));
        }

        if (string.IsNullOrWhiteSpace(ledgerRecord.ParticipantId)
            || ledgerRecord.ParticipantId.Length > 64
            || string.IsNullOrWhiteSpace(ledgerRecord.TaskId)
            || ledgerRecord.TaskId.Length > 64
            || ledgerRecord.Condition is not ("A" or "B"))
        {
            throw new ArgumentException("The study identity is invalid.", nameof(ledgerRecord));
        }

        if (string.IsNullOrEmpty(ledgerRecord.JsonLine)
            || ledgerRecord.JsonLine.Contains('\n', StringComparison.Ordinal)
            || ledgerRecord.JsonLine.Contains('\r', StringComparison.Ordinal))
        {
            throw new ArgumentException("The serialized event must be exactly one JSONL line.", nameof(ledgerRecord));
        }

        var reading = _ledgerRecordValidator.ReadCanonicalLine(ledgerRecord.JsonLine);
        if (reading.Record is not { } record)
        {
            throw new ArgumentException(
                $"The serialized event is not a canonical envelope: {reading.Refusal}",
                nameof(ledgerRecord));
        }

        if (DescribeDeclaredIdentityMismatch(record, ledgerRecord) is { } mismatch)
        {
            throw new ArgumentException(
                $"The serialized event is canonical but not the envelope for its declared identity: {mismatch}",
                nameof(ledgerRecord));
        }
    }


    private static string? DescribeDeclaredIdentityMismatch(
        EvaluationTelemetryLedgerRecord serialized,
        EvaluationTelemetryLedgerRecord declared)
    {
        if (!string.Equals(serialized.EventId, declared.EventId, StringComparison.Ordinal))
        {
            return $"the envelope names event '{serialized.EventId}' and the record declares '{declared.EventId}'";
        }

        if (!string.Equals(serialized.ParticipantId, declared.ParticipantId, StringComparison.Ordinal))
        {
            return "the envelope and the record name different participants";
        }

        if (!string.Equals(serialized.TaskId, declared.TaskId, StringComparison.Ordinal))
        {
            return "the envelope and the record name different tasks";
        }

        if (!string.Equals(serialized.Condition, declared.Condition, StringComparison.Ordinal))
        {
            return $"the envelope names condition '{serialized.Condition}' and the record declares '{declared.Condition}'";
        }

        return null;
    }

    private static bool IsLowerHexEventId(string? value)
    {
        return value is { Length: 32 } && value.All(static character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');
    }
}
