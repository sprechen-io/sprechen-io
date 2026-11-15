using System.Text.Json;
using Sprechen.EvaluationTelemetry.Ingestion;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Xunit;

namespace Sprechen.Forms.Tests.Services;

public sealed class EvaluationTelemetryEnvelopeBuilderTests
{
    private const string ReceiptTime = "2026-09-10T12:34:56.1234567+02:00";

    private readonly EvaluationTelemetryEnvelopeBuilder _sut =
        new(new TelemetryEnvelopeSchemaValidator());

    [Fact]
    public void Build_WithValidPayload_ProducesCanonicalJsonLine()
    {
        var envelope = _sut.Build(CreateValidPayload());

        Assert.True(envelope.Accepted);
        Assert.Equal("P003", envelope.ParticipantId);
        Assert.Equal("T01", envelope.TaskId);
        Assert.Equal("A", envelope.Condition);

        using var document = JsonDocument.Parse(envelope.JsonLine);
        var root = document.RootElement;
        Assert.Equal("0123456789abcdef0123456789abcdef", root.GetProperty("event_id").GetString());
        Assert.Equal("P003", root.GetProperty("participant_id").GetString());
        Assert.Equal("A", root.GetProperty("condition").GetString());
        Assert.Equal("sample.checkout", root.GetProperty("application_id").GetString());
        Assert.Equal("T01", root.GetProperty("task_id").GetString());
        Assert.Equal("task_start", root.GetProperty("event").GetString());
        Assert.Equal(1, root.GetProperty("state_version").GetInt32());
        Assert.Equal(JsonValueKind.Object, root.GetProperty("data").ValueKind);
    }

    [Fact]
    public void Build_RejectedPayload_NamesTheRuleThatRefusedIt()
    {
        // Ein Aufgabenstart erzeugt keine kanonischen Übergänge.
        // Ein aufgezeichneter Übergang muss daher den Fehler der Übergangsprüfung liefern.
        var invalid = CreateValidPayload() with
        {
            Data = JsonSerializer.Deserialize<JsonElement>(
                "{\"canonical_transitions\":[{\"canonical_class\":\"field_target_changed\","
                    + "\"scope\":\"field\",\"object_id\":\"f1\",\"resulting_state\":\"targeted\"}]}"),
        };

        var envelope = _sut.Build(invalid);

        Assert.False(envelope.Accepted);
        Assert.Equal(
            EvaluationTelemetryValidationErrorCodes.UnprojectedCanonicalTransitions,
            envelope.ErrorCode);
        Assert.Throws<InvalidOperationException>(() => envelope.LogRecord);
    }

    [Fact]
    public void Build_PayloadTheModelRefuses_FailsClosedInsteadOfWritingTheLine()
    {
        // Auch eine direkt übergebene ungültige Nutzlast muss bei der Prüfung der erzeugten Zeile scheitern.

        var invalid = CreateValidPayload() with { EventId = "not-a-transport-identifier" };

        var exception = Assert.Throws<JsonException>(() => _sut.Build(invalid));

        Assert.Contains("/event_id", exception.Message, StringComparison.Ordinal);
        Assert.Contains("pattern", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_JsonLine_HasNoEmbeddedNewlines()
    {
        var envelope = _sut.Build(CreateValidPayload());

        Assert.True(envelope.Accepted);
        Assert.DoesNotContain('\n', envelope.JsonLine);
        Assert.DoesNotContain('\r', envelope.JsonLine);
    }

    [Fact]
    public void Build_DoesNotRepairAConditionTheModelRefuses()
    {

        var exception = Assert.Throws<JsonException>(
            () => _sut.Build(CreateValidPayload() with { Condition = "b" }));

        Assert.Contains("/condition", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("  P003  ")]
    [InlineData("   ")]
    public void Build_DoesNotRepairAnIdentifierTheModelRefuses(string participantId)
    {


        var exception = Assert.Throws<JsonException>(
            () => _sut.Build(CreateValidPayload() with { ParticipantId = participantId }));

        Assert.Contains("/participant_id", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_DoesNotTruncateAnIdentifierTheModelRefuses()
    {

        var exception = Assert.Throws<JsonException>(
            () => _sut.Build(CreateValidPayload() with { ParticipantId = new string('p', 65) }));

        Assert.Contains("/participant_id", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_NullPayload_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Build(null!));
    }

    private static TelemetryEventPayload CreateValidPayload() => new()
    {
        EventId = "0123456789abcdef0123456789abcdef",
        SessionId = "abcdef0123456789abcdef0123456789",
        ClientOffsetMs = 0,
        ParticipantId = "P003",
        Condition = "A",
        ApplicationId = "sample.checkout",
        TaskId = "T01",
        RecordProvenance = TelemetryRecordProvenances.Participant,
        Event = "task_start",
        StateVersion = 1,
        RecordReceivedAt = ReceiptTime,
        Data = JsonSerializer.Deserialize<JsonElement>("{\"canonical_transitions\":[]}"),
    };
}
