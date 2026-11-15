using System.Text.Json;
using System.Text.Json.Nodes;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.Mda.Contracts.Tests.Telemetry;

public sealed class TelemetryEnvelopeSchemaValidatorTests
{
    private const string CanonicalReceiptTime = "2026-09-10T12:34:56.1234567+02:00";

    private readonly TelemetryEnvelopeSchemaValidator _sut = new();

    [Fact]
    public void CollectRefusals_NavigationAndErrorEnvelopes_AcceptsTheirCompleteCanonicalData()
    {
        var navigation = CreateEnvelope(
            TelemetryEventNames.NavigationChanged,
            new
            {
                current_field_id = "contactEmail",
                current_field_group_id = "contact",
                navigation_index = 1,
                navigation_total = 3,
                canonical_transitions = new[]
                {
                    new
                    {
                        canonical_class = "field_target_changed",
                        scope = "field",
                        object_id = "contactEmail",
                        resulting_state = "targeted",
                    },
                },
            });
        var error = CreateEnvelope(
            TelemetryEventNames.ValidationErrorOpened,
            new
            {
                error_code = TelemetryStudyDiagnosisCodes.Validation,
                severity = "error",
                scope = "field",
                field_id = "contactEmail",
                related_field_ids = new[] { "contactPhone" },
                trigger = TelemetryErrorTriggers.UserInput,
                canonical_transitions = Array.Empty<object>(),
            });

        Assert.Empty(_sut.CollectRefusals(navigation));
        Assert.Empty(_sut.CollectRefusals(error));
    }

    [Theory]
    [InlineData("extra")]
    [InlineData("missing")]
    [InlineData("wrong_type")]
    public void CollectRefusals_NavigationEnvelopeWithNoncanonicalData_FailsClosed(string corruption)
    {
        Assert.NotEmpty(_sut.CollectRefusals(CreateCorruptedNavigationEnvelope(corruption)));
    }

    [Theory]
    [InlineData("extra", "/data/legacy_position", "unevaluatedProperties")]
    [InlineData("missing", "/data", "required")]
    [InlineData("wrong_type", "/data/navigation_index", "type")]
    public void CollectRefusals_NavigationEnvelopeWithNoncanonicalData_NamesTheMemberAndTheBrokenRuleAndNothingElse(
        string corruption,
        string expectedInstanceLocation,
        string expectedKeyword)
    {
        var refusal = Assert.Single(_sut.CollectRefusals(CreateCorruptedNavigationEnvelope(corruption)));

        Assert.Equal(expectedInstanceLocation, refusal.InstanceLocation);
        Assert.Equal(expectedKeyword, refusal.Keyword);
        Assert.NotEmpty(refusal.Message);
    }

    [Fact]
    public void CollectRefusals_EnvelopeWithAnUnknownRootMember_NamesThatMemberAndNotTheProvenanceMatrix()
    {
        var envelope = JsonNode.Parse(CreateEnvelope(
            TelemetryEventNames.TaskStart,
            new { canonical_transitions = Array.Empty<object>() }))!.AsObject();
        envelope["leftover"] = "x";

        var refusal = Assert.Single(_sut.CollectRefusals(envelope.ToJsonString()));

        Assert.Equal("/leftover", refusal.InstanceLocation);
    }

    [Fact]
    public void CollectRefusals_EnvelopeNamingAnEventTheModelDoesNotDefine_RefusesTheEventAndNotItsData()
    {
        var envelope = JsonNode.Parse(CreateEnvelope(
            TelemetryEventNames.TaskStart,
            new { canonical_transitions = Array.Empty<object>() }))!.AsObject();
        envelope[TelemetryEnvelopeFieldNames.Event] = "task_begun";

        var refusal = Assert.Single(_sut.CollectRefusals(envelope.ToJsonString()));

        Assert.Equal("/event", refusal.InstanceLocation);
        Assert.Contains("task_begun", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CollectRefusals_TaskBoundEnvelopeWithoutSessionId_RefusesTheSessionMember()
    {
        var envelope = JsonNode.Parse(CreateEnvelope(
            TelemetryEventNames.TaskStart,
            new { canonical_transitions = Array.Empty<object>() }))!.AsObject();
        envelope.Remove(TelemetryEnvelopeFieldNames.SessionId);

        var refusal = Assert.Single(_sut.CollectRefusals(envelope.ToJsonString()));

        Assert.Equal("required", refusal.Keyword);
        Assert.Contains(TelemetryEnvelopeFieldNames.SessionId, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CollectRefusals_TaskBoundEnvelopeWithoutClientOffset_RefusesTheOffsetMember()
    {
        var envelope = JsonNode.Parse(CreateEnvelope(
            TelemetryEventNames.TaskStart,
            new { canonical_transitions = Array.Empty<object>() }))!.AsObject();
        envelope.Remove(TelemetryEnvelopeFieldNames.ClientOffsetMs);

        var refusal = Assert.Single(_sut.CollectRefusals(envelope.ToJsonString()));

        Assert.Equal("required", refusal.Keyword);
        Assert.Contains(TelemetryEnvelopeFieldNames.ClientOffsetMs, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CollectRefusals_OutsideTaskEnvelopeWithSessionId_RefusesTheSessionMember()
    {
        var envelope = JsonNode.Parse(CreateEnvelope(
            TelemetryEventNames.TaskStart,
            new { canonical_transitions = Array.Empty<object>() }))!.AsObject();
        envelope[TelemetryEnvelopeFieldNames.ApplicationId] = TelemetryEnvelopeContextDefaults.UnknownApplicationId;
        envelope[TelemetryEnvelopeFieldNames.TaskId] = TelemetryEnvelopeContextDefaults.UnknownTaskId;
        envelope.Remove(TelemetryEnvelopeFieldNames.ClientOffsetMs);

        var refusal = Assert.Single(_sut.CollectRefusals(envelope.ToJsonString()));

        Assert.Equal($"/{TelemetryEnvelopeFieldNames.SessionId}", refusal.InstanceLocation);
    }

    [Fact]
    public void CollectRefusals_OutsideTaskEnvelopeWithClientOffset_RefusesTheOffsetMember()
    {
        var envelope = JsonNode.Parse(CreateEnvelope(
            TelemetryEventNames.TaskStart,
            new { canonical_transitions = Array.Empty<object>() }))!.AsObject();
        envelope[TelemetryEnvelopeFieldNames.ApplicationId] = TelemetryEnvelopeContextDefaults.UnknownApplicationId;
        envelope[TelemetryEnvelopeFieldNames.TaskId] = TelemetryEnvelopeContextDefaults.UnknownTaskId;
        envelope.Remove(TelemetryEnvelopeFieldNames.SessionId);

        var refusal = Assert.Single(_sut.CollectRefusals(envelope.ToJsonString()));

        Assert.Equal($"/{TelemetryEnvelopeFieldNames.ClientOffsetMs}", refusal.InstanceLocation);
    }

    [Fact]
    public void CollectRefusals_OutsideTaskEnvelopeWithoutSessionId_Accepts()
    {
        var envelope = JsonNode.Parse(CreateEnvelope(
            TelemetryEventNames.TaskStart,
            new { canonical_transitions = Array.Empty<object>() }))!.AsObject();
        envelope[TelemetryEnvelopeFieldNames.ApplicationId] = TelemetryEnvelopeContextDefaults.UnknownApplicationId;
        envelope[TelemetryEnvelopeFieldNames.TaskId] = TelemetryEnvelopeContextDefaults.UnknownTaskId;
        envelope.Remove(TelemetryEnvelopeFieldNames.SessionId);
        envelope.Remove(TelemetryEnvelopeFieldNames.ClientOffsetMs);

        Assert.Empty(_sut.CollectRefusals(envelope.ToJsonString()));
    }

    [Fact]
    public void CollectRefusals_EnvelopeWithoutAnEvent_RefusesTheMemberThatSelectsTheDefinition()
    {
        var envelope = JsonNode.Parse(CreateEnvelope(
            TelemetryEventNames.TaskStart,
            new { canonical_transitions = Array.Empty<object>() }))!.AsObject();
        envelope.Remove(TelemetryEnvelopeFieldNames.Event);

        var refusal = Assert.Single(_sut.CollectRefusals(envelope.ToJsonString()));

        Assert.Equal("/event", refusal.InstanceLocation);
    }

    [Fact]
    public void EnvelopeSchema_ConstrainsAnEnvelopeThroughNothingButTheChoiceItsEventSelectsAndTheDispositionOfItsDirection()
    {
        // Der Validator prüft die durch event gewählte Variante und die zusätzlichen Regeln
        // für gesendete beziehungsweise gespeicherte Datensätze getrennt. Weitere äußere
        // Schemaschlüssel dürfen dabei nicht unbemerkt außerhalb dieser Prüfung bleiben.
        using var schema = JsonDocument.Parse(MdaEmbeddedRuntimeArtifacts.ReadAllBytes(
            "evaluationsmodell/schemas/telemetry-events.schema.json"));

        Assert.Equal(
            new[] { "$defs", "$id", "$ref", "$schema", "description", "title" },
            MemberNamesOf(schema.RootElement));

        var definitions = schema.RootElement.GetProperty("$defs");
        var sentShapeName = schema.RootElement.GetProperty("$ref").GetString()!["#/$defs/".Length..];
        var variantsName = AssertShapeStatesOnlyItsVariantsAndOneDisposition(definitions, sentShapeName);
        Assert.Equal(new[] { "description", "oneOf" }, MemberNamesOf(definitions.GetProperty(variantsName)));

        var storedShapeNames = definitions.EnumerateObject()
            .Where(definition => definition.Name != sentShapeName
                && definition.Value.TryGetProperty("allOf", out var parts)
                && parts.GetArrayLength() > 0
                && parts[0].TryGetProperty("$ref", out var reference)
                && reference.GetString() == "#/$defs/" + variantsName)
            .Select(static definition => definition.Name)
            .ToArray();

        var storedShapeName = Assert.Single(storedShapeNames);
        Assert.Equal(
            variantsName,
            AssertShapeStatesOnlyItsVariantsAndOneDisposition(definitions, storedShapeName));
    }

    private static string AssertShapeStatesOnlyItsVariantsAndOneDisposition(
        JsonElement definitions,
        string shapeName)
    {
        var shape = definitions.GetProperty(shapeName);
        Assert.Equal(new[] { "allOf", "description" }, MemberNamesOf(shape));

        var parts = shape.GetProperty("allOf");
        Assert.Equal(2, parts.GetArrayLength());
        Assert.Equal(new[] { "$ref" }, MemberNamesOf(parts[0]));

        return parts[0].GetProperty("$ref").GetString()!["#/$defs/".Length..];
    }

    [Fact]
    public void DefinedEventNames_CoverEveryModeledEventNameExactlyOnce()
    {
        Assert.Equal(
            TelemetryEventNames.All.OrderBy(static name => name, StringComparer.Ordinal),
            TelemetryEnvelopeSchemaValidator.DefinedEventNames.OrderBy(static name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void CollectRefusals_ErrorEnvelopeWithExtraData_FailsClosed()
    {
        var envelope = JsonNode.Parse(CreateEnvelope(
            TelemetryEventNames.NavigationError,
            new
            {
                error_code = TelemetryStudyDiagnosisCodes.Navigation,
                severity = "error",
                scope = "form",
                canonical_transitions = Array.Empty<object>(),
            }))!.AsObject();
        envelope[TelemetryEnvelopeFieldNames.Data]!["exception_message"] = "private detail";

        Assert.NotEmpty(_sut.CollectRefusals(envelope.ToJsonString()));
    }

    [Fact]
    public void CollectRefusals_DuplicateNestedDataMember_NamesTheMemberThatIsStatedTwice()
    {
        var valid = CreateEnvelope(
            TelemetryEventNames.FocusChange,
            new { field_id = "contactEmail", canonical_transitions = Array.Empty<object>() });
        var duplicate = valid.Replace(
            "\"field_id\":\"contactEmail\"",
            "\"field_id\":\"contactEmail\",\"field_id\":\"contactPhone\"",
            StringComparison.Ordinal);

        var refusal = Assert.Single(_sut.CollectRefusals(duplicate));
        Assert.Equal("/data/field_id", refusal.InstanceLocation);
        Assert.Equal("duplicateMember", refusal.Keyword);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{not json")]
    [InlineData("[]")]
    [InlineData("\"an envelope\"")]
    public void CollectRefusals_UnreadableEnvelope_NamesTheReadingInsteadOfAModeledRule(string envelope)
    {
        var refusal = Assert.Single(_sut.CollectRefusals(envelope));

        Assert.Equal("$", refusal.InstanceLocation);
        Assert.Equal("json", refusal.Keyword);
        Assert.NotEmpty(refusal.Message);
    }

    [Fact]
    public void CollectRefusals_EnvelopeIdentityUsesExactCanonicalBoundsAndPatterns()
    {
        var data = new { canonical_transitions = Array.Empty<object>() };
        Assert.Empty(_sut.CollectRefusals(
            CreateEnvelope(TelemetryEventNames.TaskStart, data, participantId: new string('p', 64))));
        Assert.NotEmpty(_sut.CollectRefusals(
            CreateEnvelope(TelemetryEventNames.TaskStart, data, participantId: new string('p', 65))));

        var invalidEventId = JsonNode.Parse(CreateEnvelope(TelemetryEventNames.TaskStart, data))!.AsObject();
        invalidEventId[TelemetryEnvelopeFieldNames.EventId] = new string('G', 32);

        Assert.Contains(
            _sut.CollectRefusals(invalidEventId.ToJsonString()),
            refusal => refusal.InstanceLocation == $"/{TelemetryEnvelopeFieldNames.EventId}");
    }

    [Theory]
    [InlineData("participant", true)]
    [InlineData("demonstration", true)]
    [InlineData("operational_probe", true)]
    [InlineData("unknown", false)]
    public void CollectRefusals_RecordProvenance_UsesModeledVocabulary(
        string recordProvenance,
        bool expected)
    {
        var envelope = CreateEnvelope(
            TelemetryEventNames.TaskStart,
            new { canonical_transitions = Array.Empty<object>() },
            recordProvenance: recordProvenance);

        Assert.Equal(expected, _sut.CollectRefusals(envelope).Count == 0);
    }

    [Fact]
    public void CollectRefusals_UnmodeledTaskVariantMember_IsRefused()
    {
        var envelope = JsonNode.Parse(CreateEnvelope(
            TelemetryEventNames.TaskStart,
            new { canonical_transitions = Array.Empty<object>() }))!.AsObject();
        envelope["task_variant"] = "X";

        Assert.Contains(
            _sut.CollectRefusals(envelope.ToJsonString()),
            refusal => refusal.InstanceLocation == "/task_variant");
    }

    [Fact]
    public void Validate_CanonicalEnvelope_PassesWithoutThrowing()
    {
        _sut.Validate(CreateEnvelope(
            TelemetryEventNames.TaskStart,
            new { canonical_transitions = Array.Empty<object>() }));
    }

    [Fact]
    public void Validate_NoncanonicalEnvelope_NamesThePlaceAndTheBrokenRule()
    {
        var exception = Assert.Throws<JsonException>(
            () => _sut.Validate(CreateCorruptedNavigationEnvelope("missing")));

        Assert.Contains("/data", exception.Message, StringComparison.Ordinal);
        Assert.Contains("required", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Json.Schema.EvaluationResults", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CollectRecordRefusals_LineCarryingTheReceiptTimeTheServerMeasured_IsCanonical()
    {
        Assert.Empty(_sut.CollectRecordRefusals(CreateRecord()));
    }

    [Fact]
    public void CollectRefusals_EnvelopeCarryingAReceiptTime_RefusesThatMemberAndNothingElse()
    {
        // Die Empfangszeit misst der Server. Sender dürfen diesen Auswertungswert nicht
        // vorgeben; deshalb wird das Feld abgelehnt und nicht bloß überschrieben.
        var refusal = Assert.Single(_sut.CollectRefusals(CreateRecord()));

        Assert.Equal("/record_received_at", refusal.InstanceLocation);
        Assert.NotEmpty(refusal.Message);
    }

    [Fact]
    public void CollectRecordRefusals_LineWithoutAReceiptTime_NamesTheMissingMember()
    {
        var line = JsonNode.Parse(CreateRecord())!.AsObject();
        line.Remove(TelemetryRecordReceivedAt.MemberName);

        var refusal = Assert.Single(_sut.CollectRecordRefusals(line.ToJsonString()));

        Assert.Equal("required", refusal.Keyword);
        Assert.Contains(TelemetryRecordReceivedAt.MemberName, refusal.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("2026-09-10T12:34:56.1234567")]
    [InlineData("2026-09-10T12:34:56Z")]
    [InlineData("2026-09-10T12:34:56.1234567Z")]
    [InlineData("2026-09-10 12:34:56.1234567+02:00")]
    [InlineData("2026-09-10T12:34:56.123+02:00")]
    public void CollectRecordRefusals_ReceiptTimeWithoutAWrittenOutOffsetOrFullPrecision_NamesThatMemberAlone(
        string receiptTime)
    {
        // Bei der Berliner Herbstumstellung kommt 02:30 zweimal vor. Ein expliziter
        // UTC-Offset unterscheidet diese Zeitpunkte; das Schema verlangt zusätzlich volle Präzision.
        var line = JsonNode.Parse(CreateRecord())!.AsObject();
        line[TelemetryRecordReceivedAt.MemberName] = receiptTime;

        var refusal = Assert.Single(_sut.CollectRecordRefusals(line.ToJsonString()));

        Assert.Equal("/record_received_at", refusal.InstanceLocation);
        Assert.Equal("pattern", refusal.Keyword);
    }

    [Theory]
    [InlineData("extra", "/data/legacy_position", "unevaluatedProperties")]
    [InlineData("missing", "/data", "required")]
    [InlineData("wrong_type", "/data/navigation_index", "type")]
    public void CollectRecordRefusals_LineWithNoncanonicalData_NamesTheMemberAndTheBrokenRuleAndNothingElse(
        string corruption,
        string expectedInstanceLocation,
        string expectedKeyword)
    {
        // Gespeicherte Datensätze haben eine eigene Schemaform. Dieselben ungültigen
        // Nutzlasten müssen deshalb auch über diesen Validierungseinstieg abgelehnt werden.
        var line = JsonNode.Parse(CreateCorruptedNavigationEnvelope(corruption))!.AsObject();
        line[TelemetryRecordReceivedAt.MemberName] = CanonicalReceiptTime;

        var refusal = Assert.Single(_sut.CollectRecordRefusals(line.ToJsonString()));

        Assert.Equal(expectedInstanceLocation, refusal.InstanceLocation);
        Assert.Equal(expectedKeyword, refusal.Keyword);
    }

    [Fact]
    public void CollectRecordRefusals_LineWithAnUnknownRootMember_NamesThatMemberAndNotTheWholeEnvelope()
    {
        var line = JsonNode.Parse(CreateRecord())!.AsObject();
        line["leftover"] = "x";

        var refusal = Assert.Single(_sut.CollectRecordRefusals(line.ToJsonString()));

        Assert.Equal("/leftover", refusal.InstanceLocation);
        Assert.Equal("additionalProperties", refusal.Keyword);
    }

    [Fact]
    public void CollectRecordRefusals_LineBreakingTwoRules_NamesBothAndNotTheWholeEnvelope()
    {
        // Zwei unabhängige Fehler sollen einzeln an ihren Eigenschaften gemeldet werden.
        // Ein Fehler darf nicht dazu führen, dass sämtliche Eigenschaften als unbekannt erscheinen.
        var line = JsonNode.Parse(CreateCorruptedNavigationEnvelope("wrong_type"))!.AsObject();
        line[TelemetryRecordReceivedAt.MemberName] = CanonicalReceiptTime;
        line["leftover"] = "x";

        var refusals = _sut.CollectRecordRefusals(line.ToJsonString());

        Assert.Equal(2, refusals.Count);
        Assert.Contains(
            refusals,
            refusal => refusal.InstanceLocation == "/leftover" && refusal.Keyword == "additionalProperties");
        Assert.Contains(
            refusals,
            refusal => refusal.InstanceLocation == "/data/navigation_index" && refusal.Keyword == "type");
    }

    private static string CreateRecord()
    {
        var line = JsonNode.Parse(CreateEnvelope(
            TelemetryEventNames.NavigationChanged,
            new
            {
                current_field_id = "contactEmail",
                current_field_group_id = "contact",
                navigation_index = 1,
                navigation_total = 3,
                canonical_transitions = Array.Empty<object>(),
            }))!.AsObject();
        line[TelemetryRecordReceivedAt.MemberName] = CanonicalReceiptTime;
        return line.ToJsonString();
    }

    private static string[] MemberNamesOf(JsonElement element)
    {
        return [.. element.EnumerateObject()
            .Select(static member => member.Name)
            .Order(StringComparer.Ordinal)];
    }

    private static string CreateCorruptedNavigationEnvelope(string corruption)
    {
        var envelope = JsonNode.Parse(CreateEnvelope(
            TelemetryEventNames.NavigationChanged,
            new
            {
                current_field_id = "contactEmail",
                current_field_group_id = "contact",
                navigation_index = 1,
                navigation_total = 3,
                canonical_transitions = Array.Empty<object>(),
            }))!.AsObject();
        var data = envelope[TelemetryEnvelopeFieldNames.Data]!.AsObject();
        switch (corruption)
        {
            case "extra":
                data["legacy_position"] = 1;
                break;
            case "missing":
                data.Remove("navigation_total");
                break;
            case "wrong_type":
                data["navigation_index"] = "1";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(corruption), corruption, "Unknown corruption.");
        }

        return envelope.ToJsonString();
    }

    private static string CreateEnvelope(
        string eventName,
        object data,
        string participantId = "P001",
        string recordProvenance = "participant")
        => JsonSerializer.Serialize(new
        {
            event_id = "0123456789abcdef0123456789abcdef",
            session_id = "abcdef0123456789abcdef0123456789",
            client_offset_ms = 0,
            participant_id = participantId,
            condition = "A",
            application_id = "sample.checkout",
            task_id = "T01",
            record_provenance = recordProvenance,
            @event = eventName,
            state_version = 1,
            data,
        });
}
