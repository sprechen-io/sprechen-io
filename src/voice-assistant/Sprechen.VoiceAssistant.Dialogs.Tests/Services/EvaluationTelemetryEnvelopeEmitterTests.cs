using Sprechen.Mda.Contracts.V1.Telemetry;

using EvaluationTelemetryImpl = Sprechen.VoiceAssistant.EvaluationTelemetry.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Services;

public sealed class EvaluationTelemetryEnvelopeEmitterTests
{
    [Fact]
    public async Task Emit_WithoutStateAdoptionKeepsStateVersionAndUsesFallbackEnvelopeValues()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var clock = new MutableClock(new DateTimeOffset(2026, 1, 24, 12, 34, 56, TimeSpan.Zero));
        var context = new StudyTelemetryContext();
        var emitter = new EvaluationTelemetryEnvelopeEmitter(
            context,
            EvaluationTelemetryTestOutboxFactory.Create(context, logger), new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        var session = new EvaluationSession("session-1", EvaluationChannel.Html, "test-app", "form-1");

        session = await emitter.StartTaskAsync(session, cancellationToken: CancellationToken.None);
        emitter.Emit(
            session,
            TelemetryEventNames.IntentRecognized,
            new Dictionary<string, object?> { [TelemetryPayloadProperties.Intent] = "help" });

        Assert.Equal(2, logger.Entries.Count);
        Assert.Equal("P000", logger.Entries[0].Values["participant_id"]);
        Assert.Equal("test-app", logger.Entries[0].Values["application_id"]);
        Assert.Equal("form-1", logger.Entries[0].Values["task_id"]);
        Assert.Equal("A", logger.Entries[0].Values["condition"]);
        // Beide Ereignisse übernehmen keinen neuen Formularzustand; die Zustandsversion bleibt unverändert.
        Assert.Equal(1, logger.Entries[0].Values["state_version"]);
        Assert.Equal(1, logger.Entries[1].Values["state_version"]);
    }

    [Fact]
    public void EmitOutsideSession_UsesModeledPlaceholderEnvelopeContext()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var emitter = CreateEmitter(logger);

        emitter.EmitOutsideSession(TelemetryEventNames.WakeWordMiss, WakeWordMissPayload());

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(TelemetryEnvelopeContextDefaults.UnknownParticipantId, entry.Values["participant_id"]);
        Assert.Equal(TelemetryEnvelopeContextDefaults.UnknownApplicationId, entry.Values["application_id"]);
        Assert.Equal(TelemetryEnvelopeContextDefaults.UnknownTaskId, entry.Values["task_id"]);
        Assert.Equal("B", entry.Values["condition"]);
        Assert.Equal(TelemetryEnvelopeContextDefaults.OutsideTaskStateVersion, entry.Values["state_version"]);
    }

    [Fact]
    public void EmitOutsideSession_RepeatedEmissions_KeepTheOutsideTaskStateVersion()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var emitter = CreateEmitter(logger);

        emitter.EmitOutsideSession(TelemetryEventNames.WakeWordMiss, WakeWordMissPayload());
        emitter.EmitOutsideSession(TelemetryEventNames.WakeWordMiss, WakeWordMissPayload());

        Assert.All(
            logger.Entries,
            entry => Assert.Equal(TelemetryEnvelopeContextDefaults.OutsideTaskStateVersion, entry.Values["state_version"]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmitOutsideSession_RejectsBlankEventName(string eventName)
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var emitter = CreateEmitter(logger);

        Assert.Throws<ArgumentException>(() => emitter.EmitOutsideSession(eventName, data: null));
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void EmitOutsideSession_WhenAStudyContextIsPresent_UsesItsParticipantAndTask()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var studyContext = new StudyTelemetryContext();
        studyContext.SetStudyContext("p942demo");
        var clock = new MutableClock(new DateTimeOffset(2026, 1, 24, 12, 34, 56, TimeSpan.Zero));
        var emitter = new EvaluationTelemetryEnvelopeEmitter(
            studyContext,
            EvaluationTelemetryTestOutboxFactory.Create(studyContext, logger), new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());

        emitter.EmitOutsideSession(TelemetryEventNames.WakeWordMiss, WakeWordMissPayload());

        var entry = Assert.Single(logger.Entries);
        Assert.Equal("p942demo", entry.Values["participant_id"]);
        Assert.Equal(TelemetryEnvelopeContextDefaults.UnknownTaskId, entry.Values["task_id"]);
        // Die Anwendung ist noch nicht gebunden; deshalb bleibt ihre Platzhalterkennung erhalten.
        Assert.Equal(TelemetryEnvelopeContextDefaults.UnknownApplicationId, entry.Values["application_id"]);
        Assert.Equal(TelemetryRecordProvenances.Participant, entry.Values["record_provenance"]);
    }

    [Fact]
    public void EmitOutsideSession_WhenParticipantTokenIsBound_KeepsTheTokenAndUsesPlaceholderTask()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var studyContext = new StudyTelemetryContext();
        studyContext.SetStudyContext("x9jdln");
        var clock = new MutableClock(new DateTimeOffset(2026, 1, 24, 12, 34, 56, TimeSpan.Zero));
        var emitter = new EvaluationTelemetryEnvelopeEmitter(
            studyContext,
            EvaluationTelemetryTestOutboxFactory.Create(studyContext, logger), new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());

        emitter.EmitOutsideSession(TelemetryEventNames.NoSpeechDetected, new Dictionary<string, object?>());

        var entry = Assert.Single(logger.Entries);
        Assert.Equal("x9jdln", entry.Values["participant_id"]);
        Assert.Equal(TelemetryEnvelopeContextDefaults.UnknownTaskId, entry.Values["task_id"]);
        Assert.Equal(TelemetryRecordProvenances.Participant, entry.Values["record_provenance"]);
        Assert.Equal("B", entry.Values["condition"]);
    }

    private static EvaluationTelemetryEnvelopeEmitter CreateEmitter(CapturingLogger<EvaluationTelemetryImpl> logger)
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 1, 24, 12, 34, 56, TimeSpan.Zero));
        var context = new StudyTelemetryContext();
        return new EvaluationTelemetryEnvelopeEmitter(
            context,
            EvaluationTelemetryTestOutboxFactory.Create(context, logger), new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
    }

    private static object WakeWordMissPayload()
        => new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.Phase] = TelemetryWakeWordMissPhases.MissTrace,
            [TelemetryPayloadProperties.EventCode] = TelemetryWakeWordMissEventCodes.WakeWordMissTrace,
            [TelemetryPayloadProperties.Status] = TelemetryWakeWordActivationStatuses.NoRecognizedSpeech,
        };
}
