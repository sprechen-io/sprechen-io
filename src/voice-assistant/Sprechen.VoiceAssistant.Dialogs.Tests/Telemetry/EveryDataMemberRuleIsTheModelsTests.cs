using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.TestDoubles;
using Sprechen.VoiceAssistant.Dialogs.Tests.Services;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

using EvaluationTelemetryImpl = Sprechen.VoiceAssistant.EvaluationTelemetry.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Telemetry;

/// <summary>
/// Prüft ausgewählte ungültige Ereignisdaten an den Telemetrieaufrufen. Die Ablehnung muss Datenmember und Schemaregel nennen, statt den Wert stillschweigend zu normalisieren.
/// </summary>
public sealed class EveryDataMemberRuleIsTheModelsTests
{

    [Theory]
    [InlineData("field_id", "pattern", " email ")]
    [InlineData("field_id", "pattern", "   ")]
    [InlineData("field_id", "minLength", "")]
    [InlineData("field_id", "type", null)]
    public async Task AFieldIdTheModelRefuses_IsRefusedAtThatMemberAndKeyword(string member, string keyword, string fieldId)
    {
        await AssertRefusedAsync(member, keyword, (telemetry, session) => telemetry.TrackNoSpeechDetected(session, fieldId));
    }

    // Auch bei einem kanonischen Übergang muss die Ablehnung das ursprüngliche Datenmember benennen.
    [Fact]
    public async Task APaddedFieldIdOnATransitionCarryingLine_IsRefusedAtTheMember()
    {
        await AssertRefusedAsync("field_id", "pattern", (telemetry, session) => telemetry.TrackFieldFocus(session, " email "));
    }

    [Theory]
    // Interne blockierende Fehlercodes behalten eigene Format- und Längengrenzen; sie sind von den Diagnosecodes der Studienereignisse zu unterscheiden.
    [InlineData("maxLength", "EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE")]
    [InlineData("minLength", "")]
    [InlineData("pattern", "9_REQUIRED")]
    public async Task ABlockingErrorCodeTheModelRefuses_IsRefusedAtThatMemberAndKeyword(
        string keyword,
        string blockingErrorCode)
    {
        var telemetry = CreateTelemetry(preparedStudyRun: true);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);
        telemetry.TrackFieldAndGroupStatus(session, DetailedStatus(completed: false, blockingErrorCodes: []));

        var exception = Assert.Throws<JsonException>(() => telemetry.TrackFieldAndGroupStatus(
            session,
            DetailedStatus(completed: true, blockingErrorCodes: [blockingErrorCode])));

        Assert.Contains("/data/blocking_error_codes/0 " + keyword, exception.Message, StringComparison.Ordinal);
    }

    private static EvaluationDetailedFormStatus DetailedStatus(
        bool completed,
        string[] blockingErrorCodes)
        => new(
            [new EvaluationFieldStatus("email", "contact", Relevant: true, Required: true, completed, [.. blockingErrorCodes])],
            [new EvaluationFieldGroupStatus("contact", Relevant: true, completed, [])]);

    [Theory]
    // Das Studienereignis trägt den festgelegten Diagnosecode unabhängig vom gemeldeten internen Fehlercode.
    [InlineData(" form.required ")]
    [InlineData("9_REQUIRED")]
    [InlineData("form.required")]
    public async Task AnInternalErrorCode_NeverReachesTheStudyLine(string reportedCode)
    {
        var store = new RecordingCheckpointStore();
        var telemetry = CreateTelemetry(preparedStudyRun: true, store);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackConstraintViolation(
            session,
            "email",
            reportedCode,
            TelemetrySeverityLevels.Error,
            TelemetryErrorTriggers.UserInput);

        var data = store.LastDataOf(TelemetryEventNames.ValidationErrorOpened);
        Assert.Equal(TelemetryStudyDiagnosisCodes.Validation, data.GetProperty("error_code").GetString());
    }

    [Theory]
    // Ungültige Schweregrade dürfen nicht durch Normalisierung zu gültigen Fehlerereignissen werden.
    [InlineData("ERROR")]
    [InlineData(" error ")]
    [InlineData("critical")]
    [InlineData("Fehler")]
    public async Task ASeverityTheModelDoesNotState_IsRefusedInsteadOfCountedAsAnError(string severity)
    {
        await AssertRefusedAsync("severity", "enum", (telemetry, session) => telemetry.TrackConstraintViolation(
            session,
            "email",
            "form.required",
            severity,
            TelemetryErrorTriggers.UserInput));
    }

    [Theory]
    [InlineData("USER_INPUT")]
    [InlineData(" user_input ")]
    [InlineData("guessing")]
    public async Task AnErrorTriggerTheModelDoesNotState_IsRefusedAtThatMemberAndKeyword(string trigger)
    {
        await AssertRefusedAsync("trigger", "enum", (telemetry, session) => telemetry.TrackConstraintViolation(
            session,
            "email",
            "form.required",
            TelemetrySeverityLevels.Error,
            trigger));
    }

    [Theory]
    [InlineData("not_a_scope")]
    [InlineData("FIELD_GROUP")]
    [InlineData(" fieldGroup ")]
    public async Task AScopeThePairingDoesNotName_IsRefusedInsteadOfReplaced(string scope)
    {
        await AssertRefusedAsync("scope", "enum", (telemetry, session) => telemetry.TrackConstraintViolation(
            session,
            "email",
            "form.required",
            TelemetrySeverityLevels.Error,
            scope,
            fieldGroupId: null,
            TelemetryErrorTriggers.UserInput));
    }

    [Theory]
    [InlineData("to_mode", "enum", " keyboard ")]
    [InlineData("to_mode", "enum", "KEYBOARD")]
    [InlineData("to_mode", "enum", "morse")]
    public async Task AnInputModeTheModelDoesNotState_IsRefusedAtThatMemberAndKeyword(string member, string keyword, string toMode)
    {
        await AssertRefusedAsync(member, keyword, (telemetry, session) => telemetry.TrackInputModeChanged(
            session,
            "email",
            InputModes.Voice,
            toMode,
            TelemetryInputModeTriggers.ExplicitCommand));
    }

    [Fact]
    public async Task AnInputModeTriggerTheModelDoesNotState_IsRefusedAtThatMemberAndKeyword()
    {
        await AssertRefusedAsync("trigger", "enum", (telemetry, session) => telemetry.TrackInputModeChanged(
            session,
            "email",
            InputModes.Voice,
            InputModes.Keyboard,
            "explicit command"));
    }

    [Theory]
    [InlineData("ambiguity_type", "enum", " input_vs_command ", "context_default", "ambiguous", "accepted")]
    [InlineData("ambiguity_type", "enum", "command_vs_input", "context_default", "ambiguous", "accepted")]
    [InlineData("resolution_strategy", "enum", "input_vs_command", "CONTEXT_DEFAULT", "ambiguous", "accepted")]
    [InlineData("signal_class", "enum", "input_vs_command", "context_default", " ambiguous ", "accepted")]
    [InlineData("result", "enum", "input_vs_command", "context_default", "ambiguous", "ACCEPTED")]
    public async Task AnAmbiguityMemberTheModelDoesNotState_IsRefusedAtThatMemberAndKeyword(
        string member,
        string keyword,
        string ambiguityType,
        string resolutionStrategy,
        string signalClass,
        string result)
    {
        await AssertRefusedAsync(member, keyword, (telemetry, session) => telemetry.TrackAmbiguityResolved(
            session,
            ambiguityType,
            resolutionStrategy,
            signalClass,
            result,
            fieldId: null,
            commandId: null));
    }

    [Theory]
    [InlineData("intent", "pattern", " help ")]
    [InlineData("intent", "pattern", "   ")]
    public async Task ACommandNameTheModelRefuses_IsRefusedAtThatMemberAndKeyword(string member, string keyword, string commandId)
    {
        await AssertRefusedAsync(member, keyword, (telemetry, session) => telemetry.TrackGlobalCommand(session, commandId));
    }

    [Fact]
    public async Task ACommandNameLongerThanTheModelAllows_IsRefusedAtItsMaximum()
    {
        await AssertRefusedAsync("command_id", "maxLength", (telemetry, session) => telemetry.TrackDestructiveCommandResolved(
            session,
            new string('h', 65),
            confirmed: true));
    }

    [Theory]
    [InlineData("failure_reason", "pattern", " no_match ")]
    [InlineData("failure_reason", "pattern", "   ")]
    [InlineData("failure_reason", "type", null)]
    public async Task AFailureReasonTheModelRefuses_IsRefusedAtThatMemberAndKeyword(string member, string keyword, string failureReason)
    {
        await AssertRefusedAsync(member, keyword, (telemetry, session) =>
            telemetry.TrackCommandRecognitionFailure(session, failureReason, commandId: null));
    }

    [Fact]
    public async Task ARepairModeLongerThanTheModelAllows_IsRefusedInsteadOfShortened()
    {
        await AssertRefusedAsync(
            "repair_mode",
            "maxLength",
            (telemetry, session) => telemetry.TrackRepairAttempt(session, "email", new string('r', 129)),
            preparedStudyRun: false);
    }

    [Theory]
    [InlineData("application_id", "pattern", " test-app ")]
    [InlineData("application_id", "pattern", "9app")]
    [InlineData("application_id", "pattern", "invalid/app")]
    [InlineData("application_id", "pattern", "   ")]
    [InlineData("application_id", "minLength", "")]
    public async Task AnApplicationIdTheModelRefuses_IsRefusedAtThatMemberAndKeyword(string member, string keyword, string? applicationId)
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(preparedStudyRun: true, logger: logger);
        var exception = await Assert.ThrowsAsync<JsonException>(() => telemetry.StartSessionAsync(
            applicationId!,
            "form-1",
            "de",
            EvaluationChannel.Voice,
            CancellationToken.None));

        Assert.Contains("/" + member + " " + keyword, exception.Message, StringComparison.Ordinal);

        // Ein abgelehnter Start darf keine Protokolleinträge erzeugen.
        Assert.Empty(logger.Entries);
    }

    // Eine fehlende application_id wird bereits vor der ersten Ereignishülle als fehlendes Argument abgelehnt.
    [Fact]
    public async Task AMissingApplicationId_IsRefusedBeforeTheFirstLineExists()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(preparedStudyRun: true, logger: logger);

        await Assert.ThrowsAsync<ArgumentNullException>(() => telemetry.StartSessionAsync(
            null!,
            "form-1",
            "de",
            EvaluationChannel.Voice,
            CancellationToken.None));

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task TheCanonicalMembers_AreWrittenExactlyAsTheyWereReported()
    {
        var store = new RecordingCheckpointStore();
        var telemetry = CreateTelemetry(preparedStudyRun: true, store);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackConstraintViolation(
            session,
            "email",
            "form.required",
            TelemetrySeverityLevels.Error,
            ConstraintScopes.FieldGroup,
            "group-1",
            TelemetryErrorTriggers.UserInput);

        var data = store.LastDataOf(TelemetryEventNames.ValidationErrorOpened);
        Assert.Equal("email", data.GetProperty("field_id").GetString());
        Assert.Equal(TelemetryStudyDiagnosisCodes.Validation, data.GetProperty("error_code").GetString());
        Assert.Equal(TelemetrySeverityLevels.Error, data.GetProperty("severity").GetString());
        Assert.Equal("group-1", data.GetProperty("field_group_id").GetString());
        Assert.Equal(TelemetryErrorTriggers.UserInput, data.GetProperty("trigger").GetString());

        // Der interne Gültigkeitsbereich wird auf den entsprechenden Telemetriewert abgebildet.
        Assert.Equal(TelemetryScopes.FieldGroup, data.GetProperty("scope").GetString());
    }

    [Fact]
    public async Task ARepairModeAtTheStatedMaximum_IsWrittenWhole()
    {
        var repairMode = new string('r', 128);
        var store = new RecordingCheckpointStore();
        var telemetry = CreateTelemetry(preparedStudyRun: false, store);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackRepairAttempt(session, "email", repairMode);

        var data = store.LastDataOf(TelemetryEventNames.ConversationalPacingSignal);
        Assert.Equal(repairMode, data.GetProperty("repair_mode").GetString());
    }

    private static async Task AssertRefusedAsync(
        string member,
        string keyword,
        Action<IEvaluationTelemetry, EvaluationSession> act,
        bool preparedStudyRun = true)
    {
        var telemetry = CreateTelemetry(preparedStudyRun);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        var exception = Assert.Throws<JsonException>(() => act(telemetry, session));

        Assert.Contains("/data/" + member + " " + keyword, exception.Message, StringComparison.Ordinal);
    }

    private static IEvaluationTelemetry CreateTelemetry(
        bool preparedStudyRun,
        IEvaluationTelemetryOutboxStore? store = null,
        CapturingLogger<EvaluationTelemetryImpl>? logger = null)
    {
        logger ??= new CapturingLogger<EvaluationTelemetryImpl>();
        var context = preparedStudyRun
            ? new TestStudyTelemetryContext("P123")
            : new TestStudyTelemetryContext(null);
        var outbox = EvaluationTelemetryTestOutboxFactory.Create(context, logger, store);
        var sessionStateStore = new EvaluationTelemetrySessionStateStore();
        var envelopeEmitter = new EvaluationTelemetryEnvelopeEmitter(context, outbox, new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        var statusPayloads = new EvaluationStatusTelemetryPayloads();
        var pacingTelemetry = new EvaluationConversationalPacingTelemetry(
            sessionStateStore,
            envelopeEmitter,
            new EvaluationConversationalPacingTelemetryPayloads(),
            context);
        return new EvaluationTelemetryImpl(
            new EvaluationSessionTelemetry(
                context,
                sessionStateStore,
                envelopeEmitter,
                new EvaluationSessionTelemetryPayloads(),
                pacingTelemetry),
            new EvaluationFormTelemetry(
                sessionStateStore,
                envelopeEmitter,
                new EvaluationFieldTelemetryPayloads(),
                statusPayloads,
                pacingTelemetry,
                new EvaluationTelemetryDetailedStatusChangeDetector(statusPayloads)),
            new EvaluationCommandTelemetry(
                envelopeEmitter,
                new EvaluationCommandTelemetryPayloads(),
                pacingTelemetry));
    }
}
