using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;

using EvaluationTelemetryImpl = Sprechen.VoiceAssistant.EvaluationTelemetry.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Services;

public sealed class EvaluationTelemetryTests
{
    [Fact]
    public async Task StartSession_EmitsTaskStart()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);

        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        Assert.Equal("test-app", session.ApplicationId);
        Assert.Equal("form-1", session.FormId);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal("task_start", entry.Values["event"]);
        Assert.Equal("B", entry.Values["condition"]);
        Assert.Equal("test-app", entry.Values["application_id"]);
        Assert.Equal(TelemetryEnvelopeContextDefaults.UnknownParticipantId, entry.Values["participant_id"]);
        Assert.Equal(TelemetryRecordProvenances.Demonstration, entry.Values["record_provenance"]);
        Assert.Equal(1, entry.Values["state_version"]);
    }

    [Fact]
    public async Task StartSession_WithStudyContext_UsesParticipantAndTheRunningFormAsTask()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var studyContext = new TestStudyTelemetryContext("P123");
        var telemetry = CreateTelemetry(logger, studyContext);

        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);
        telemetry.TrackGlobalCommand(session, "status");

        Assert.Equal(2, logger.Entries.Count);

        var startEntry = logger.Entries[0];
        Assert.Equal("P123", startEntry.Values["participant_id"]);
        Assert.Equal("test-app", startEntry.Values["application_id"]);
        Assert.Equal("form-1", startEntry.Values["task_id"]);
        Assert.Equal(TelemetryRecordProvenances.Participant, startEntry.Values["record_provenance"]);
        // task_start trägt keine ereignisspezifischen Nutzdaten und übernimmt keinen Formularzustand.
        // Die gemeinsame Übergangsliste bleibt deshalb leer.
        Assert.Equal("{\"canonical_transitions\":[]}", Assert.IsType<string>(startEntry.Values["data"]));

        // Auch das folgende Befehlsereignis trägt die Zuordnung zu Person, Anwendung und Aufgabe.
        var commandEntry = logger.Entries[1];
        Assert.Equal("P123", commandEntry.Values["participant_id"]);
        Assert.Equal("test-app", commandEntry.Values["application_id"]);
        Assert.Equal("form-1", commandEntry.Values["task_id"]);
        Assert.Equal(TelemetryRecordProvenances.Participant, commandEntry.Values["record_provenance"]);
    }

    [Fact]
    public async Task EndSession_EmitsTaskEndWithSubmittedOutcome()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        await telemetry.EndSessionAsync(
            session,
            EvaluationOutcome.Submitted,
            new EvaluationTaskCompletion(3, 3, 0, ParticipantTaskTerminationClassification.Completed, TelemetryMismatchCodes.None),
            CancellationToken.None);
        await WaitForEntriesAsync(logger, 2);

        Assert.Equal(2, logger.Entries.Count);
        var endEntry = logger.Entries[1];
        Assert.Equal("task_end", endEntry.Values["event"]);
        var data = Assert.IsType<string>(endEntry.Values["data"]);
        Assert.Contains("\"end_state\":\"process_submitted\"", data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EndSession_WhenTheTargetIsReachedWithoutSubmitting_RecordsAReachedTarget()
    {
        // Ein erreichtes Aufgabenziel kann auch ohne Absenden des gesamten Ablaufs gemeldet werden.
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        await telemetry.EndSessionAsync(
            session,
            EvaluationOutcome.Aborted,
            new EvaluationTaskCompletion(4, 4, 0, ParticipantTaskTerminationClassification.SessionAbandoned, TelemetryMismatchCodes.None),
            CancellationToken.None);
        await WaitForEntriesAsync(logger, 2);

        var endEntry = logger.Entries[^1];
        var data = Assert.IsType<string>(endEntry.Values["data"]);
        Assert.Contains("\"end_state\":\"task_target_reached\"", data, StringComparison.Ordinal);
    }

    [Fact]
    public void ParticipantTaskTerminationClassification_ContainsOnlyParticipantValues()
    {
        Assert.Equal(
            [
                ParticipantTaskTerminationClassification.Completed,
                ParticipantTaskTerminationClassification.ParticipantCancelled,
                ParticipantTaskTerminationClassification.SessionAbandoned,
                ParticipantTaskTerminationClassification.TechnicalFailure,
            ],
            Enum.GetValues<ParticipantTaskTerminationClassification>());
        Assert.DoesNotContain(
            TelemetryTaskTerminationClassifications.StudyDirectedAbort,
            Enum.GetValues<ParticipantTaskTerminationClassification>()
                .Select(classification => classification.ToModelValue()));
    }

    [Fact]
    public async Task ConversationalPacingSignal_IsNotEmittedInAStudyRun()
    {
        // Bei gesetztem Studienkontext wird das zusätzliche Diagnosesignal zum Dialogtempo nicht ausgegeben.
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var studyContext = new TestStudyTelemetryContext("P123");
        var telemetry = CreateTelemetry(logger, studyContext);
        var session = await telemetry.StartSessionAsync(
            "test-app",
            "form-1",
            "de",
            EvaluationChannel.Voice,
            CancellationToken.None);

        telemetry.TrackGlobalCommand(session, "help");

        Assert.DoesNotContain(
            logger.Entries,
            entry => string.Equals(entry.Values["event"] as string, "conversational_pacing_signal", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConversationalPacingSignal_IsStillEmittedOutsideAStudyRun()
    {
        // Ohne Studienkontext bleibt das Diagnosesignal zum Dialogtempo verfügbar.
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync(
            "test-app",
            "form-1",
            "de",
            EvaluationChannel.Voice,
            CancellationToken.None);

        telemetry.TrackGlobalCommand(session, "help");

        Assert.Contains(
            logger.Entries,
            entry => string.Equals(entry.Values["event"] as string, "conversational_pacing_signal", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EndSession_CountsTheStepsTheProducerItselfRecorded()
    {
        // Die Prüfung liest die vom Telemetrieerzeuger protokollierten Fokusübergänge
        // und leitet deren Anzahl daraus ab.
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger, new TestStudyTelemetryContext("P123"));
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackFieldFocus(session, "contactEmail");
        telemetry.TrackFieldFocus(session, "contactPhone");

        await telemetry.EndSessionAsync(
            session,
            EvaluationOutcome.Submitted,
            new EvaluationTaskCompletion(2, 2, 0, ParticipantTaskTerminationClassification.Completed, TelemetryMismatchCodes.None),
            CancellationToken.None);
        await WaitForEntriesAsync(logger, 4);

        Assert.Equal(
            ["task_start", "focus_change", "focus_change", "task_end"],
            logger.Entries.Select(entry => Assert.IsType<string>(entry.Values["event"])).ToArray());
        Assert.All(logger.Entries, entry =>
        {
            Assert.Equal("P123", entry.Values["participant_id"]);
            Assert.Equal("test-app", entry.Values["application_id"]);
            Assert.Equal("form-1", entry.Values["task_id"]);
            Assert.Equal(TelemetryRecordProvenances.Participant, entry.Values["record_provenance"]);
        });

        AssertFocusTransition(logger.Entries[1], "contactEmail");
        AssertFocusTransition(logger.Entries[2], "contactPhone");

        var data = Assert.IsType<string>(logger.Entries[^1].Values["data"]);
        using var document = JsonDocument.Parse(data);
        Assert.Empty(document.RootElement.GetProperty("canonical_transitions").EnumerateArray());
        Assert.Equal(2, DeduplicatedStepCount(logger.Entries));
    }

    [Fact]
    public async Task EndSession_WhenARequiredFieldIsStillOpen_WritesTheSuppliedMismatchCode()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        // Das übergebene Ergebnis meldet Absenden, obwohl noch erforderliche Felder offen sind.
        // Die Telemetrie schreibt den gelieferten Abweichungscode und leitet ihn nicht aus den Zählern ab.
        await telemetry.EndSessionAsync(
            session,
            EvaluationOutcome.Submitted,
            new EvaluationTaskCompletion(4, 2, 0, ParticipantTaskTerminationClassification.Completed, TelemetryMismatchCodes.MissingRequired),
            CancellationToken.None);
        await WaitForEntriesAsync(logger, 2);

        var data = Assert.IsType<string>(logger.Entries[^1].Values["data"]);
        using var document = JsonDocument.Parse(data);
        Assert.Equal("process_submitted", document.RootElement.GetProperty("end_state").GetString());
        Assert.Equal(TelemetryMismatchCodes.MissingRequired, document.RootElement.GetProperty("mismatch_code").GetString());
        Assert.Equal(TelemetryCorrectnessBases.LogAndSystemCheck, document.RootElement.GetProperty("correctness_basis").GetString());
        AssertJsonObjectPropertyNames(document.RootElement, "end_state", "mismatch_code", "correctness_basis");
    }

    [Fact]
    public async Task EndSession_AfterRepairAttempt_EmitsTaskEnd()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackRepairAttempt(session, "field-1", "no_input");
        await telemetry.EndSessionAsync(
            session,
            EvaluationOutcome.Submitted,
            new EvaluationTaskCompletion(3, 3, 0, ParticipantTaskTerminationClassification.Completed, TelemetryMismatchCodes.None),
            CancellationToken.None);
        await WaitForEntriesAsync(logger, 4);

        Assert.Equal(4, logger.Entries.Count);
        var endEntry = logger.Entries[3];
        Assert.Equal("task_end", endEntry.Values["event"]);
        var data = Assert.IsType<string>(endEntry.Values["data"]);
        Assert.Contains("\"end_state\":\"process_submitted\"", data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TrackFieldFocus_LogsFocusChangeEnvelope()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Html, CancellationToken.None);

        telemetry.TrackFieldFocus(session, "field-1");

        Assert.Equal(2, logger.Entries.Count);
        var entry = logger.Entries[1];
        Assert.Equal("focus_change", entry.Values["event"]);
        Assert.Equal("A", entry.Values["condition"]);
        // Die erste Zustandsübernahme verwendet die bereits laufende Version.
        Assert.Equal(1, entry.Values["state_version"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        using var document = JsonDocument.Parse(data);
        AssertJsonObjectPropertyNames(document.RootElement, "field_id");
        Assert.Equal("field-1", document.RootElement.GetProperty("field_id").GetString());
    }

    [Fact]
    public async Task TrackFieldFocus_WhenFieldDoesNotChange_DoesNotEmitDuplicateFocusChange()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Html, CancellationToken.None);

        telemetry.TrackFieldFocus(session, "field-1");
        telemetry.TrackFieldFocus(session, "field-1");
        telemetry.TrackFieldFocus(session, "field-2");

        Assert.Equal(3, logger.Entries.Count);
        Assert.Equal("task_start", logger.Entries[0].Values["event"]);
        Assert.Equal("focus_change", logger.Entries[1].Values["event"]);
        Assert.Equal("focus_change", logger.Entries[2].Values["event"]);
        // Der Wechsel zu field-2 erhöht die Version einmal; die wiederholte Meldung für field-1 nicht.
        Assert.Equal(2, logger.Entries[2].Values["state_version"]);
    }

    [Fact]
    public async Task TrackFieldAttempt_LogsValueChangeEnvelope()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "task-form", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackFieldAttempt(session, "field-1", "voice", success: true, sttConfidencePercent: 85);

        Assert.Equal(3, logger.Entries.Count);
        var entry = logger.Entries[1];
        Assert.Equal("value_change", entry.Values["event"]);
        Assert.Equal("B", entry.Values["condition"]);
        Assert.Equal(TelemetryEnvelopeContextDefaults.UnknownParticipantId, entry.Values["participant_id"]);
        Assert.Equal("task-form", entry.Values["task_id"]);
        Assert.Equal(TelemetryRecordProvenances.Demonstration, entry.Values["record_provenance"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        using var document = JsonDocument.Parse(data);
        AssertJsonObjectPropertyNames(document.RootElement, "field_id", "stt_confidence");
        Assert.Equal("field-1", document.RootElement.GetProperty("field_id").GetString());
        Assert.Equal(85, document.RootElement.GetProperty("stt_confidence").GetInt32());

        var pacingEntry = logger.Entries[2];
        Assert.Equal("conversational_pacing_signal", pacingEntry.Values["event"]);
        var pacingData = Assert.IsType<string>(pacingEntry.Values["data"]);
        using var pacingDocument = JsonDocument.Parse(pacingData);
        AssertJsonObjectPropertyNames(
            pacingDocument.RootElement,
            "signal_type",
            "field_id",
            "input_mode",
            "success",
            "stt_confidence");
        Assert.Equal("field_attempt", pacingDocument.RootElement.GetProperty("signal_type").GetString());
        Assert.Equal("voice", pacingDocument.RootElement.GetProperty("input_mode").GetString());
        Assert.True(pacingDocument.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(85, pacingDocument.RootElement.GetProperty("stt_confidence").GetInt32());
    }

    [Fact]
    public async Task TrackFieldAttempt_WhenNotSuccessful_LogsPacingSignalButNoValueChange()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "task-form", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackFieldAttempt(session, "field-1", "voice", success: false, sttConfidencePercent: 20);

        Assert.Equal(2, logger.Entries.Count);
        Assert.Equal("task_start", logger.Entries[0].Values["event"]);
        Assert.Equal("conversational_pacing_signal", logger.Entries[1].Values["event"]);
        var data = Assert.IsType<string>(logger.Entries[1].Values["data"]);
        using var document = JsonDocument.Parse(data);
        AssertJsonObjectPropertyNames(
            document.RootElement,
            "signal_type",
            "field_id",
            "input_mode",
            "success",
            "stt_confidence");
        Assert.Equal("field_attempt", document.RootElement.GetProperty("signal_type").GetString());
        Assert.False(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(20, document.RootElement.GetProperty("stt_confidence").GetInt32());
    }

    [Fact]
    public async Task TrackConstraintViolation_RecognitionFault_LogsSttErrorWithTheStudyDiagnosis()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackConstraintViolation(session, "field-1", "recognized_as_empty", "warning", TelemetryErrorTriggers.SystemRecognition);

        Assert.Equal(2, logger.Entries.Count);
        var entry = logger.Entries[1];
        Assert.Equal("stt_error", entry.Values["event"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        Assert.Contains("\"error_code\":\"D-STT\"", data, StringComparison.Ordinal);
        Assert.Contains("\"severity\":\"warning\"", data, StringComparison.Ordinal);
        Assert.Contains("\"scope\":\"field\"", data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TrackConstraintViolation_Silence_LogsTheNoSpeechEventAndNoErrorLine()
    {
        // Fehlende Sprache wird als eigenes Ereignis gemeldet.
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackConstraintViolation(session, "field-1", "no_speech_detected", "warning", TelemetryErrorTriggers.SystemRecognition);

        Assert.Equal(3, logger.Entries.Count);
        Assert.Equal("no_speech_detected", logger.Entries[1].Values["event"]);
        Assert.Contains("\"field_id\":\"field-1\"", (string)logger.Entries[1].Values["data"]!, StringComparison.Ordinal);
        // Das zusätzliche Diagnosesignal ist im hier verwendeten Kontext ohne Studienkennung zulässig.
        Assert.Equal("conversational_pacing_signal", logger.Entries[2].Values["event"]);
    }

    [Fact]
    public async Task TrackConstraintViolation_JudgementRestingOnTheTranscript_WritesNoStudyErrorLine()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackConstraintViolation(session, "field-1", "unrecognized_input", "warning", TelemetryErrorTriggers.SystemRecognition);

        Assert.Single(logger.Entries);
    }

    [Fact]
    public async Task TrackConstraintViolation_LowSttConfidence_LogsSttErrorWithoutClearedPairing()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        // Niedrige Erkennungskonfidenz wird als stt_error gemeldet. Sie eröffnet keinen
        // Validierungsfehler, den die spätere erfolgreiche Eingabe wieder schließen könnte.
        telemetry.TrackConstraintViolation(session, "field-1", "low_stt_confidence", "warning", TelemetryErrorTriggers.SystemRecognition);
        telemetry.TrackFieldAttempt(session, "field-1", "voice", success: true);

        Assert.Equal(4, logger.Entries.Count);
        Assert.Equal("stt_error", logger.Entries[1].Values["event"]);
        var data = Assert.IsType<string>(logger.Entries[1].Values["data"]);
        Assert.Contains("\"error_code\":\"D-STT\"", data, StringComparison.Ordinal);
        Assert.Equal("value_change", logger.Entries[2].Values["event"]);
        Assert.Equal("conversational_pacing_signal", logger.Entries[3].Values["event"]);
        Assert.DoesNotContain(logger.Entries, entry =>
            entry.Values["event"] is string eventName
            && string.Equals(eventName, "validation_error_cleared", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TrackConstraintViolation_WithFieldGroupScope_MapsToLoggingScopeAndFieldGroupId()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackConstraintViolation(session, "field-1", "E01", "warning", ConstraintScopes.FieldGroup, "group-1", TelemetryErrorTriggers.UserInput);

        Assert.Equal(2, logger.Entries.Count);
        var entry = logger.Entries[1];
        Assert.Equal("validation_error_opened", entry.Values["event"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        Assert.Contains("\"scope\":\"field_group\"", data, StringComparison.Ordinal);
        Assert.Contains("\"field_group_id\":\"group-1\"", data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TrackNavigationError_WithCrossGroupScope_EmitsNavigationError()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackNavigationError(session, NavigationErrorCodes.JumpTargetNotFound, ConstraintScopes.CrossGroup);

        Assert.Equal(2, logger.Entries.Count);
        var entry = logger.Entries[1];
        Assert.Equal("navigation_error", entry.Values["event"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        Assert.Contains("\"error_code\":\"D-NAVIGATION\"", data, StringComparison.Ordinal);
        Assert.Contains("\"severity\":\"error\"", data, StringComparison.Ordinal);
        Assert.Contains("\"scope\":\"cross_group\"", data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TrackConstraintViolation_ThenSuccessfulFieldAttempt_LogsValidationErrorCleared()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackConstraintViolation(session, "field-1", "E01", "error", TelemetryErrorTriggers.UserInput);
        telemetry.TrackFieldAttempt(session, "field-1", "voice", success: true);

        Assert.Equal(5, logger.Entries.Count);
        Assert.Equal("validation_error_opened", logger.Entries[1].Values["event"]);
        Assert.Equal("value_change", logger.Entries[2].Values["event"]);
        Assert.DoesNotContain("stt_confidence", (string)logger.Entries[2].Values["data"]!);
        Assert.Equal("conversational_pacing_signal", logger.Entries[3].Values["event"]);
        Assert.Equal("validation_error_cleared", logger.Entries[4].Values["event"]);
    }

    [Theory]
    [InlineData(TelemetryErrorTriggers.Load)]
    [InlineData(TelemetryErrorTriggers.UserInput)]
    [InlineData(TelemetryErrorTriggers.SubmitAttempt)]
    [InlineData(TelemetryErrorTriggers.RuleVisibility)]
    [InlineData(TelemetryErrorTriggers.SystemRecognition)]
    public async Task TrackConstraintViolation_WritesTheTriggerTheCallingTurnNamed(string trigger)
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackConstraintViolation(session, "field-1", "form.required", "error", trigger);

        var entry = logger.Entries[1];
        Assert.Equal("validation_error_opened", entry.Values["event"]);
        Assert.Contains($"\"trigger\":\"{trigger}\"", (string)entry.Values["data"]!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TrackConstraintViolation_RoutedToSttError_CarriesNoTrigger()
    {
        // Das stt_error-Ereignis trägt kein trigger-Feld.
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackConstraintViolation(
            session,
            "field-1",
            "capture_interrupted",
            "warning",
            TelemetryErrorTriggers.SystemRecognition);

        var entry = logger.Entries[1];
        Assert.Equal("stt_error", entry.Values["event"]);
        Assert.DoesNotContain("\"trigger\"", (string)entry.Values["data"]!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TrackConstraintViolation_RepeatedOnTheSameField_CountsEveryOccurrence()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        for (var attempt = 0; attempt < 3; attempt += 1)
        {
            telemetry.TrackConstraintViolation(session, "field-1", "form.required", "error", TelemetryErrorTriggers.UserInput);
        }

        var opened = logger.Entries
            .Where(entry => string.Equals((string?)entry.Values["event"], "validation_error_opened", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(3, opened.Length);
        Assert.All(
            opened,
            entry => Assert.Contains("\"error_code\":\"D-VALIDATION\"", (string)entry.Values["data"]!, StringComparison.Ordinal));
    }

    [Fact]
    public async Task TrackGlobalCommand_LogsIntentRecognized()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Html, CancellationToken.None);

        telemetry.TrackGlobalCommand(session, "help");

        Assert.Equal(2, logger.Entries.Count);
        var entry = logger.Entries[1];
        Assert.Equal("intent_recognized", entry.Values["event"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        Assert.Contains("\"intent\":\"help\"", data, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(data);
        AssertJsonObjectPropertyNames(document.RootElement, "intent");
    }

    [Theory]
    [InlineData("help", "help_request")]
    [InlineData("repeat", "repeat_request")]
    public async Task TrackGlobalCommand_ForPacingCommands_LogsConversationalPacingSignal(
        string commandId,
        string expectedSignalType)
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackGlobalCommand(session, commandId);

        Assert.Equal(3, logger.Entries.Count);
        Assert.Equal("intent_recognized", logger.Entries[1].Values["event"]);
        var entry = logger.Entries[2];
        Assert.Equal("conversational_pacing_signal", entry.Values["event"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        using var document = JsonDocument.Parse(data);
        AssertJsonObjectPropertyNames(document.RootElement, "signal_type", "command_id");
        Assert.Equal(expectedSignalType, document.RootElement.GetProperty("signal_type").GetString());
        Assert.Equal(commandId, document.RootElement.GetProperty("command_id").GetString());
    }

    [Fact]
    public async Task TrackGlobalCommand_ForNonPacingCommand_DoesNotLogConversationalPacingSignalOrRetry()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackStateEntry(session, "LISTENING");
        telemetry.TrackGlobalCommand(session, "status");
        telemetry.TrackStateEntry(session, "LISTENING");

        Assert.Equal(2, logger.Entries.Count);
        Assert.Equal("intent_recognized", logger.Entries[1].Values["event"]);
        Assert.DoesNotContain(logger.Entries, entry =>
            entry.Values["event"] is string eventName
            && string.Equals(eventName, "conversational_pacing_signal", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TrackCommandRecognitionFailure_LogsCommandRecognitionFailedEvent()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackCommandRecognitionFailure(session, "low_stt_confidence", commandId: "next");

        Assert.Equal(3, logger.Entries.Count);
        var entry = logger.Entries[1];
        Assert.Equal("command_recognition_failed", entry.Values["event"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        using var document = JsonDocument.Parse(data);
        AssertJsonObjectPropertyNames(document.RootElement, "failure_reason", "command_id");
        Assert.Equal("low_stt_confidence", document.RootElement.GetProperty("failure_reason").GetString());
        Assert.Equal("next", document.RootElement.GetProperty("command_id").GetString());

        var pacingEntry = logger.Entries[2];
        Assert.Equal("conversational_pacing_signal", pacingEntry.Values["event"]);
        var pacingData = Assert.IsType<string>(pacingEntry.Values["data"]);
        using var pacingDocument = JsonDocument.Parse(pacingData);
        AssertJsonObjectPropertyNames(pacingDocument.RootElement, "signal_type", "command_id", "failure_reason");
        Assert.Equal("command_recognition_failure", pacingDocument.RootElement.GetProperty("signal_type").GetString());
    }

    [Fact]
    public async Task TrackCommandRecognitionFailure_WithoutCommandId_OmitsCommandId()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackCommandRecognitionFailure(session, "unknown_command", commandId: null);

        Assert.Equal(3, logger.Entries.Count);
        var entry = logger.Entries[1];
        Assert.Equal("command_recognition_failed", entry.Values["event"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        using var document = JsonDocument.Parse(data);
        AssertJsonObjectPropertyNames(document.RootElement, "failure_reason");
        Assert.Equal("unknown_command", document.RootElement.GetProperty("failure_reason").GetString());

        var pacingEntry = logger.Entries[2];
        Assert.Equal("conversational_pacing_signal", pacingEntry.Values["event"]);
        var pacingData = Assert.IsType<string>(pacingEntry.Values["data"]);
        using var pacingDocument = JsonDocument.Parse(pacingData);
        AssertJsonObjectPropertyNames(pacingDocument.RootElement, "signal_type", "failure_reason");
        Assert.Equal("command_recognition_failure", pacingDocument.RootElement.GetProperty("signal_type").GetString());
    }

    [Fact]
    public async Task TrackNoSpeechDetected_LogsNoSpeechDetectedEvent()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackNoSpeechDetected(session, "field-1");

        Assert.Equal(3, logger.Entries.Count);
        var entry = logger.Entries[1];
        Assert.Equal("no_speech_detected", entry.Values["event"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        using var document = JsonDocument.Parse(data);
        AssertJsonObjectPropertyNames(document.RootElement, "field_id");
        Assert.Equal("field-1", document.RootElement.GetProperty("field_id").GetString());

        var pacingEntry = logger.Entries[2];
        Assert.Equal("conversational_pacing_signal", pacingEntry.Values["event"]);
        var pacingData = Assert.IsType<string>(pacingEntry.Values["data"]);
        using var pacingDocument = JsonDocument.Parse(pacingData);
        AssertJsonObjectPropertyNames(pacingDocument.RootElement, "signal_type", "field_id");
        Assert.Equal("no_input", pacingDocument.RootElement.GetProperty("signal_type").GetString());
    }

    [Fact]
    public void TrackNoSpeechDetected_WithoutAFormSession_OmitsFieldIdAndCarriesCondition()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);

        telemetry.TrackNoSpeechDetected();

        var entry = Assert.Single(logger.Entries);
        Assert.Equal("no_speech_detected", entry.Values["event"]);
        Assert.Equal("B", entry.Values["condition"]);
        Assert.Equal(TelemetryEnvelopeContextDefaults.UnknownParticipantId, entry.Values["participant_id"]);
        Assert.Equal(TelemetryEnvelopeContextDefaults.UnknownTaskId, entry.Values["task_id"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        using var document = JsonDocument.Parse(data);
        Assert.False(document.RootElement.TryGetProperty("field_id", out _));
    }

    [Fact]
    public void TrackNoSpeechDetected_WithoutAFormSession_WhenStudyContextIsPresent_CarriesParticipantAndTask()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var studyContext = new StudyTelemetryContext();
        studyContext.SetStudyContext("p942demo");
        var telemetry = CreateTelemetry(logger, studyContext);

        telemetry.TrackNoSpeechDetected();

        var entry = Assert.Single(logger.Entries);
        Assert.Equal("p942demo", entry.Values["participant_id"]);
        Assert.Equal("B", entry.Values["condition"]);
        Assert.Equal(TelemetryEnvelopeContextDefaults.UnknownTaskId, entry.Values["task_id"]);
        Assert.Equal(TelemetryEnvelopeContextDefaults.UnknownApplicationId, entry.Values["application_id"]);
    }

    [Fact]
    public void TrackNoSpeechDetected_WhenAParticipantTokenIsBound_KeepsTheTokenAndUsesPlaceholderTask()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var studyContext = new StudyTelemetryContext();
        studyContext.SetStudyContext("x9jdln");
        var telemetry = CreateTelemetry(logger, studyContext);

        telemetry.TrackNoSpeechDetected();

        var entry = Assert.Single(logger.Entries);
        Assert.Equal("no_speech_detected", entry.Values["event"]);
        Assert.Equal("x9jdln", entry.Values["participant_id"]);
        Assert.Equal("B", entry.Values["condition"]);
        Assert.Equal(TelemetryEnvelopeContextDefaults.UnknownTaskId, entry.Values["task_id"]);
        Assert.Equal(TelemetryRecordProvenances.Participant, entry.Values["record_provenance"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        using var document = JsonDocument.Parse(data);
        Assert.False(document.RootElement.TryGetProperty("field_id", out _));
    }

    [Fact]
    public async Task ConversationalPacingSignal_AfterListeningWindow_IncludesAttemptButNoSyntheticResponseLatency()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackStateEntry(session, "LISTENING");
        telemetry.TrackFieldAttempt(session, "field-1", "voice", success: true, sttConfidencePercent: 77);

        Assert.Equal(3, logger.Entries.Count);
        Assert.Equal("value_change", logger.Entries[1].Values["event"]);
        Assert.Equal("conversational_pacing_signal", logger.Entries[2].Values["event"]);

        var data = Assert.IsType<string>(logger.Entries[2].Values["data"]);
        using var document = JsonDocument.Parse(data);
        AssertJsonObjectPropertyNames(
            document.RootElement,
            "signal_type",
            "field_id",
            "input_mode",
            "success",
            "attempt_number",
            "stt_confidence");
        Assert.Equal("field_attempt", document.RootElement.GetProperty("signal_type").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("attempt_number").GetInt32());
        Assert.False(document.RootElement.TryGetProperty("response_latency_ms", out _));
    }

    [Fact]
    public async Task ConversationalPacingSignal_WhenListeningWindowRetries_LogsCaptureRetry()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackStateEntry(session, "LISTENING");
        telemetry.TrackNoSpeechDetected(session, "field-1");
        telemetry.TrackStateEntry(session, "LISTENING");

        Assert.Equal(4, logger.Entries.Count);
        Assert.Equal("conversational_pacing_signal", logger.Entries[2].Values["event"]);
        var noInputData = Assert.IsType<string>(logger.Entries[2].Values["data"]);
        using var noInputDocument = JsonDocument.Parse(noInputData);
        Assert.Equal("no_input", noInputDocument.RootElement.GetProperty("signal_type").GetString());
        Assert.Equal(1, noInputDocument.RootElement.GetProperty("attempt_number").GetInt32());
        Assert.False(noInputDocument.RootElement.TryGetProperty("response_latency_ms", out _));

        Assert.Equal("conversational_pacing_signal", logger.Entries[3].Values["event"]);
        var retryData = Assert.IsType<string>(logger.Entries[3].Values["data"]);
        using var retryDocument = JsonDocument.Parse(retryData);
        AssertJsonObjectPropertyNames(retryDocument.RootElement, "signal_type", "attempt_number");
        Assert.Equal("capture_retry", retryDocument.RootElement.GetProperty("signal_type").GetString());
        Assert.Equal(2, retryDocument.RootElement.GetProperty("attempt_number").GetInt32());
    }

    [Fact]
    public async Task TrackStateEntry_DoesNotMisclassifyDialogStateAsManualStep()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackStateEntry(session, "VALIDATING");

        var entry = Assert.Single(logger.Entries);
        Assert.Equal("task_start", entry.Values["event"]);
    }

    [Fact]
    public async Task TransitionAttacher_ManualStepAcceptsOneCompleteModeledTransition()
    {
        var input = ManualStepPayload(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [TelemetryPayloadProperties.CanonicalClass] = TelemetryCanonicalClasses.FieldCompletionChanged,
                [TelemetryPayloadProperties.TransitionScope] = TelemetryTransitionScopes.Field,
                [TelemetryPayloadProperties.ObjectId] = "field-1",
                [TelemetryPayloadProperties.ResultingState] = TelemetryResultingStates.Complete,
            });

        var result = EvaluationTelemetryTransitionAttacher.Attach(TelemetryEventNames.ManualStep, input);

        var transition = Assert.Single(result.Transitions);
        Assert.Equal(TelemetryCanonicalClasses.FieldCompletionChanged, transition.CanonicalClass);
        Assert.Equal(TelemetryTransitionScopes.Field, transition.Scope);
        Assert.Equal("field-1", transition.ObjectId);
        Assert.Equal(TelemetryResultingStates.Complete, transition.ResultingState);
    }

    [Fact]
    public void TransitionAttacher_ManualStepRejectsMissingOrInvalidTransitions()
    {
        var empty = ManualStepPayload();
        var invalid = ManualStepPayload(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [TelemetryPayloadProperties.CanonicalClass] = TelemetryCanonicalClasses.FieldTargetChanged,
                [TelemetryPayloadProperties.TransitionScope] = TelemetryTransitionScopes.FieldGroup,
                [TelemetryPayloadProperties.ObjectId] = "group-1",
                [TelemetryPayloadProperties.ResultingState] = TelemetryResultingStates.Targeted,
            });

        Assert.Throws<ArgumentException>(() =>
            EvaluationTelemetryTransitionAttacher.Attach(TelemetryEventNames.ManualStep, empty));
        Assert.Throws<ArgumentException>(() =>
            EvaluationTelemetryTransitionAttacher.Attach(TelemetryEventNames.ManualStep, invalid));
    }

    [Fact]
    public async Task TrackRepairAttempt_LogsSystemFeedback()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackRepairAttempt(session, "field-1", "charEdit");

        Assert.Equal(3, logger.Entries.Count);
        var entry = logger.Entries[1];
        Assert.Equal("system_feedback", entry.Values["event"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        Assert.Contains("\"type\":\"help\"", data, StringComparison.Ordinal);

        var pacingEntry = logger.Entries[2];
        Assert.Equal("conversational_pacing_signal", pacingEntry.Values["event"]);
        var pacingData = Assert.IsType<string>(pacingEntry.Values["data"]);
        using var pacingDocument = JsonDocument.Parse(pacingData);
        AssertJsonObjectPropertyNames(pacingDocument.RootElement, "signal_type", "field_id", "repair_mode");
        Assert.Equal("repair_attempt", pacingDocument.RootElement.GetProperty("signal_type").GetString());
        Assert.Equal("charEdit", pacingDocument.RootElement.GetProperty("repair_mode").GetString());
    }

    [Fact]
    public async Task TrackInputModeChanged_LogsInputModeChangedEvent()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackInputModeChanged(
            session,
            "field-1",
            InputModes.Voice,
            InputModes.Keyboard,
            TelemetryInputModeTriggers.ExplicitCommand);

        Assert.Equal(2, logger.Entries.Count);
        var entry = logger.Entries[1];
        Assert.Equal("input_mode_changed", entry.Values["event"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        Assert.Contains("\"field_id\":\"field-1\"", data, StringComparison.Ordinal);
        Assert.Contains("\"from_mode\":\"voice\"", data, StringComparison.Ordinal);
        Assert.Contains("\"to_mode\":\"keyboard\"", data, StringComparison.Ordinal);
        Assert.Contains("\"trigger\":\"explicit_command\"", data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TrackDestructiveCommandResolved_LogsDestructiveCommandResolvedEvent()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackDestructiveCommandResolved(session, "cancel", confirmed: false, refractoryBlocked: true);

        Assert.Equal(2, logger.Entries.Count);
        var entry = logger.Entries[1];
        Assert.Equal("destructive_command_resolved", entry.Values["event"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        Assert.Contains("\"command_id\":\"cancel\"", data, StringComparison.Ordinal);
        Assert.Contains("\"confirmed\":false", data, StringComparison.Ordinal);
        Assert.Contains("\"refractory_blocked\":true", data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TrackBargeIn_UsesLastFocusedField()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);
        telemetry.TrackFieldFocus(session, "field-1");

        telemetry.TrackBargeIn(session, "dialog.help");

        Assert.Equal(4, logger.Entries.Count);
        var entry = logger.Entries[2];
        Assert.Equal("barge_in", entry.Values["event"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        Assert.Contains("\"field_id\":\"field-1\"", data, StringComparison.Ordinal);
        Assert.Contains("\"interrupted_prompt_id\":\"dialog.help\"", data, StringComparison.Ordinal);

        var pacingEntry = logger.Entries[3];
        Assert.Equal("conversational_pacing_signal", pacingEntry.Values["event"]);
        var pacingData = Assert.IsType<string>(pacingEntry.Values["data"]);
        using var pacingDocument = JsonDocument.Parse(pacingData);
        AssertJsonObjectPropertyNames(pacingDocument.RootElement, "signal_type", "field_id", "interrupted_prompt_id");
        Assert.Equal("barge_in", pacingDocument.RootElement.GetProperty("signal_type").GetString());
    }

    [Fact]
    public async Task TrackAmbiguityResolved_LogsAmbiguityResolvedEvent()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackAmbiguityResolved(
            session,
            ambiguityType: "input_vs_command",
            resolutionStrategy: "context_default",
            signalClass: "ambiguous",
            result: "accepted",
            fieldId: "name",
            commandId: "next");

        Assert.Equal(2, logger.Entries.Count);
        var entry = logger.Entries[1];
        Assert.Equal("ambiguity_resolved", entry.Values["event"]);
        var data = Assert.IsType<string>(entry.Values["data"]);
        Assert.Contains("\"ambiguity_type\":\"input_vs_command\"", data, StringComparison.Ordinal);
        Assert.Contains("\"resolution_strategy\":\"context_default\"", data, StringComparison.Ordinal);
        Assert.Contains("\"signal_class\":\"ambiguous\"", data, StringComparison.Ordinal);
        Assert.Contains("\"result\":\"accepted\"", data, StringComparison.Ordinal);
        Assert.Contains("\"field_id\":\"name\"", data, StringComparison.Ordinal);
        Assert.Contains("\"command_id\":\"next\"", data, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(CommandSignalClass.Clear)]
    [InlineData(CommandSignalClass.Ambiguous)]
    [InlineData(CommandSignalClass.NoCommand)]
    public async Task TrackAmbiguityResolved_AcceptsAllValidSignalClasses(string signalClass)
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        telemetry.TrackAmbiguityResolved(
            session,
            ambiguityType: "input_vs_command",
            resolutionStrategy: "context_default",
            signalClass: signalClass,
            result: "accepted",
            fieldId: "name",
            commandId: null);

        Assert.Equal(2, logger.Entries.Count);
        var entry = logger.Entries[1];
        var data = Assert.IsType<string>(entry.Values["data"]);
        Assert.Contains($"\"signal_class\":\"{signalClass}\"", data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TrackStatusSnapshot_LogsNavigationChangedAndStatusSnapshot()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);
        var snapshot = new FormStatusSnapshot(
            Navigation: new FormStatusNavigationPosition(
                CurrentFormId: null,
                CurrentFormIndex: null,
                TotalForms: null,
                CurrentFieldGroupId: "group-1",
                CurrentFieldGroupIndex: 1,
                TotalFieldGroups: 2,
                CurrentFieldId: "field-1"),
            Progress: new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(2, 4),
                RequiredFieldsOnly: new FormStatusProgressDimension(1, 2),
                FieldGroupBased: new FormStatusProgressDimension(0, 2)),
            CurrentFieldLabel: "Vorname",
            CurrentFieldIndex: 1,
            CurrentFieldGroupTitle: "Persoenliche Daten",
            OpenRequiredFieldCount: 1,
            ValidationErrorCount: 1);

        telemetry.TrackStatusSnapshot(session, snapshot.ToEvaluationStatusSnapshot());

        Assert.Equal(3, logger.Entries.Count);
        Assert.Equal("navigation_changed", logger.Entries[1].Values["event"]);
        Assert.Equal("status_snapshot", logger.Entries[2].Values["event"]);
    }

    [Fact]
    public async Task TrackStatusSnapshot_WhenNavigationIsUnchanged_EmitsOnlyStatusSnapshot()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);
        var snapshot = new FormStatusSnapshot(
            Navigation: new FormStatusNavigationPosition(
                CurrentFormId: null,
                CurrentFormIndex: null,
                TotalForms: null,
                CurrentFieldGroupId: "group-1",
                CurrentFieldGroupIndex: 1,
                TotalFieldGroups: 2,
                CurrentFieldId: "field-1"),
            Progress: new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(2, 4),
                RequiredFieldsOnly: new FormStatusProgressDimension(1, 2),
                FieldGroupBased: new FormStatusProgressDimension(0, 2)),
            CurrentFieldLabel: "Vorname",
            CurrentFieldIndex: 1,
            CurrentFieldGroupTitle: "Persoenliche Daten",
            OpenRequiredFieldCount: 1,
            ValidationErrorCount: 1);

        telemetry.TrackStatusSnapshot(session, snapshot.ToEvaluationStatusSnapshot());
        telemetry.TrackStatusSnapshot(session, snapshot.ToEvaluationStatusSnapshot());

        Assert.Equal(4, logger.Entries.Count);
        Assert.Equal("navigation_changed", logger.Entries[1].Values["event"]);
        Assert.Equal("status_snapshot", logger.Entries[2].Values["event"]);
        Assert.Equal("status_snapshot", logger.Entries[3].Values["event"]);
        // Eine wiederholte Meldung desselben Zustands erhöht dessen Version nicht.
        Assert.Equal(1, logger.Entries[3].Values["state_version"]);
    }

    [Fact]
    public async Task TrackFieldAndGroupStatus_EmitsChangeEventsAndDiffs()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        var firstSnapshot = new FormDetailedStatusSnapshot(
            Fields:
            [
                new FormFieldStatusSnapshot(
                    FieldId: "field-1",
                    FieldGroupId: "group-1",
                    Relevant: true,
                    Required: true,
                    Completed: false,
                    BlockingErrorCodes: ["E01"]),
                new FormFieldStatusSnapshot(
                    FieldId: "field-2",
                    FieldGroupId: "group-2",
                    Relevant: true,
                    Required: true,
                    Completed: false,
                    BlockingErrorCodes: ["E02"])
            ],
            FieldGroups:
            [
                new FormFieldGroupStatusSnapshot(
                    FieldGroupId: "group-1",
                    Relevant: true,
                    Completed: false,
                    OpenGroupErrorCodes: ["E01"]),
                new FormFieldGroupStatusSnapshot(
                    FieldGroupId: "group-2",
                    Relevant: true,
                    Completed: false,
                    OpenGroupErrorCodes: ["E02"])
            ]);

        var secondSnapshot = new FormDetailedStatusSnapshot(
            Fields:
            [
                new FormFieldStatusSnapshot(
                    FieldId: "field-1",
                    FieldGroupId: "group-1",
                    Relevant: true,
                    Required: true,
                    Completed: true,
                    BlockingErrorCodes: []),
                new FormFieldStatusSnapshot(
                    FieldId: "field-2",
                    FieldGroupId: "group-2",
                    Relevant: true,
                    Required: true,
                    Completed: true,
                    BlockingErrorCodes: [])
            ],
            FieldGroups:
            [
                new FormFieldGroupStatusSnapshot(
                    FieldGroupId: "group-1",
                    Relevant: true,
                    Completed: true,
                    OpenGroupErrorCodes: []),
                new FormFieldGroupStatusSnapshot(
                    FieldGroupId: "group-2",
                    Relevant: true,
                    Completed: true,
                    OpenGroupErrorCodes: [])
            ]);

        telemetry.TrackFieldAndGroupStatus(session, firstSnapshot.ToEvaluationDetailedStatus());
        telemetry.TrackFieldAndGroupStatus(session, secondSnapshot.ToEvaluationDetailedStatus());
        await telemetry.EndSessionAsync(
            session,
            EvaluationOutcome.Submitted,
            new EvaluationTaskCompletion(2, 2, 0, ParticipantTaskTerminationClassification.Completed, TelemetryMismatchCodes.None),
            CancellationToken.None);

        Assert.DoesNotContain(logger.Entries, entry =>
            string.Equals(Convert.ToString(entry.Values["event"]), "validation_error_opened", StringComparison.Ordinal));
        var completionEvents = logger.Entries
            .Where(entry => entry.Values["event"] is TelemetryEventNames.FieldCompletionChanged
                or TelemetryEventNames.FieldGroupCompletionChanged)
            .ToArray();
        Assert.Equal(4, completionEvents.Length);
        Assert.All(completionEvents, entry => Assert.Equal(2, entry.Values["state_version"]));
        Assert.DoesNotContain(logger.Entries, entry => entry.Values["event"] is
            TelemetryEventNames.FieldRelevanceChanged or TelemetryEventNames.FieldGroupRelevanceChanged);

        Assert.Equal(4, DeduplicatedStepCount(logger.Entries));
    }

    [Fact]
    public async Task StateVersion_AdvancesPerStateAdoptionNotPerEmittedLine()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Html, CancellationToken.None);

        telemetry.TrackFieldFocus(session, "field-1");
        telemetry.TrackFieldAttempt(session, "field-1", "keyboard", success: true);

        Assert.Equal(3, logger.Entries.Count);

        // task_start übernimmt keinen Formularzustand und erhöht die Version nicht.
        Assert.Equal(1, logger.Entries[0].Values["state_version"]);
        // Der erste Fokuswechsel verwendet die laufende Zustandsversion.
        Assert.Equal(1, logger.Entries[1].Values["state_version"]);
        // Die folgende Wertänderung übernimmt einen neuen Zustand und erhöht die Version.
        Assert.Equal(2, logger.Entries[2].Values["state_version"]);
    }

    [Fact]
    public async Task RepeatedConfirmedChangesOnTheSameFieldRemainDistinctOccurrences()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync(
            "test-app",
            "form-1",
            "de",
            EvaluationChannel.Voice,
            CancellationToken.None);

        telemetry.TrackFieldFocus(session, "field-a");
        telemetry.TrackFieldAttempt(session, "field-a", "voice", success: true);
        telemetry.TrackFieldAttempt(session, "field-a", "voice", success: true);
        await telemetry.EndSessionAsync(
            session,
            EvaluationOutcome.Submitted,
            new EvaluationTaskCompletion(1, 1, 0, ParticipantTaskTerminationClassification.Completed, TelemetryMismatchCodes.None),
            CancellationToken.None);

        var stateChanges = logger.Entries
            .Where(entry => entry.Values["event"] is TelemetryEventNames.FocusChange or TelemetryEventNames.ValueChange)
            .ToArray();
        Assert.Equal([1, 2, 3], stateChanges
            .Select(entry => Assert.IsType<int>(entry.Values["state_version"]))
            .ToArray());

        Assert.Equal(3, DeduplicatedStepCount(logger.Entries));
    }

    [Fact]
    public async Task NavigationSnapshots_EmitOnlyChangedDimensionsAcrossAFieldReturn()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync(
            "test-app",
            "form-1",
            "de",
            EvaluationChannel.Voice,
            CancellationToken.None);

        telemetry.TrackStatusSnapshot(session, CreateStatusSnapshot("field-a", "group-a"));
        telemetry.TrackStatusSnapshot(session, CreateStatusSnapshot("field-b", "group-a"));
        telemetry.TrackStatusSnapshot(session, CreateStatusSnapshot("field-a", "group-a"));
        await telemetry.EndSessionAsync(
            session,
            EvaluationOutcome.Submitted,
            new EvaluationTaskCompletion(1, 1, 0, ParticipantTaskTerminationClassification.Completed, TelemetryMismatchCodes.None),
            CancellationToken.None);

        var navigation = logger.Entries
            .Where(entry => Equals(entry.Values["event"], TelemetryEventNames.NavigationChanged))
            .ToArray();
        Assert.Equal(3, navigation.Length);
        Assert.Equal([1, 2, 3], navigation.Select(entry => Assert.IsType<int>(entry.Values["state_version"])).ToArray());

        var transitionObjects = navigation.Select(entry =>
        {
            using var data = JsonDocument.Parse(Assert.IsType<string>(entry.Values["data"]));
            return data.RootElement.GetProperty("canonical_transitions")
                .EnumerateArray()
                .Select(transition => transition.GetProperty("object_id").GetString()!)
                .ToArray();
        }).ToArray();
        Assert.Equal(["field-a", "group-a"], transitionObjects[0]);
        Assert.Equal(["field-b"], transitionObjects[1]);
        Assert.Equal(["field-a"], transitionObjects[2]);

        Assert.Equal(4, DeduplicatedStepCount(logger.Entries));
    }

    [Fact]
    public async Task FocusAndFollowingNavigationSnapshotShareOneAdoption()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync(
            "test-app",
            "form-1",
            "de",
            EvaluationChannel.Voice,
            CancellationToken.None);

        telemetry.TrackFieldFocus(session, "field-a");
        telemetry.TrackStatusSnapshot(session, CreateStatusSnapshot("field-a", "group-a"));
        await telemetry.EndSessionAsync(
            session,
            EvaluationOutcome.Submitted,
            new EvaluationTaskCompletion(1, 1, 0, ParticipantTaskTerminationClassification.Completed, TelemetryMismatchCodes.None),
            CancellationToken.None);

        var stateChanges = logger.Entries
            .Where(entry => entry.Values["event"] is TelemetryEventNames.FocusChange or TelemetryEventNames.NavigationChanged)
            .ToArray();
        Assert.Equal(2, stateChanges.Length);
        Assert.All(stateChanges, entry => Assert.Equal(1, entry.Values["state_version"]));
        using var navigationData = JsonDocument.Parse(Assert.IsType<string>(stateChanges[1].Values["data"]));
        Assert.Equal(
            ["group-a"],
            navigationData.RootElement.GetProperty("canonical_transitions")
                .EnumerateArray()
                .Select(transition => transition.GetProperty("object_id").GetString()!)
                .ToArray());
        Assert.Equal(2, DeduplicatedStepCount(logger.Entries));
    }
    [Fact]
    public async Task ASecondFieldTargetInTheSameGroup_RecordsNothingOnItsNavigationSnapshot()
    {
        // Nach dem zweiten Fokuswechsel derselben Feldgruppe sind Feld und Gruppe bereits erfasst.
        // Die folgende Navigationsmeldung ergänzt keinen Übergang und behält dieselbe Version.
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync(
            "test-app",
            "form-1",
            "de",
            EvaluationChannel.Voice,
            CancellationToken.None);

        telemetry.TrackFieldFocus(session, "field-a");
        telemetry.TrackStatusSnapshot(session, CreateStatusSnapshot("field-a", "group-a"));
        telemetry.TrackFieldFocus(session, "field-b");
        telemetry.TrackStatusSnapshot(session, CreateStatusSnapshot("field-b", "group-a"));
        await telemetry.EndSessionAsync(
            session,
            EvaluationOutcome.Submitted,
            new EvaluationTaskCompletion(1, 1, 0, ParticipantTaskTerminationClassification.Completed, TelemetryMismatchCodes.None),
            CancellationToken.None);

        var stateChanges = logger.Entries
            .Where(entry => entry.Values["event"] is TelemetryEventNames.FocusChange or TelemetryEventNames.NavigationChanged)
            .ToArray();
        Assert.Equal(4, stateChanges.Length);
        Assert.Equal([1, 1, 2, 2], stateChanges.Select(entry => Assert.IsType<int>(entry.Values["state_version"])).ToArray());
        Assert.Equal(TelemetryEventNames.NavigationChanged, stateChanges[3].Values["event"]);
        using var navigationData = JsonDocument.Parse(Assert.IsType<string>(stateChanges[3].Values["data"]));
        Assert.Equal("field-b", navigationData.RootElement.GetProperty("current_field_id").GetString());
        Assert.Empty(navigationData.RootElement.GetProperty("canonical_transitions").EnumerateArray());
        Assert.Equal(3, DeduplicatedStepCount(logger.Entries));
    }

    [Fact]
    public async Task ANavigationWithoutATarget_StaysRecordable_ProjectsNothing_AndStopsClaimingAMeasuredStepCount()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync(
            "test-app",
            "form-1",
            "de",
            EvaluationChannel.Voice,
            CancellationToken.None);

        // Der übergebene Status enthält weder ein aktuelles Feld noch eine aktuelle Feldgruppe.
        telemetry.TrackStatusSnapshot(session, new FormStatusSnapshot(
            Navigation: new FormStatusNavigationPosition(
                CurrentFormId: null,
                CurrentFormIndex: null,
                TotalForms: null,
                CurrentFieldGroupId: null!,
                CurrentFieldGroupIndex: 1,
                TotalFieldGroups: 1,
                CurrentFieldId: null),
            Progress: new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(0, 1),
                RequiredFieldsOnly: new FormStatusProgressDimension(0, 1),
                FieldGroupBased: new FormStatusProgressDimension(0, 1)),
            CurrentFieldLabel: null!,
            CurrentFieldIndex: 1,
            CurrentFieldGroupTitle: null!,
            OpenRequiredFieldCount: 1,
            ValidationErrorCount: 0).ToEvaluationStatusSnapshot());

        await telemetry.EndSessionAsync(
            session,
            EvaluationOutcome.Submitted,
            new EvaluationTaskCompletion(1, 1, 0, ParticipantTaskTerminationClassification.Completed, TelemetryMismatchCodes.None),
            CancellationToken.None);

        var navigation = logger.Entries.Single(entry => Equals(entry.Values["event"], TelemetryEventNames.NavigationChanged));
        using var navigationData = JsonDocument.Parse(Assert.IsType<string>(navigation.Values["data"]));

        // Fehlende Navigationsziele werden durch die vorgesehenen Platzhalter dargestellt.
        Assert.Equal("n/a", navigationData.RootElement.GetProperty("current_field_id").GetString());
        Assert.Equal("n/a", navigationData.RootElement.GetProperty("current_field_group_id").GetString());

        // Die Platzhalter selbst ergeben keinen Zustandsübergang.
        Assert.Empty(navigationData.RootElement.GetProperty("canonical_transitions").EnumerateArray());

        // Die Meldung bleibt als schrittfähiges Ereignis ohne projizierten Übergang erkennbar.
        // Aus ihrer leeren Liste wird kein Schritt gezählt.
        Assert.True(IsStepCapableWithoutProjection(navigation));
        Assert.Equal(0, DeduplicatedStepCount(logger.Entries));
    }

    [Fact]
    public async Task ARunInWhichEveryStepCapableLineProjects_LeavesNoUnprojectedStepCapableLine()
    {
        // Dieser Lauf enthält einen tatsächlichen Fokusübergang und keine fehlende Projektion.
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var session = await telemetry.StartSessionAsync(
            "test-app",
            "form-1",
            "de",
            EvaluationChannel.Voice,
            CancellationToken.None);

        telemetry.TrackFieldFocus(session, "field-1");
        await telemetry.EndSessionAsync(
            session,
            EvaluationOutcome.Submitted,
            new EvaluationTaskCompletion(1, 1, 0, ParticipantTaskTerminationClassification.Completed, TelemetryMismatchCodes.None),
            CancellationToken.None);

        Assert.DoesNotContain(logger.Entries, IsStepCapableWithoutProjection);
        Assert.Equal(1, DeduplicatedStepCount(logger.Entries));
    }

    internal static IEvaluationTelemetry CreateTelemetry(
        ILogger<EvaluationTelemetryImpl> logger,
        IStudyTelemetryContext? studyTelemetryContext = null,
        EvaluationTelemetryTaskOutbox? taskOutbox = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var context = studyTelemetryContext ?? new TestStudyTelemetryContext(null);
        var sessionStateStore = new EvaluationTelemetrySessionStateStore();
        var envelopeEmitter = new EvaluationTelemetryEnvelopeEmitter(
            context,
            taskOutbox ?? EvaluationTelemetryTestOutboxFactory.Create(context, logger), new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        var sessionPayloads = new EvaluationSessionTelemetryPayloads();
        var fieldPayloads = new EvaluationFieldTelemetryPayloads();
        var statusPayloads = new EvaluationStatusTelemetryPayloads();
        var commandPayloads = new EvaluationCommandTelemetryPayloads();
        var pacingPayloads = new EvaluationConversationalPacingTelemetryPayloads();
        var pacingTelemetry = new EvaluationConversationalPacingTelemetry(
            sessionStateStore,
            envelopeEmitter,
            pacingPayloads,
            context);
        var detailedStatusChangeDetector = new EvaluationTelemetryDetailedStatusChangeDetector(statusPayloads);

        return new EvaluationTelemetryImpl(
            new EvaluationSessionTelemetry(
                context,
                sessionStateStore,
                envelopeEmitter,
                sessionPayloads,
                pacingTelemetry),
            new EvaluationFormTelemetry(
                sessionStateStore,
                envelopeEmitter,
                fieldPayloads,
                statusPayloads,
                pacingTelemetry,
                detailedStatusChangeDetector),
            new EvaluationCommandTelemetry(
                envelopeEmitter,
                commandPayloads,
                pacingTelemetry));
    }

    private static async Task WaitForEntriesAsync(
        CapturingLogger<EvaluationTelemetryImpl> logger,
        int expectedCount)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (logger.Entries.Count < expectedCount)
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    internal static EvaluationFormStatusSnapshot CreateStatusSnapshot(string fieldId, string fieldGroupId)
        => new(
            new EvaluationFormNavigation(fieldId, fieldGroupId, 1, 3),
            new EvaluationFormProgress(
                new EvaluationFormProgressCounter(1, 3),
                new EvaluationFormProgressCounter(1, 2),
                new EvaluationFormProgressCounter(0, 1)),
            OpenRequiredFieldCount: 1,
            ValidationErrorCount: 0);

    private static void AssertJsonObjectPropertyNames(JsonElement element, params string[] expectedPropertyNames)
    {
        Assert.Equal(JsonValueKind.Object, element.ValueKind);

        var actualPropertyNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            actualPropertyNames.Add(property.Name);
        }

        // canonical_transitions wird bei jedem Nutzdatenobjekt erwartet. Die Aufrufer nennen
        // nur zusätzliche ereignisspezifische Felder; die Anzahlprüfung weist weitere Felder zurück.
        Assert.Contains(TelemetryPayloadProperties.CanonicalTransitions, actualPropertyNames);

        Assert.Equal(expectedPropertyNames.Length + 1, actualPropertyNames.Count);
        foreach (var expectedPropertyName in expectedPropertyNames)
        {
            Assert.Contains(expectedPropertyName, actualPropertyNames);
        }
    }

    // Die Schrittzahl wird aus unterschiedlichen Übergangsschlüsseln abgeleitet.
    // Der Schlüssel enthält Kennungen des Ereignisses, Zustandsversion und Übergangsmerkmale.
    private static int DeduplicatedStepCount(IReadOnlyList<CapturingLogEntry> entries)
    {
        var keys = new List<string>();
        foreach (var entry in entries)
        {
            using var document = JsonDocument.Parse(Assert.IsType<string>(entry.Values["data"]));
            foreach (var transition in document.RootElement.GetProperty("canonical_transitions").EnumerateArray())
            {
                keys.Add(TelemetryCanonicalTransition.BuildDeduplicationKey(
                    Assert.IsType<string>(entry.Values["participant_id"]),
                    Assert.IsType<string>(entry.Values["application_id"]),
                    Assert.IsType<string>(entry.Values["condition"]),
                    Assert.IsType<string>(entry.Values["task_id"]),
                    Assert.IsType<int>(entry.Values["state_version"]),
                    new TelemetryCanonicalTransition(
                        transition.GetProperty("canonical_class").GetString()!,
                        transition.GetProperty("scope").GetString()!,
                        transition.GetProperty("object_id").GetString()!,
                        transition.GetProperty("resulting_state").GetString()!)));
            }
        }

        return TelemetryCanonicalTransitionProjection.CountDeduplicated(keys);
    }

    private static bool IsStepCapableWithoutProjection(CapturingLogEntry entry)
    {
        using var document = JsonDocument.Parse(Assert.IsType<string>(entry.Values["data"]));
        var payload = document.RootElement.EnumerateObject().ToDictionary(
            static property => property.Name,
            static property => (object?)(property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => property.Value.GetInt32(),
                _ => null,
            }),
            StringComparer.Ordinal);
        return TelemetryCanonicalTransitionProjection.IsProjectionMissing(
            Assert.IsType<string>(entry.Values["event"]),
            payload);
    }

    private static void AssertFocusTransition(CapturingLogEntry entry, string expectedFieldId)
    {
        var data = Assert.IsType<string>(entry.Values["data"]);
        using var document = JsonDocument.Parse(data);
        var transition = Assert.Single(document.RootElement.GetProperty("canonical_transitions").EnumerateArray());
        Assert.Equal(TelemetryCanonicalClasses.FieldTargetChanged, transition.GetProperty("canonical_class").GetString());
        Assert.Equal(TelemetryTransitionScopes.Field, transition.GetProperty("scope").GetString());
        Assert.Equal(expectedFieldId, transition.GetProperty("object_id").GetString());
        Assert.Equal(TelemetryResultingStates.Targeted, transition.GetProperty("resulting_state").GetString());
    }

    private static Dictionary<string, object?> ManualStepPayload(
        params IReadOnlyDictionary<string, object?>[] transitions)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [TelemetryPayloadProperties.CanonicalTransitions] = transitions.Cast<object?>().ToArray(),
        };
    }
}
