using Sprechen.EvaluationTelemetry.Ingestion;
using Sprechen.Forms.Services;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;
using System.Text.Json;
using Xunit;

namespace Sprechen.Forms.Tests.Services;

public sealed class EvaluationTelemetryAcceptedEventLoggerTests
{
    private const string ReceiptTime = "2026-09-10T12:34:56.1234567+02:00";

    private readonly FakeLogger<EvaluationTelemetryAcceptedEventLogger> _logger = new();
    private readonly EvaluationTelemetryEnvelopeBuilder _builder =
        new(new TelemetryEnvelopeSchemaValidator());
    private readonly EvaluationTelemetryAcceptedEventLogger _sut;

    public EvaluationTelemetryAcceptedEventLoggerTests()
    {
        _sut = new EvaluationTelemetryAcceptedEventLogger(_logger);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EvaluationTelemetryAcceptedEventLogger(null!));
    }

    [Fact]
    public void AcceptAndLog_WithNullPayload_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AcceptAndLog(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AcceptAndLog_WithEmptyEvent_FailsFastBecauseNothingMeasuredThisPayload(string? eventName)
    {


        var payload = CreateValidPayload() with { Event = eventName! };

        Assert.ThrowsAny<ArgumentException>(() => _builder.Build(payload));
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_WithUnknownEvent_DoesNotLog()
    {
        var payload = CreateValidPayload() with { Event = "legacy_session_start" };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("C")]
    public void AcceptAndLog_WithInvalidCondition_DoesNotLog(string? condition)
    {
        var payload = CreateValidPayload() with { Condition = condition! };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void AcceptAndLog_WithNegativeStateVersion_DoesNotLog(int stateVersion)
    {
        var payload = CreateValidPayload() with { StateVersion = stateVersion };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_TaskStart_LogsWithExplicitEmptyTransitionList()
    {
        var payload = CreateValidPayload();

        var accepted = AcceptAndLog(payload);

        Assert.True(accepted);
        var entry = Assert.Single(_logger.LogEntries);
        Assert.Contains("task_start", entry);
        Assert.Contains("P001", entry);
        Assert.Contains("sample.checkout", entry);
        Assert.Contains("T01", entry);
        Assert.Contains("\"canonical_transitions\":[]", entry);
    }

    [Fact]
    public void AcceptAndLog_TaskStart_CarriesProvenanceInTheEnvelope()
    {
        var payload = CreateValidPayload() with
        {
            Data = ParseJson("{\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.True(accepted);
        var entry = Assert.Single(_logger.LogEntries);
        Assert.Contains("participant", entry);
    }

    [Fact]
    public void AcceptAndLog_TaskStart_WithTheVariantInItsData_DoesNotLog()
    {


        var payload = CreateValidPayload() with
        {
            Data = ParseJson("{\"task_variant\":\"v1\",\"canonical_transitions\":[]}"),
        };

        Assert.False(AcceptAndLog(payload));
        Assert.Empty(_logger.LogEntries);
    }


    [Fact]
    public void AcceptAndLog_TaskStart_WithUnexpectedDataProperty_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Data = ParseJson("{\"unexpected\":true,\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_TaskEnd_WithRequiredData_Logs()
    {
        var payload = CreateValidPayload() with
        {
            Event = "task_end",
            StateVersion = 10,
            Data = ParseJson("{\"end_state\":\"process_submitted\",\"canonical_transitions\":[],\"mismatch_code\":\"none\",\"correctness_basis\":\"log_and_system_check\"}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.True(accepted);
        var entry = Assert.Single(_logger.LogEntries);
        Assert.Contains("task_end", entry);
        Assert.Contains("submitted", entry);
    }

    [Fact]
    public void AcceptAndLog_TaskEnd_WithoutData_DoesNotLog()
    {
        var payload = CreateValidPayload() with { Event = "task_end", Data = null };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_TaskEnd_WithUnsupportedEndState_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "task_end",
            Data = ParseJson("{\"end_state\":\"submitted\",\"canonical_transitions\":[],\"mismatch_code\":\"none\",\"correctness_basis\":\"log_and_system_check\"}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_NavigationChanged_MissingRequiredProperty_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "navigation_changed",
            Data = ParseJson("{\"current_field_id\":\"f1\",\"current_field_group_id\":\"g1\",\"navigation_index\":1,\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_NavigationChanged_WithUnexpectedProperty_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "navigation_changed",
            Data = ParseJson("{\"current_field_id\":\"f1\",\"current_field_group_id\":\"g1\",\"navigation_index\":1,\"navigation_total\":5,\"extra\":\"x\",\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_NavigationChanged_WithRequiredData_Logs()
    {
        var payload = CreateValidPayload() with
        {
            Event = "navigation_changed",
            Data = ParseJson("{\"current_field_id\":\"f1\",\"current_field_group_id\":\"g1\",\"navigation_index\":1,\"navigation_total\":5,\"canonical_transitions\":[{\"canonical_class\":\"field_target_changed\",\"scope\":\"field\",\"object_id\":\"f1\",\"resulting_state\":\"targeted\"},{\"canonical_class\":\"field_group_target_changed\",\"scope\":\"field_group\",\"object_id\":\"g1\",\"resulting_state\":\"targeted\"}]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.True(accepted);
        var entry = Assert.Single(_logger.LogEntries);
        Assert.Contains("navigation_changed", entry);
        Assert.Contains("navigation_total", entry);
    }

    [Fact]
    public void AcceptAndLog_ValidationErrorOpened_InvalidSeverity_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "validation_error_opened",
            Data = ParseJson("{\"error_code\":\"D-VALIDATION\",\"severity\":\"critical\",\"scope\":\"field\",\"trigger\":\"user_input\",\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_ValidationErrorOpened_UnsupportedScopeValue_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "validation_error_opened",
            Data = ParseJson("{\"error_code\":\"D-VALIDATION\",\"severity\":\"error\",\"scope\":\"section\",\"trigger\":\"user_input\",\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_ValidationErrorOpened_WithUnexpectedProperty_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "validation_error_opened",
            Data = ParseJson("{\"error_code\":\"D-VALIDATION\",\"severity\":\"error\",\"scope\":\"field\",\"extra\":\"x\",\"trigger\":\"user_input\",\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_ValidationErrorOpened_WithUppercaseSeverity_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "validation_error_opened",
            Data = ParseJson("{\"error_code\":\"D-VALIDATION\",\"severity\":\"ERROR\",\"scope\":\"field\",\"trigger\":\"user_input\",\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_ValidationErrorOpened_WithValidErrorData_Logs()
    {
        var payload = CreateValidPayload() with
        {
            Event = "validation_error_opened",
            Data = ParseJson("{\"error_code\":\"D-VALIDATION\",\"severity\":\"error\",\"scope\":\"field_group\",\"field_group_id\":\"g1\",\"trigger\":\"user_input\",\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.True(accepted);
        var entry = Assert.Single(_logger.LogEntries);
        Assert.Contains("validation_error_opened", entry);
        Assert.Contains("field_group", entry);
    }

    [Fact]
    public void AcceptAndLog_ValidationErrorOpened_WithoutTrigger_DoesNotLog()
    {
        // Der Auslöser unterscheidet, wie der Validierungsfehler entstanden ist.

        var payload = CreateValidPayload() with
        {
            Event = "validation_error_opened",
            Data = ParseJson("{\"error_code\":\"D-VALIDATION\",\"severity\":\"error\",\"scope\":\"field\",\"canonical_transitions\":[]}"),
        };

        Assert.False(AcceptAndLog(payload));
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_ValidationErrorOpened_WithUnmodeledTrigger_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "validation_error_opened",
            Data = ParseJson("{\"error_code\":\"D-VALIDATION\",\"severity\":\"error\",\"scope\":\"field\",\"trigger\":\"because\",\"canonical_transitions\":[]}"),
        };

        Assert.False(AcceptAndLog(payload));
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_SttError_WithTrigger_DoesNotLog()
    {

        var payload = CreateValidPayload() with
        {
            Event = "stt_error",
            Data = ParseJson("{\"error_code\":\"D-STT\",\"severity\":\"warning\",\"scope\":\"field\",\"trigger\":\"system_recognition\",\"canonical_transitions\":[]}"),
        };

        Assert.False(AcceptAndLog(payload));
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_SttError_WithTheValidationDiagnosis_DoesNotLog()
    {

        var payload = CreateValidPayload() with
        {
            Event = "stt_error",
            Data = ParseJson("{\"error_code\":\"D-VALIDATION\",\"severity\":\"warning\",\"scope\":\"field\",\"canonical_transitions\":[]}"),
        };

        Assert.False(AcceptAndLog(payload));
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_NavigationError_WithTheRecognitionDiagnosis_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "navigation_error",
            Data = ParseJson("{\"error_code\":\"D-STT\",\"severity\":\"error\",\"scope\":\"cross_group\",\"canonical_transitions\":[]}"),
        };

        Assert.False(AcceptAndLog(payload));
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_StatusSnapshot_WithNestedAdditionalProperty_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "status_snapshot",
            Data = ParseJson("{\"navigation\":{\"current_field_id\":\"f1\",\"current_field_group_id\":\"g1\",\"navigation_index\":1,\"navigation_total\":5,\"extra\":\"x\"},\"progress\":{\"allFields\":{\"completed\":1,\"total\":2},\"requiredFieldsOnly\":{\"completed\":1,\"total\":1},\"fieldGroupBased\":{\"completed\":0,\"total\":1}},\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_StatusSnapshot_WithInvalidCounterType_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "status_snapshot",
            Data = ParseJson("{\"navigation\":{\"current_field_id\":\"f1\",\"current_field_group_id\":\"g1\",\"navigation_index\":1,\"navigation_total\":5},\"progress\":{\"allFields\":{\"completed\":\"1\",\"total\":2},\"requiredFieldsOnly\":{\"completed\":1,\"total\":1},\"fieldGroupBased\":{\"completed\":0,\"total\":1}},\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_ManualStep_WithoutData_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "manual_step",
            Data = null,
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_ManualStep_WithNonStringNote_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "manual_step",
            Data = ParseJson("{\"note\":123,\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_ValueChange_WithSttConfidencePercent_Logs()
    {
        var payload = CreateValidPayload() with
        {
            Event = "value_change",
            Data = ParseJson("{\"field_id\":\"f1\",\"stt_confidence\":85,\"canonical_transitions\":[{\"canonical_class\":\"confirmed_field_value_changed\",\"scope\":\"field\",\"object_id\":\"f1\",\"resulting_state\":\"confirmed_changed\"}]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.True(accepted);
        var entry = Assert.Single(_logger.LogEntries);
        Assert.Contains("\"stt_confidence\":85", entry);
    }

    [Fact]
    public void AcceptAndLog_ValueChange_WithOutOfRangeSttConfidence_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "value_change",
            Data = ParseJson("{\"field_id\":\"f1\",\"stt_confidence\":101,\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_ValueChange_WithNonIntegerSttConfidence_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "value_change",
            Data = ParseJson("{\"field_id\":\"f1\",\"stt_confidence\":0.85,\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_AmbiguityResolved_WithInvalidSignalClass_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "ambiguity_resolved",
            Data = ParseJson("{\"ambiguity_type\":\"input_vs_command\",\"resolution_strategy\":\"context_default\",\"signal_class\":\"unclear\",\"result\":\"accepted\",\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_AmbiguityResolved_WithUnsupportedAmbiguityType_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "ambiguity_resolved",
            Data = ParseJson("{\"ambiguity_type\":\"command_vs_input\",\"resolution_strategy\":\"context_default\",\"signal_class\":\"ambiguous\",\"result\":\"accepted\",\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_AmbiguityResolved_WithUnsupportedResolutionStrategy_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "ambiguity_resolved",
            Data = ParseJson("{\"ambiguity_type\":\"input_vs_command\",\"resolution_strategy\":\"deterministic\",\"signal_class\":\"ambiguous\",\"result\":\"accepted\",\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_InputModeChanged_WithValidData_Logs()
    {
        var payload = CreateValidPayload() with
        {
            Event = "input_mode_changed",
            Data = ParseJson("{\"from_mode\":\"voice\",\"to_mode\":\"keyboard\",\"field_id\":\"f1\",\"trigger\":\"explicit_command\",\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.True(accepted);
        var entry = Assert.Single(_logger.LogEntries);
        Assert.Contains("input_mode_changed", entry);
        Assert.Contains("\"trigger\":\"explicit_command\"", entry);
    }

    [Fact]
    public void AcceptAndLog_InputModeChanged_WithUnsupportedMode_DoesNotLog()
    {
        var payload = CreateValidPayload() with
        {
            Event = "input_mode_changed",
            Data = ParseJson("{\"from_mode\":\"touch\",\"to_mode\":\"keyboard\",\"field_id\":\"f1\",\"trigger\":\"explicit_command\",\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Fact]
    public void AcceptAndLog_DestructiveCommandResolved_WithValidData_Logs()
    {
        var payload = CreateValidPayload() with
        {
            Event = "destructive_command_resolved",
            Data = ParseJson("{\"command_id\":\"cancel\",\"confirmed\":true,\"refractory_blocked\":false,\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.True(accepted);
        var entry = Assert.Single(_logger.LogEntries);
        Assert.Contains("destructive_command_resolved", entry);
        Assert.Contains("\"command_id\":\"cancel\"", entry);
    }

    [Fact]
    public void AcceptAndLog_BargeIn_WithValidData_Logs()
    {
        var payload = CreateValidPayload() with
        {
            Event = "barge_in",
            Data = ParseJson("{\"field_id\":\"f1\",\"interrupted_prompt_id\":\"dialog.status\",\"canonical_transitions\":[]}"),
        };

        var accepted = AcceptAndLog(payload);

        Assert.True(accepted);
        var entry = Assert.Single(_logger.LogEntries);
        Assert.Contains("barge_in", entry);
        Assert.Contains("\"interrupted_prompt_id\":\"dialog.status\"", entry);
    }

    [Fact]
    public void AcceptAndLog_DoesNotRepairASpellingTheModelRefuses()
    {


        var payload = CreateValidPayload() with
        {
            Event = "  TASK_START  ",
            Condition = "b",
        };

        var accepted = AcceptAndLog(payload);

        Assert.False(accepted);
        Assert.Empty(_logger.LogEntries);
    }

    [Theory]
    [InlineData("task_start")]
    [InlineData("task_end")]
    [InlineData("navigation_changed")]
    [InlineData("field_relevance_changed")]
    [InlineData("field_completion_changed")]
    [InlineData("field_group_relevance_changed")]
    [InlineData("field_group_completion_changed")]
    [InlineData("status_snapshot")]
    [InlineData("validation_error_opened")]
    [InlineData("validation_error_cleared")]
    [InlineData("navigation_error")]
    [InlineData("stt_error")]
    [InlineData("intent_recognized")]
    [InlineData("system_feedback")]
    [InlineData("focus_change")]
    [InlineData("value_change")]
    [InlineData("manual_step")]
    [InlineData("ambiguity_resolved")]
    [InlineData("input_mode_changed")]
    [InlineData("destructive_command_resolved")]
    [InlineData("barge_in")]
    [InlineData("wake_word_listener_setup")]
    [InlineData("wake_word_acoustic_detection")]
    [InlineData("wake_word_capture_loop")]
    [InlineData("wake_word_transcription_rejection")]
    [InlineData("wake_stitch")]
    [InlineData("wake_word_miss")]
    [InlineData("wake_word_activation")]
    [InlineData("wake_word_fallback")]
    public void AcceptAndLog_AllSupportedEvents_AcceptedWhenDataIsValid(string eventName)
    {
        var payload = CreatePayloadForEvent(eventName);

        var accepted = AcceptAndLog(payload);

        Assert.True(accepted);
        Assert.Single(_logger.LogEntries);
        Assert.Contains(eventName, _logger.LogEntries[0]);
        _logger.LogEntries.Clear();
    }

    private bool AcceptAndLog(TelemetryEventPayload payload)
    {
        EvaluationTelemetryEnvelopeBuildResult envelope;
        try
        {
            envelope = _builder.Build(payload);
        }
        catch (JsonException)
        {
            // Der Builder prüft die erzeugte Zeile gegen das Schema.
            // Bei einem Schemafehler ruft diese Testhilfe den Logger nicht auf.
            return false;
        }

        if (!envelope.Accepted)
        {
            return false;
        }

        _sut.LogAcceptedEnvelope(envelope);
        return true;
    }

    private static TelemetryEventPayload CreateValidPayload() => new()
    {
        EventId = "0123456789abcdef0123456789abcdef",
        SessionId = "abcdef0123456789abcdef0123456789",
        ClientOffsetMs = 0,
        ParticipantId = "P001",
        Condition = "A",
        ApplicationId = "sample.checkout",
        TaskId = "T01",
        RecordProvenance = TelemetryRecordProvenances.Participant,
        Event = "task_start",
        StateVersion = 0,
        RecordReceivedAt = ReceiptTime,
        Data = ParseJson("{\"canonical_transitions\":[]}"),
    };

    private static TelemetryEventPayload CreatePayloadForEvent(string eventName)
    {
        var payload = CreateValidPayload() with { Event = eventName };

        return eventName switch
        {
            "task_start" => payload,
            "task_end" => payload with { Data = ParseJson("{\"end_state\":\"process_submitted\",\"canonical_transitions\":[],\"mismatch_code\":\"none\",\"correctness_basis\":\"log_and_system_check\"}") },
            "navigation_changed" => payload with { Data = ParseJson("{\"current_field_id\":\"f1\",\"current_field_group_id\":\"g1\",\"navigation_index\":1,\"navigation_total\":5,\"canonical_transitions\":[{\"canonical_class\":\"field_target_changed\",\"scope\":\"field\",\"object_id\":\"f1\",\"resulting_state\":\"targeted\"},{\"canonical_class\":\"field_group_target_changed\",\"scope\":\"field_group\",\"object_id\":\"g1\",\"resulting_state\":\"targeted\"}]}") },
            "field_relevance_changed" => payload with { Data = ParseJson("{\"field_id\":\"f1\",\"field_group_id\":\"g1\",\"relevant\":true,\"canonical_transitions\":[]}") },
            "field_completion_changed" => payload with { Data = ParseJson("{\"field_id\":\"f1\",\"field_group_id\":\"g1\",\"relevant\":true,\"required\":true,\"completed\":true,\"canonical_transitions\":[{\"canonical_class\":\"field_completion_changed\",\"scope\":\"field\",\"object_id\":\"f1\",\"resulting_state\":\"complete\"}]}") },
            "field_group_relevance_changed" => payload with { Data = ParseJson("{\"field_group_id\":\"g1\",\"relevant\":true,\"canonical_transitions\":[]}") },
            "field_group_completion_changed" => payload with { Data = ParseJson("{\"field_group_id\":\"g1\",\"relevant\":true,\"completed\":false,\"canonical_transitions\":[{\"canonical_class\":\"field_group_completion_changed\",\"scope\":\"field_group\",\"object_id\":\"g1\",\"resulting_state\":\"incomplete\"}]}") },
            "status_snapshot" => payload with { Data = ParseJson("{\"navigation\":{\"current_field_id\":\"f1\",\"current_field_group_id\":\"g1\",\"navigation_index\":1,\"navigation_total\":5},\"progress\":{\"allFields\":{\"completed\":1,\"total\":2},\"requiredFieldsOnly\":{\"completed\":1,\"total\":1},\"fieldGroupBased\":{\"completed\":0,\"total\":1}},\"canonical_transitions\":[]}") },
            "validation_error_opened" => payload with { Data = ParseJson("{\"error_code\":\"D-VALIDATION\",\"severity\":\"error\",\"scope\":\"field\",\"trigger\":\"user_input\",\"canonical_transitions\":[]}") },
            "validation_error_cleared" => payload with { Data = ParseJson("{\"error_code\":\"D-VALIDATION\",\"severity\":\"info\",\"scope\":\"field\",\"canonical_transitions\":[]}") },
            "navigation_error" => payload with { Data = ParseJson("{\"error_code\":\"D-NAVIGATION\",\"severity\":\"error\",\"scope\":\"cross_group\",\"canonical_transitions\":[]}") },
            "stt_error" => payload with { Data = ParseJson("{\"error_code\":\"D-STT\",\"severity\":\"warning\",\"scope\":\"field\",\"canonical_transitions\":[]}") },
            "intent_recognized" => payload with { Data = ParseJson("{\"intent\":\"NAVIGATE\",\"canonical_transitions\":[]}") },
            "system_feedback" => payload with { Data = ParseJson("{\"type\":\"status\",\"canonical_transitions\":[]}") },
            "focus_change" => payload with { Data = ParseJson("{\"field_id\":\"f1\",\"canonical_transitions\":[{\"canonical_class\":\"field_target_changed\",\"scope\":\"field\",\"object_id\":\"f1\",\"resulting_state\":\"targeted\"}]}") },
            "value_change" => payload with { Data = ParseJson("{\"field_id\":\"f1\",\"canonical_transitions\":[{\"canonical_class\":\"confirmed_field_value_changed\",\"scope\":\"field\",\"object_id\":\"f1\",\"resulting_state\":\"confirmed_changed\"}]}") },
            "manual_step" => payload with { Data = ParseJson("{\"note\":\"manual observation\",\"canonical_transitions\":[{\"canonical_class\":\"field_target_changed\",\"scope\":\"field\",\"object_id\":\"f1\",\"resulting_state\":\"targeted\"}]}") },
            "ambiguity_resolved" => payload with { Data = ParseJson("{\"ambiguity_type\":\"input_vs_command\",\"resolution_strategy\":\"context_default\",\"signal_class\":\"ambiguous\",\"result\":\"accepted\",\"field_id\":\"f1\",\"command_id\":\"next\",\"canonical_transitions\":[]}") },
            "input_mode_changed" => payload with { Data = ParseJson("{\"from_mode\":\"voice\",\"to_mode\":\"keyboard\",\"field_id\":\"f1\",\"trigger\":\"automatic\",\"canonical_transitions\":[]}") },
            "destructive_command_resolved" => payload with { Data = ParseJson("{\"command_id\":\"cancel\",\"confirmed\":true,\"canonical_transitions\":[]}") },
            "barge_in" => payload with { Data = ParseJson("{\"field_id\":\"f1\",\"interrupted_prompt_id\":\"dialog.help\",\"canonical_transitions\":[]}") },
            "wake_word_listener_setup" => payload with { Data = ParseJson("{\"phase\":\"Started\",\"event_code\":\"WAKE_WORD_LISTENER_STARTED\",\"language_code\":\"de-DE\",\"canonical_transitions\":[]}") },
            "wake_word_acoustic_detection" => payload with { Data = ParseJson("{\"phase\":\"Detected\",\"event_code\":\"WAKE_WORD_ACOUSTIC_DETECTED\",\"detector_id\":\"local\",\"canonical_transitions\":[]}") },
            "wake_word_capture_loop" => payload with { Data = ParseJson("{\"phase\":\"CaptureNoSpeechRetry\",\"event_code\":\"WAKE_WORD_CAPTURE_NO_SPEECH_RETRY\",\"canonical_transitions\":[]}") },
            "wake_word_transcription_rejection" => payload with { Data = ParseJson("{\"phase\":\"QueueFull\",\"event_code\":\"WAKE_WORD_TRANSCRIPTION_QUEUE_FULL\",\"failure_code\":\"QUEUE_FULL\",\"canonical_transitions\":[]}") },
            "wake_stitch" => payload with { Data = ParseJson("{\"phase\":\"Attached\",\"event_code\":\"WAKE_STITCH_ATTACHED\",\"transcript_text_length\":4,\"stitched_transcript_length\":12,\"command_text_length\":8,\"canonical_transitions\":[]}") },
            "wake_word_miss" => payload with { Data = ParseJson("{\"phase\":\"MissTrace\",\"event_code\":\"WAKE_WORD_MISS_TRACE\",\"status\":\"MissingLeadingWakeWord\",\"canonical_transitions\":[]}") },
            "wake_word_activation" => payload with { Data = ParseJson("{\"phase\":\"LexicalDetected\",\"event_code\":\"WAKE_WORD_ACTIVATION_LEXICAL_DETECTED\",\"command_text_present\":true,\"canonical_transitions\":[]}") },
            "wake_word_fallback" => payload with { Data = ParseJson("{\"phase\":\"Published\",\"event_code\":\"WAKE_WORD_FALLBACK_PUBLISHED\",\"reason_code\":\"FORCED\",\"canonical_transitions\":[]}") },
            _ => throw new ArgumentOutOfRangeException(nameof(eventName), eventName, "Unsupported event for test."),
        };
    }

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
