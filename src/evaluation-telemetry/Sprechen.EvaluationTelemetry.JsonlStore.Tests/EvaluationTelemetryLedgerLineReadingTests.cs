using Sprechen.EvaluationTelemetry.Ingestion;
using Xunit;

namespace Sprechen.EvaluationTelemetry.JsonlStore.Tests;

public sealed class EvaluationTelemetryLedgerLineReadingTests
{
    [Fact]
    public void Read_Record_RefusesNothing()
    {
        var record = new EvaluationTelemetryLedgerRecord("e", "P001", "T01", "A", "{}");

        var reading = EvaluationTelemetryLedgerLineReading.Read(record, () => "{}");

        Assert.Same(record, reading.Record);
        Assert.Equal(string.Empty, reading.Refusal);
        Assert.Equal("{}", reading.SentLine);
    }

    [Fact]
    public void Read_SentLine_IsRenderedOnlyWhenItIsAskedFor()
    {
        // Die Vergleichsform wird erst bei Bedarf serialisiert;
        // das Lesen des Datensatzes allein darf diese Arbeit nicht auslösen.
        var renderCount = 0;
        var reading = EvaluationTelemetryLedgerLineReading.Read(
            new EvaluationTelemetryLedgerRecord("e", "P001", "T01", "A", "{}"),
            () =>
            {
                renderCount++;
                return "{}";
            });

        Assert.Equal(0, renderCount);
        Assert.Equal("{}", reading.SentLine);
        Assert.Equal(1, renderCount);
    }

    [Fact]
    public void Refused_SentLine_Throws()
    {
        var reading = EvaluationTelemetryLedgerLineReading.Refused("/state_version", "type", "Value is a string.");

        Assert.Throws<InvalidOperationException>(() => reading.SentLine);
    }

    [Fact]
    public void Refused_PlaceRuleAndReason_ReadsInTheShapeTheSchemaOwnerUses()
    {
        var reading = EvaluationTelemetryLedgerLineReading.Refused("/state_version", "type", "Value is a string.");

        Assert.Null(reading.Record);
        Assert.Equal("/state_version type: Value is a string.", reading.Refusal);
    }

    [Fact]
    public void Refused_ReasonAcrossSeveralLines_StaysOneLine()
    {
        // Der Ablehnungsgrund wird zusammen mit Dateipfad und Zeilennummer ausgegeben.
        // Eigene Zeilenumbrüche würden diese Zuordnung in der Meldung auseinanderreißen.
        var reading = EvaluationTelemetryLedgerLineReading.Refused(
            "$",
            "payload",
            "Not convertible.\r\n  LineNumber: 0\n  BytePositionInLine: 268.");

        Assert.Equal(
            "$ payload: Not convertible. LineNumber: 0 BytePositionInLine: 268.",
            reading.Refusal);
    }

    [Fact]
    public void Refused_NothingToName_Throws()
    {
        Assert.Throws<ArgumentException>(() => EvaluationTelemetryLedgerLineReading.Refused("   "));
    }

    [Fact]
    public void Read_NullRecord_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => EvaluationTelemetryLedgerLineReading.Read(null!, () => "{}"));
    }

    [Fact]
    public void Read_NullSentLine_Throws()
    {
        var record = new EvaluationTelemetryLedgerRecord("e", "P001", "T01", "A", "{}");

        Assert.Throws<ArgumentNullException>(
            () => EvaluationTelemetryLedgerLineReading.Read(record, null!));
    }
}
