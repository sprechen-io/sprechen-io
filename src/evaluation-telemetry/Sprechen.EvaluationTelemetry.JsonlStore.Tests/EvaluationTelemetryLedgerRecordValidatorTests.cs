using System.Text.Json;
using Sprechen.EvaluationTelemetry.Ingestion;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Xunit;

namespace Sprechen.EvaluationTelemetry.JsonlStore.Tests;

public sealed class EvaluationTelemetryLedgerRecordValidatorTests
{
    private const string EventId = "0123456789abcdef0123456789abcdef";

    private const string ReceiptTime = "2026-09-10T12:34:56.1234567+02:00";

    private static readonly TelemetryEnvelopeSchemaValidator SchemaValidator = new();

    private readonly EvaluationTelemetryLedgerRecordValidator _sut = new(
        SchemaValidator,
        new EvaluationTelemetryEnvelopeBuilder(SchemaValidator));

    [Fact]
    public void ReadCanonicalLine_CanonicalLine_ReadsTheRecordAndRefusesNothing()
    {
        var reading = _sut.ReadCanonicalLine(CanonicalLine());

        Assert.Equal(string.Empty, reading.Refusal);
        Assert.NotNull(reading.Record);
        Assert.Equal(EventId, reading.Record!.EventId);
        Assert.Equal("P001", reading.Record.ParticipantId);
        Assert.Equal("T01", reading.Record.TaskId);
        Assert.Equal("A", reading.Record.Condition);
    }

    [Fact]
    public void ReadCanonicalLine_LineWithALineBreak_NamesTheLineRuleAndTheIndex()
    {
        var line = CanonicalLine();
        var broken = line[..10] + "\n" + line[10..];

        var reading = _sut.ReadCanonicalLine(broken);

        Assert.Null(reading.Record);
        Assert.Equal("$ line: The evaluation telemetry ledger line carries a line break at index 10.", reading.Refusal);
    }

    [Fact]
    public void ReadCanonicalLine_CarriageReturn_IsRefusedByTheSameLineRule()
    {
        var reading = _sut.ReadCanonicalLine(CanonicalLine() + "\r");

        Assert.Null(reading.Record);
        Assert.StartsWith("$ line: ", reading.Refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadCanonicalLine_EmptyLine_IsRefusedByTheModelReading()
    {
        var reading = _sut.ReadCanonicalLine(string.Empty);

        Assert.Null(reading.Record);
        Assert.Equal("$ json: The evaluation telemetry envelope carries no text.", reading.Refusal);
    }

    [Fact]
    public void ReadCanonicalLine_UnreadableJson_NamesTheJsonRule()
    {
        var reading = _sut.ReadCanonicalLine("{not-json}");

        Assert.Null(reading.Record);
        Assert.StartsWith("$ json: ", reading.Refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadCanonicalLine_JsonNullLiteral_IsRefusedByTheModelAndNotByTheDeserializer()
    {

        var reading = _sut.ReadCanonicalLine("null");

        Assert.Null(reading.Record);
        Assert.StartsWith("$ json: ", reading.Refusal, StringComparison.Ordinal);
        Assert.Contains("not an object", reading.Refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadCanonicalLine_BrokenModeledRule_NamesTheMemberAndTheRule()
    {
        var reading = _sut.ReadCanonicalLine(
            CanonicalLine().Replace("\"state_version\":1", "\"state_version\":\"1\"", StringComparison.Ordinal));

        Assert.Null(reading.Record);
        Assert.StartsWith("/state_version type: ", reading.Refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadCanonicalLine_NoncanonicalSerializationOfACanonicalEnvelope_NamesTheSerializationRule()
    {
        var line = CanonicalLine();
        var spaced = line.Replace("{\"event_id\":", "{\"event_id\": ", StringComparison.Ordinal);
        Assert.Empty(SchemaValidator.DescribeRecordRefusal(spaced));

        var reading = _sut.ReadCanonicalLine(spaced);

        Assert.Null(reading.Record);
        Assert.Equal(
            "$ serialization: The evaluation telemetry ledger line differs from the canonical serialization "
                + "of what it carries at index 12.",
            reading.Refusal);
    }

    [Fact]
    public void ReadCanonicalLine_EveryWayALineCanBeRefused_NamesADistinctReason()
    {
        var canonical = CanonicalLine();
        string[] refusedLines =
        [
            canonical[..10] + "\n" + canonical[10..],
            string.Empty,
            "{not-json}",
            "null",
            canonical.Replace("\"state_version\":1", "\"state_version\":\"1\"", StringComparison.Ordinal),
            canonical.Replace("{\"event_id\":", "{\"event_id\": ", StringComparison.Ordinal),
            canonical.Replace("\"state_version\":1", "\"state_version\":999999999999", StringComparison.Ordinal),
        ];

        var refusals = refusedLines.Select(line => _sut.ReadCanonicalLine(line).Refusal).ToArray();

        Assert.All(refusals, refusal => Assert.NotEqual(string.Empty, refusal));
        Assert.Equal(refusedLines.Length, refusals.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ReadCanonicalLine_LineWithoutTheServersReceiptTime_IsRefusedByTheModelAtTheMember()
    {
        // Gespeicherte Zeilen brauchen den vom Server gemessenen Empfangszeitpunkt.
        // Ohne ihn darf die Zeile nicht als kanonischer Datensatz wiederhergestellt werden.
        var line = EvaluationTelemetryEnvelopeBuilder.SerializeWithoutRecordReceivedAt(CanonicalPayload());

        var reading = _sut.ReadCanonicalLine(line);

        Assert.Null(reading.Record);
        Assert.Contains(TelemetryRecordReceivedAt.MemberName, reading.Refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadCanonicalLine_CanonicalLine_ReadsASentLineWithoutTheReceiptTime()
    {
        var reading = _sut.ReadCanonicalLine(CanonicalLine());

        Assert.DoesNotContain(TelemetryRecordReceivedAt.MemberName, reading.SentLine, StringComparison.Ordinal);
        Assert.DoesNotContain(ReceiptTime, reading.SentLine, StringComparison.Ordinal);
        Assert.StartsWith("{\"event_id\":\"" + EventId + "\"", reading.SentLine, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadCanonicalLine_TwoLinesThatDifferOnlyInTheReceiptTime_ReadTheSameSentLine()
    {
        // Der Empfangszeitpunkt wird bei jedem Versuch vom Server gemessen.
        // Stimmen die übrigen Daten überein, handelt es sich um dieselbe Zustellung.
        var first = _sut.ReadCanonicalLine(CanonicalLine());
        var second = _sut.ReadCanonicalLine(CanonicalLine("2026-12-24T18:00:00.7654321+01:00"));

        Assert.NotEqual(first.Record!.JsonLine, second.Record!.JsonLine);
        Assert.Equal(first.SentLine, second.SentLine);
    }

    [Fact]
    public void ReadCanonicalLine_LineWithAnOldReceiptTime_ReadsBackByteIdenticalAndDoesNotRemeasure()
    {
        // Beim Lesen bleibt der ursprüngliche Empfangszeitpunkt erhalten.
        // Ein neuer Zeitstempel würde die zu prüfende Archivzeile verändern.
        var line = CanonicalLine("2024-03-01T08:00:00.0000001+01:00");

        var reading = _sut.ReadCanonicalLine(line);

        Assert.Equal(string.Empty, reading.Refusal);
        Assert.Equal(line, reading.Record!.JsonLine);
        Assert.Contains("2024-03-01T08:00:00.0000001", line, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadCanonicalLine_NullLine_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.ReadCanonicalLine(null!));
    }


    [Fact]
    public void ReadCanonicalLine_ModelAcceptsAndThisServerRefuses_NamesTheCodeOfTheRuleThatRefusedIt()
    {
        // Die zusätzliche Übergangsprüfung lehnt einen aufgezeichneten Übergang für task_start ab,
        // auch wenn die Zeile die Schemaprüfung besteht.
        var line = CanonicalLine().Replace(
            "\"canonical_transitions\":[]",
            "\"canonical_transitions\":[{\"canonical_class\":\"field_target_changed\",\"scope\":\"field\""
                + ",\"object_id\":\"f1\",\"resulting_state\":\"targeted\"}]",
            StringComparison.Ordinal);
        Assert.Empty(SchemaValidator.DescribeRecordRefusal(line));

        var reading = _sut.ReadCanonicalLine(line);

        Assert.Null(reading.Record);
        Assert.Equal(
            "$ payload: The evaluation telemetry ledger line is canonical for the model and refused by "
                + "'telemetry.canonical_transitions.unprojected'.",
            reading.Refusal);
    }

    [Fact]
    public void ReadCanonicalLine_ValueBeyondTheReadersRange_IsRefusedByTheModelAtTheMember()
    {

        var line = CanonicalLine().Replace(
            "\"state_version\":1",
            "\"state_version\":999999999999",
            StringComparison.Ordinal);

        var reading = _sut.ReadCanonicalLine(line);

        Assert.Null(reading.Record);
        Assert.Contains("/state_version", reading.Refusal, StringComparison.Ordinal);
        Assert.Contains("maximum", reading.Refusal, StringComparison.Ordinal);
    }

    private static string CanonicalLine(string receiptTime = ReceiptTime)
    {
        var envelope = new EvaluationTelemetryEnvelopeBuilder(SchemaValidator).Build(CanonicalPayload(receiptTime));

        Assert.True(envelope.Accepted);
        return envelope.JsonLine;
    }

    private static TelemetryEventPayload CanonicalPayload(string receiptTime = ReceiptTime)
    {
        return new TelemetryEventPayload
        {
            EventId = EventId,
            SessionId = "abcdef0123456789abcdef0123456789",
            ClientOffsetMs = 0,
            ParticipantId = "P001",
            Condition = "A",
            ApplicationId = "sample.checkout",
            TaskId = "T01",
            RecordProvenance = TelemetryRecordProvenances.Participant,
            Event = "task_start",
            StateVersion = 1,
            RecordReceivedAt = receiptTime,
            Data = JsonSerializer.SerializeToElement(new { canonical_transitions = Array.Empty<object>() }),
        };
    }
}
