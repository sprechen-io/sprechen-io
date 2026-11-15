using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;
using EvaluationTelemetryImpl = Sprechen.VoiceAssistant.EvaluationTelemetry.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Telemetry;

public sealed class OperationalProbeCaptureTests
{
    private const string ProbeId = "probe-0123456789abcdef0123456789abcdef";
    private const string ApplicationId = "study.umfrage";
    private const string TaskId = "hintergrund-und-setup";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BoundProbe_CompletesOrCancelsThroughExistingTerminalPath(bool cancelled)
    {
        var participant = new StudyTelemetryContext();
        var probe = BoundProbe();
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        using var outbox = CreateOutbox(participant, probe, store, transport);
        var emitter = new EvaluationTelemetryEnvelopeEmitter(participant, outbox, probe);
        var session = await emitter.StartTaskAsync(Session(), CancellationToken.None);
        var outcome = new TelemetryTaskEndOutcome(cancelled ? TelemetryTaskEndStates.Aborted : TelemetryTaskEndStates.ProcessSubmitted, cancelled ? TelemetryTaskTerminationClassifications.ParticipantCancelled : TelemetryTaskTerminationClassifications.Completed, TelemetryMismatchCodes.None, TelemetryCorrectnessBases.LogAndSystemCheck);
        var acceptance = await emitter.RequestTaskEndAsync(session, outcome, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(acceptance);
        await emitter.CommitTaskAsync(acceptance, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Null(participant.ParticipantId);
        Assert.Equal(2, transport.Bodies.Count);
        Assert.Empty(store.Checkpoints);
        Assert.All(transport.Bodies, body =>
        {
            using var envelope = JsonDocument.Parse(body);
            Assert.Equal(ProbeId, envelope.RootElement.GetProperty("participant_id").GetString());
            Assert.Equal("operational_probe", envelope.RootElement.GetProperty("record_provenance").GetString());
            Assert.Equal(ApplicationId, envelope.RootElement.GetProperty("application_id").GetString());
            Assert.Equal(TaskId, envelope.RootElement.GetProperty("task_id").GetString());
            Assert.False(envelope.RootElement.TryGetProperty("probe_id", out _));
        });
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("unbound")]
    [InlineData("foreign-application")]
    [InlineData("foreign-task")]
    [InlineData("wrong-channel")]
    [InlineData("mixed-participant")]
    public async Task UnadmittedTask_DoesNotCapture(string boundary)
    {
        var participant = new StudyTelemetryContext();
        var probe = boundary == "pending" ? new OperationalProbeSession() : BoundProbe();
        if (boundary == "pending") probe.Request();
        if (boundary == "unbound")
        {
            probe.Request();
            probe.AcceptContext(new OperationalProbeContext(ProbeId, "B"));
        }
        if (boundary == "mixed-participant") participant.SetStudyContext("xyz001");
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        using var outbox = CreateOutbox(participant, probe, store, transport);
        var emitter = new EvaluationTelemetryEnvelopeEmitter(participant, outbox, probe);
        var session = new EvaluationSession("run-1",
            boundary == "wrong-channel" ? EvaluationChannel.Html : EvaluationChannel.Voice,
            boundary == "foreign-application" ? "sample.complex" : ApplicationId,
            boundary == "foreign-task" ? "other-form" : TaskId);
        await emitter.StartTaskAsync(session, CancellationToken.None);
        Assert.Empty(store.Checkpoints);
        Assert.Empty(transport.Bodies);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OutsideTask_IsSuppressedBeforeAndAfterCatalogBinding(bool bound)
    {
        var participant = new StudyTelemetryContext();
        var probe = bound ? BoundProbe() : new OperationalProbeSession();
        if (!bound) probe.Request();
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        using var outbox = EvaluationTelemetryTestOutboxFactory.Create(participant,
            new CapturingLogger<EvaluationTelemetryImpl>(), store, transport,
            options: new EvaluationTelemetryOptions { RequireStudyContext = false }, operationalProbeSession: probe);
        var emitter = new EvaluationTelemetryEnvelopeEmitter(participant, outbox, probe);
        emitter.EmitOutsideSession(TelemetryEventNames.NoSpeechDetected, new Dictionary<string, object?>());
        Assert.Empty(store.Checkpoints);
        Assert.Empty(transport.Bodies);
    }

    [Theory]
    [InlineData("participant")]
    [InlineData("demonstration")]
    [InlineData("operational_probe")]
    public async Task NewProbe_LeavesForeignCheckpointUnchangedAndUnsent(string provenance)
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var priorProbe = provenance == "operational_probe" ? BoundProbe("probe-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa") : new OperationalProbeSession();
        var priorParticipant = new StudyTelemetryContext();
        priorParticipant.SetStudyContext(provenance == "participant" ? "xyz001" : null);
        using (var prior = EvaluationTelemetryTestOutboxFactory.Create(priorParticipant,
            new CapturingLogger<EvaluationTelemetryImpl>(), store, operationalProbeSession: priorProbe))
        {
            var emitter = new EvaluationTelemetryEnvelopeEmitter(priorParticipant, prior, priorProbe);
            await emitter.StartTaskAsync(Session(), CancellationToken.None);
        }
        var original = Assert.Single(store.Checkpoints);
        var participant = new StudyTelemetryContext();
        var probe = BoundProbe();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        using var outbox = CreateOutbox(participant, probe, store, transport);
        await new EvaluationTelemetryEnvelopeEmitter(participant, outbox, probe).StartTaskAsync(Session(), CancellationToken.None);
        Assert.Equal(original.Value, store.Read(original.Key));
        Assert.Single(transport.Bodies);
        Assert.Contains(ProbeId, transport.Bodies[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task NormalEntry_LeavesOrphanOperationalCheckpointUnchangedAndUnsent()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var probe = BoundProbe();
        var participant = new StudyTelemetryContext();
        using (var prior = CreateOutbox(participant, probe, store, new RecordingEvaluationTelemetryDeliveryTransport()))
        {
            await new EvaluationTelemetryEnvelopeEmitter(participant, prior, probe).StartTaskAsync(Session(), CancellationToken.None);
        }
        var original = Assert.Single(store.Checkpoints);
        probe.Reset();
        participant.SetStudyContext("xyz001");
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        using var outbox = CreateOutbox(participant, probe, store, transport);
        await new EvaluationTelemetryEnvelopeEmitter(participant, outbox, probe).StartTaskAsync(Session(), CancellationToken.None);
        Assert.Equal(original.Value, store.Read(original.Key));
        Assert.Single(transport.Bodies);
        Assert.DoesNotContain("operational_probe", transport.Bodies[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task TransientRetry_PreservesOriginalBodyAndIdentity()
    {
        var probe = BoundProbe();
        var participant = new StudyTelemetryContext();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        transport.Enqueue(new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.TransientFailure, 503));
        using var outbox = CreateOutbox(participant, probe, new InMemoryEvaluationTelemetryOutboxStore(), transport);
        await new EvaluationTelemetryEnvelopeEmitter(participant, outbox, probe).StartTaskAsync(Session(), CancellationToken.None);
        Assert.Equal(2, transport.Bodies.Count);
        Assert.Equal(transport.Bodies[0], transport.Bodies[1]);
    }

    [Fact]
    public async Task PermanentRejection_FailsTerminalWithoutPublicFallback()
    {
        var probe = BoundProbe();
        var participant = new StudyTelemetryContext();
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        transport.Enqueue(new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.PermanentFailure, 403));
        using var outbox = CreateOutbox(participant, probe, store, transport);
        var emitter = new EvaluationTelemetryEnvelopeEmitter(participant, outbox, probe);
        var session = await emitter.StartTaskAsync(Session(), CancellationToken.None);
        await Assert.ThrowsAsync<EvaluationTelemetryDeliveryRejectedException>(() => emitter.RequestTaskEndAsync(session,
            new TelemetryTaskEndOutcome(TelemetryTaskEndStates.NotSubmitted, TelemetryTaskTerminationClassifications.TechnicalFailure, TelemetryMismatchCodes.None, TelemetryCorrectnessBases.LogAndSystemCheck), CancellationToken.None));
        Assert.Single(transport.Bodies);
        Assert.Contains("faulted", Assert.Single(store.Checkpoints.Values), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RevokedOrMixedIdentity_StopsPendingRetry(bool participantDrift)
    {
        var probe = BoundProbe();
        var participant = new StudyTelemetryContext();
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        transport.Enqueue(_ =>
        {
            if (participantDrift) participant.SetStudyContext("xyz001");
            else probe.Reset();
            return new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.TransientFailure, 503);
        });
        using var outbox = CreateOutbox(participant, probe, store, transport);
        await new EvaluationTelemetryEnvelopeEmitter(participant, outbox, probe).StartTaskAsync(Session(), CancellationToken.None);
        Assert.Single(transport.Bodies);
        Assert.Single(store.Checkpoints);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrphanOutsideTaskProbe_IsLeftUntouchedWithoutStartingItsPump(bool operational)
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var key = EvaluationTelemetryOutboxKeys.BuildOutsideTaskStorageKey("old-event");
        const string original = "{\"identity\":{\"record_provenance\":\"operational_probe\"}}";
        store.Write(key, original);
        var participant = new StudyTelemetryContext();
        if (!operational) participant.SetStudyContext("xyz001");
        var probe = operational ? BoundProbe() : new OperationalProbeSession();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        using var outbox = CreateOutbox(participant, probe, store, transport);
        await new EvaluationTelemetryEnvelopeEmitter(participant, outbox, probe).StartTaskAsync(Session(), CancellationToken.None);
        Assert.Equal(original, store.Read(key));
        Assert.Single(transport.Bodies);
    }

    [Fact]
    public void Binding_CannotChangeTaskOrAdoptAResponseFromAFormerLifecycle()
    {
        var probe = BoundProbe();
        var oldContext = probe.Context!;
        Assert.Throws<ArgumentNullException>(() => probe.BindStudy(null!, ApplicationId, TaskId));
        Assert.Throws<InvalidOperationException>(() => probe.BindStudy(oldContext, ApplicationId, "other-task"));
        probe.Request();
        probe.AcceptContext(new OperationalProbeContext("probe-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "B"));
        Assert.Throws<InvalidOperationException>(() => probe.BindStudy(oldContext, ApplicationId, TaskId));
        probe.Reset();
        Assert.False(probe.Matches(ProbeId, "B", ApplicationId, TaskId));
    }

    private static OperationalProbeSession BoundProbe(string probeId = ProbeId)
    {
        var probe = new OperationalProbeSession();
        probe.Request();
        var context = new OperationalProbeContext(probeId, "B");
        probe.AcceptContext(context);
        probe.BindStudy(context, ApplicationId, TaskId);
        return probe;
    }

    private static EvaluationSession Session() => new("run-1", EvaluationChannel.Voice, ApplicationId, TaskId);

    private static EvaluationTelemetryTaskOutbox CreateOutbox(StudyTelemetryContext participant,
        OperationalProbeSession probe, InMemoryEvaluationTelemetryOutboxStore store, RecordingEvaluationTelemetryDeliveryTransport transport)
        => EvaluationTelemetryTestOutboxFactory.Create(participant, new CapturingLogger<EvaluationTelemetryImpl>(),
            store, transport, options: new EvaluationTelemetryOptions(), operationalProbeSession: probe);
}
