using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

using EvaluationTelemetryImpl = Sprechen.VoiceAssistant.EvaluationTelemetry.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Telemetry;

public sealed class EvaluationTelemetryTaskOutboxTests
{
    [Theory]
    [InlineData("active")]
    [InlineData("ending")]
    [InlineData("accepted")]
    [InlineData("committed")]
    [InlineData("faulted")]
    public async Task CanonicalTaskIdentity_DoesNotAdoptOrRewriteHistoricalCheckpoint(string phase)
    {
        var oldIdentity = Identity() with { TaskId = "test-app/form-1" };
        var currentIdentity = oldIdentity with { TaskId = "form-1" };
        var store = await CreateHistoricalCheckpointAsync(phase, oldIdentity);
        var oldKey = Assert.Single(store.Checkpoints.Keys);
        var before = JsonNode.Parse(store.Checkpoints[oldKey])!.AsObject();
        Assert.Equal(phase, before["phase"]!.GetValue<string>());
        var pendingBodies = before["pending_events"]!.AsArray()
            .Select(item => item!["body"]!.GetValue<string>()).ToArray();
        var acknowledgedBodies = before["acknowledged_events"]!.AsArray()
            .Select(item => item!["body"]!.GetValue<string>()).ToArray();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        using var restored = CreateOutbox(store, transport, exclusiveLease: new ExclusiveEvaluationTelemetryLease());

        var current = await StartTaskAsync(
            restored, Session() with { SessionId = "current-run" }, currentIdentity);

        Assert.Equal("current-run", current.SessionId);
        Assert.Null(current.RestoredTerminal);
        if (phase is "active" or "ending")
        {
            await WaitUntilAsync(() => JsonNode.Parse(store.Checkpoints[oldKey])!["phase"]!.GetValue<string>() == "accepted");
            Assert.NotEmpty(pendingBodies);
            Assert.All(pendingBodies, body => Assert.Contains(body, transport.Bodies));
        }
        else
        {
            Assert.DoesNotContain(transport.Bodies,
                body => ReadString(body, TelemetryEnvelopeFieldNames.TaskId) == oldIdentity.TaskId);
        }

        var after = JsonNode.Parse(store.Checkpoints[oldKey])!.AsObject();
        var retainedBodies = after["pending_events"]!.AsArray()
            .Concat(after["acknowledged_events"]!.AsArray())
            .Select(item => item!["body"]!.GetValue<string>()).ToArray();
        Assert.All(pendingBodies.Concat(acknowledgedBodies), body => Assert.Contains(body, retainedBodies));
        Assert.All(acknowledgedBodies, body => Assert.DoesNotContain(body, transport.Bodies));
        Assert.All(retainedBodies,
            body => Assert.Equal(oldIdentity.TaskId, ReadString(body, TelemetryEnvelopeFieldNames.TaskId)));
        if (phase == "active")
        {
            var terminal = transport.Bodies.Single(body =>
                ReadString(body, TelemetryEnvelopeFieldNames.TaskId) == oldIdentity.TaskId &&
                ReadString(body, TelemetryEnvelopeFieldNames.Event) == TelemetryEventNames.TaskEnd);
            using var terminalDocument = JsonDocument.Parse(terminal);
            Assert.Equal(TelemetryTaskEndStates.NotSubmitted,
                terminalDocument.RootElement.GetProperty(TelemetryEnvelopeFieldNames.Data)
                    .GetProperty(TelemetryPayloadProperties.EndState).GetString());
            Assert.Equal(TelemetryTaskTerminationClassifications.SessionAbandoned,
                after["terminal_intent"]!["termination_classification"]!.GetValue<string>());
        }

        var acceptance = Assert.IsType<EvaluationSessionAcceptance>(await restored.RequestTaskEndAsync(
            current, currentIdentity, SuccessfulOutcome(), CancellationToken.None));
        await restored.CommitTaskAsync(acceptance, CancellationToken.None);

        Assert.Equal(oldKey, Assert.Single(store.Checkpoints.Keys));
        Assert.Equal(after.ToJsonString(), JsonNode.Parse(store.Checkpoints[oldKey])!.ToJsonString());
        Assert.Single(transport.Bodies, body =>
            ReadString(body, TelemetryEnvelopeFieldNames.TaskId) == currentIdentity.TaskId &&
            ReadString(body, TelemetryEnvelopeFieldNames.Event) == TelemetryEventNames.TaskStart);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanonicalTaskIdentity_ExcludesAnotherCurrentTabIndependentlyOfHistoricalLease(bool oldTabOpen)
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var lease = new ExclusiveEvaluationTelemetryLease();
        var oldIdentity = Identity() with { TaskId = "test-app/form-1" };
        var currentIdentity = oldIdentity with { TaskId = "form-1" };
        using var old = CreateOutbox(store, new RecordingEvaluationTelemetryDeliveryTransport(), exclusiveLease: lease);
        if (oldTabOpen)
        {
            await StartTaskAsync(old, Session() with { FormId = oldIdentity.TaskId }, oldIdentity);
        }
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        using var current = CreateOutbox(store, transport, exclusiveLease: lease);
        var contenderTransport = new RecordingEvaluationTelemetryDeliveryTransport();
        using var contender = CreateOutbox(store, contenderTransport, exclusiveLease: lease);

        var started = await StartTaskAsync(current, Session() with { SessionId = "current-run" }, currentIdentity);
        await Assert.ThrowsAsync<EvaluationSessionAlreadyActiveException>(() =>
            StartTaskAsync(contender, Session() with { SessionId = "contender-run" }, currentIdentity));

        Assert.Null(started.RestoredTerminal);
        Assert.Equal(currentIdentity.TaskId, ReadString(Assert.Single(transport.Bodies), TelemetryEnvelopeFieldNames.TaskId));
        Assert.Empty(contenderTransport.Bodies);
        Assert.Equal(oldTabOpen ? 2 : 1, store.Checkpoints.Count);
        Assert.Equal(oldTabOpen ? 2 : 1, lease.HeldCount);
    }

    [Fact]
    public async Task CanonicalTaskIdentity_CancelledNewStartCannotPersistOrSend()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var lease = new ExclusiveEvaluationTelemetryLease();
        using var outbox = CreateOutbox(store, transport, exclusiveLease: lease);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var identity = Identity() with { TaskId = "form-1" };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => outbox.StartTaskAsync(
            Session(), identity, EmptyPayload(), cancellation.Token));

        Assert.Empty(store.Checkpoints);
        Assert.Empty(transport.Bodies);
        Assert.Equal(0, lease.HeldCount);
    }


    private static async Task<InMemoryEvaluationTelemetryOutboxStore> CreateHistoricalCheckpointAsync(
        string phase, EvaluationTelemetryEnvelopeIdentity identity)
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var retry = new BlockingEvaluationTelemetryRetryDelay();
        var outbox = CreateOutbox(store, transport, retryDelay: retry);
        var session = await StartTaskAsync(outbox, Session() with { FormId = identity.TaskId }, identity);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 1);
        if (phase == "active")
        {
            transport.Enqueue(new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.TransientFailure, 503));
            var attachment = EvaluationTelemetryTransitionAttacher.Attach(TelemetryEventNames.FocusChange,
                new Dictionary<string, object?> { [TelemetryPayloadProperties.FieldId] = "field-1" });
            outbox.EnqueueTaskEvent(session, identity, TelemetryEventNames.FocusChange,
                attachment.Payload, attachment.Transitions);
            await retry.WaitObserved.WaitAsync(TimeSpan.FromSeconds(5));
            outbox.Dispose();
            return store;
        }
        if (phase is "ending" or "faulted")
        {
            transport.Enqueue(new EvaluationTelemetryDeliveryResult(
                phase == "ending" ? EvaluationTelemetryDeliveryStatus.TransientFailure : EvaluationTelemetryDeliveryStatus.PermanentFailure,
                phase == "ending" ? 503 : 422));
        }
        var end = outbox.RequestTaskEndAsync(session, identity, SuccessfulOutcome(), CancellationToken.None);
        if (phase == "ending")
        {
            await retry.WaitObserved.WaitAsync(TimeSpan.FromSeconds(5));
            outbox.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => end);
            return store;
        }
        if (phase == "faulted")
        {
            await Assert.ThrowsAsync<EvaluationTelemetryDeliveryRejectedException>(() => end);
            outbox.Dispose();
            return store;
        }
        var acceptance = Assert.IsType<EvaluationSessionAcceptance>(await end);
        if (phase == "committed")
        {
            store.DeleteFailuresRemaining = 1;
            var commit = outbox.CommitTaskAsync(acceptance, CancellationToken.None);
            await retry.WaitObserved.WaitAsync(TimeSpan.FromSeconds(5));
            outbox.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => commit);
        }
        else
        {
            outbox.Dispose();
        }
        return store;
    }


    [Fact]
    public async Task StartTaskAsync_PersistsTaskStartOnlyAfterOwningTheFullIdentityLease()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var lease = new RecordingEvaluationTelemetryExclusiveLease();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        transport.Enqueue(body =>
        {
            var eventId = ReadString(body, TelemetryEnvelopeFieldNames.EventId);
            Assert.Equal(
                TelemetryRecordProvenances.Participant,
                ReadString(body, TelemetryEnvelopeFieldNames.RecordProvenance));
            Assert.Contains(store.Checkpoints.Values, checkpoint => checkpoint.Contains(eventId, StringComparison.Ordinal));
            Assert.Equal(EvaluationTelemetryOutboxKeys.BuildIdentityLeaseKey(Identity()), lease.HeldLeaseKeys.Single());
            return Accepted();
        });
        var outbox = CreateOutbox(store, transport, exclusiveLease: lease);

        var session = await StartTaskAsync(outbox);

        Assert.Equal(Session().SessionId, session.SessionId);
        Assert.Single(transport.Bodies);
        Assert.Contains("sprechen.evaluation.outbox.v1:task:", Assert.Single(store.Checkpoints.Keys), StringComparison.Ordinal);
        Assert.DoesNotContain(session.SessionId, Assert.Single(lease.HeldLeaseKeys), StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartTaskAsync_DemonstrationV0UsesItsOwnDurableIdentity()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var outbox = CreateOutbox(store, transport);
        var identity = new EvaluationTelemetryEnvelopeIdentity(
            TelemetryEnvelopeContextDefaults.UnknownParticipantId,
            "B",
            "test-app",
            TelemetryEnvelopeContextDefaults.UnknownTaskId,
            TelemetryRecordProvenances.Demonstration);

        _ = await outbox.StartTaskAsync(
            Session(),
            identity,
            EmptyPayload(),
            CancellationToken.None);

        var body = Assert.Single(transport.Bodies);
        Assert.Equal(
            TelemetryRecordProvenances.Demonstration,
            ReadString(body, TelemetryEnvelopeFieldNames.RecordProvenance));
        Assert.Contains(":demonstration:", Assert.Single(store.Checkpoints.Keys), StringComparison.Ordinal);
    }



    [Fact]
    public async Task TransientTaskEndFailure_RetriesByteIdenticalBodyAndCompletesOnlyAfter204()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        transport.Enqueue(Accepted());
        transport.Enqueue(new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.TransientFailure, 503));
        transport.Enqueue(Accepted());
        var outbox = CreateOutbox(store, transport);
        var session = await StartTaskAsync(outbox);

        var acceptance = Assert.IsType<EvaluationSessionAcceptance>(
            await outbox.RequestTaskEndAsync(session, Identity(), SuccessfulOutcome(), CancellationToken.None));

        Assert.Equal(3, transport.Bodies.Count);
        Assert.Equal(transport.Bodies[1], transport.Bodies[2]);
        Assert.Equal(TelemetryEventNames.TaskEnd, ReadString(transport.Bodies[1], TelemetryEnvelopeFieldNames.Event));
        var acceptedCheckpoint = Assert.Single(store.Checkpoints.Values);
        Assert.Contains("\"phase\":\"accepted\"", acceptedCheckpoint, StringComparison.Ordinal);
        Assert.Contains(acceptance.EventId, acceptedCheckpoint, StringComparison.Ordinal);
        Assert.Contains(acceptance.BodySha256, acceptedCheckpoint, StringComparison.Ordinal);

        await outbox.CommitTaskAsync(acceptance, CancellationToken.None);
        await WaitUntilAsync(() => store.Checkpoints.Count == 0);
    }

    [Fact]
    public async Task TaskEnd_IsDeferredUntilPredecessorsAreAcknowledged_AndCountsAcknowledgedKeys()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new BlockingPredecessorEvaluationTelemetryDeliveryTransport();
        var outbox = CreateOutbox(store, transport);
        var session = await StartTaskAsync(outbox);
        var attachment = EvaluationTelemetryTransitionAttacher.Attach(
            TelemetryEventNames.FocusChange,
            new Dictionary<string, object?>
            {
                [TelemetryPayloadProperties.FieldId] = "field-1",
            });

        outbox.EnqueueTaskEvent(
            session,
            Identity(),
            TelemetryEventNames.FocusChange,
            attachment.Payload,
            attachment.Transitions);
        await transport.PredecessorObserved;
        var endTask = outbox.RequestTaskEndAsync(
            session,
            Identity(),
            SuccessfulOutcome(),
            CancellationToken.None);

        Assert.False(endTask.IsCompleted);
        Assert.DoesNotContain(
            transport.Bodies,
            body => ReadString(body, TelemetryEnvelopeFieldNames.Event) == TelemetryEventNames.TaskEnd);

        transport.AcceptPredecessor();
        _ = Assert.IsType<EvaluationSessionAcceptance>(await endTask);

        // Der Fokusübergang steht im zugehörigen Ereignis; task_end trägt keinen Schrittzähler.
        var focusBody = transport.Bodies.Single(
            body => ReadString(body, TelemetryEnvelopeFieldNames.Event) == TelemetryEventNames.FocusChange);
        using var focusDocument = JsonDocument.Parse(focusBody);
        Assert.Single(
            focusDocument.RootElement
                .GetProperty(TelemetryEnvelopeFieldNames.Data)
                .GetProperty(TelemetryPayloadProperties.CanonicalTransitions)
                .EnumerateArray());
    }

    [Fact]
    public async Task SameIdentityInTwoTabs_LoserCannotPersistOrSendTaskStart()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var lease = new ExclusiveEvaluationTelemetryLease();
        var firstTransport = new RecordingEvaluationTelemetryDeliveryTransport();
        var first = CreateOutbox(store, firstTransport, exclusiveLease: lease);
        var secondTransport = new RecordingEvaluationTelemetryDeliveryTransport();
        var second = CreateOutbox(store, secondTransport, exclusiveLease: lease);

        _ = await StartTaskAsync(first);
        await Assert.ThrowsAsync<EvaluationSessionAlreadyActiveException>(() => StartTaskAsync(second));

        Assert.Single(firstTransport.Bodies);
        Assert.Empty(secondTransport.Bodies);
        Assert.Single(store.Checkpoints);
    }

    [Fact]
    public async Task TwoRestoredTabs_UseOneBrowserWideOwnerAndDoNotResurrectTheCheckpoint()
    {
        var store = await CreatePersistedActiveCheckpointAsync();
        var lease = new ExclusiveEvaluationTelemetryLease();
        var firstTransport = new RecordingEvaluationTelemetryDeliveryTransport();
        var first = CreateOutbox(store, firstTransport, exclusiveLease: lease);
        var secondTransport = new RecordingEvaluationTelemetryDeliveryTransport();
        var second = CreateOutbox(store, secondTransport, exclusiveLease: lease);

        var restored = await StartTaskAsync(first, Session() with { SessionId = "first-proposal" });
        await Assert.ThrowsAsync<EvaluationSessionAlreadyActiveException>(
            () => StartTaskAsync(second, Session() with { SessionId = "second-proposal" }));

        Assert.Equal(Session().SessionId, restored.SessionId);
        Assert.Equal(
            ParticipantTaskTerminationClassification.SessionAbandoned,
            Assert.IsType<EvaluationSessionRestoredTerminal>(restored.RestoredTerminal).TerminationClassification);
        Assert.Empty(secondTransport.Bodies);
        Assert.Single(store.Checkpoints);

        var acceptance = await restored.RestoredTerminal.Acceptance;
        await first.CommitTaskAsync(acceptance, CancellationToken.None);
        await WaitUntilAsync(() => store.Checkpoints.Count == 0);
        await WaitUntilAsync(() => lease.HeldCount == 0);

        var fresh = await StartTaskAsync(second, Session() with { SessionId = "fresh-run" });

        Assert.Equal("fresh-run", fresh.SessionId);
        Assert.Single(secondTransport.Bodies);
        Assert.Equal(TelemetryEventNames.TaskEnd, ReadString(Assert.Single(firstTransport.Bodies), TelemetryEnvelopeFieldNames.Event));
        Assert.Single(store.Checkpoints);
    }

    [Fact]
    public async Task TabInitializedBehindRestoredOwner_CanAdoptTerminalAfterThatOwnerCloses()
    {
        var store = await CreatePersistedActiveCheckpointAsync();
        var lease = new ExclusiveEvaluationTelemetryLease();
        var first = CreateOutbox(
            store,
            new RecordingEvaluationTelemetryDeliveryTransport(),
            exclusiveLease: lease);
        var secondTransport = new RecordingEvaluationTelemetryDeliveryTransport();
        var second = CreateOutbox(store, secondTransport, exclusiveLease: lease);

        var firstRestored = await StartTaskAsync(first, Session() with { SessionId = "first-proposal" });
        await Assert.ThrowsAsync<EvaluationSessionAlreadyActiveException>(
            () => StartTaskAsync(second, Session() with { SessionId = "blocked-proposal" }));

        first.Dispose();
        await WaitUntilAsync(() => lease.HeldCount == 0);

        var adopted = await StartTaskAsync(second, Session() with { SessionId = "second-proposal" });

        Assert.Equal(firstRestored.SessionId, adopted.SessionId);
        Assert.Equal(
            ParticipantTaskTerminationClassification.SessionAbandoned,
            Assert.IsType<EvaluationSessionRestoredTerminal>(adopted.RestoredTerminal).TerminationClassification);
        var acceptance = await adopted.RestoredTerminal.Acceptance;
        await second.CommitTaskAsync(acceptance, CancellationToken.None);
        await WaitUntilAsync(() => store.Checkpoints.Count == 0);
        Assert.Empty(secondTransport.Bodies);
    }

    [Fact]
    public async Task SameProcessSecondStartCannotResumeAnActiveDialog()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var outbox = CreateOutbox(store, transport);
        _ = await StartTaskAsync(outbox);
        var proposed = Session() with { SessionId = "second-run" };

        await Assert.ThrowsAsync<EvaluationSessionAlreadyActiveException>(() => StartTaskAsync(outbox, proposed));
        Assert.Single(transport.Bodies);
    }

    [Fact]
    public async Task IdentityLease_RemainsHeldThroughTaskEnd204UntilApplicationCommitAndClear()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var lease = new ExclusiveEvaluationTelemetryLease();
        var terminalTransport = new BlockingTerminalEvaluationTelemetryDeliveryTransport();
        var first = CreateOutbox(store, terminalTransport, exclusiveLease: lease);
        var firstSession = await StartTaskAsync(first);
        var endTask = first.RequestTaskEndAsync(firstSession, Identity(), SuccessfulOutcome(), CancellationToken.None);
        await terminalTransport.TerminalObserved;

        var contenderTransport = new RecordingEvaluationTelemetryDeliveryTransport();
        var contender = CreateOutbox(store, contenderTransport, exclusiveLease: lease);
        await Assert.ThrowsAsync<EvaluationSessionAlreadyActiveException>(() => StartTaskAsync(contender));
        Assert.False(endTask.IsCompleted);

        terminalTransport.AcceptTerminal();
        var acceptance = Assert.IsType<EvaluationSessionAcceptance>(await endTask);
        await Assert.ThrowsAsync<EvaluationSessionAlreadyActiveException>(() => StartTaskAsync(contender));

        await first.CommitTaskAsync(acceptance, CancellationToken.None);
        await WaitUntilAsync(() => store.Checkpoints.Count == 0);
        await WaitUntilAsync(() => lease.HeldCount == 0);
        var nextSession = await StartTaskAsync(contender);

        Assert.Equal(Session().SessionId, nextSession.SessionId);
        Assert.Single(contenderTransport.Bodies);
    }

    [Fact]
    public async Task ActiveRestore_ClosesAsSessionAbandonedBeforeASeparateRunCanStart()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var initial = CreateOutbox(store, new RecordingEvaluationTelemetryDeliveryTransport());
        var initialSession = await StartTaskAsync(initial);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 1);
        initial.Dispose();

        var restoredTransport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, restoredTransport);
        var proposed = Session() with { SessionId = "new-run" };
        var restoredSession = await StartTaskAsync(restored, proposed);
        await Assert.ThrowsAsync<EvaluationSessionAlreadyActiveException>(() => StartTaskAsync(restored, proposed));

        Assert.Equal(initialSession.SessionId, restoredSession.SessionId);
        Assert.Equal(
            ParticipantTaskTerminationClassification.SessionAbandoned,
            Assert.IsType<EvaluationSessionRestoredTerminal>(restoredSession.RestoredTerminal).TerminationClassification);
        var terminalBody = Assert.Single(restoredTransport.Bodies);
        using (var document = JsonDocument.Parse(terminalBody))
        {
            var data = document.RootElement.GetProperty(TelemetryEnvelopeFieldNames.Data);
            Assert.Equal(TelemetryEventNames.TaskEnd, document.RootElement.GetProperty(TelemetryEnvelopeFieldNames.Event).GetString());
            Assert.Equal(TelemetryTaskEndStates.NotSubmitted, data.GetProperty(TelemetryPayloadProperties.EndState).GetString());
        }

        var acceptance = await restoredSession.RestoredTerminal.Acceptance;
        await restored.CommitTaskAsync(acceptance, CancellationToken.None);
        await WaitUntilAsync(() => store.Checkpoints.Count == 0);
        var fresh = await StartTaskAsync(restored, proposed);
        Assert.Equal("new-run", fresh.SessionId);
        Assert.Null(fresh.RestoredTerminal);
    }

    [Fact]
    public async Task CorruptRestoredBodyIdentity_FailsClosedWithoutFetching()
    {
        var store = await CreatePersistedActiveCheckpointAsync();
        MutateStoredBody(store, "acknowledged_events", 0, envelope =>
        {
            envelope[TelemetryEnvelopeFieldNames.ParticipantId] = "P999";
        });
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(transport.Bodies);
    }

    [Fact]
    public async Task CorruptRestoredBodyProvenance_FailsClosedWithoutFetching()
    {
        var store = await CreatePersistedActiveCheckpointAsync();
        MutateStoredBody(store, "acknowledged_events", 0, envelope =>
        {
            envelope[TelemetryEnvelopeFieldNames.RecordProvenance] =
                TelemetryRecordProvenances.OperationalProbe;
        });
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(transport.Bodies);
    }

    [Fact]
    public async Task ProbeCheckpointWithoutCurrentBinding_IsNotRestoredOrDelivered()
    {
        // Ohne passende Sitzungsbindung darf der gespeicherte Prüflauf weder übernommen noch gesendet werden.
        var store = await CreatePersistedActiveCheckpointAsync();
        var key = Assert.Single(store.Checkpoints.Keys);
        var checkpoint = JsonNode.Parse(store.Checkpoints[key])!.AsObject();
        checkpoint["identity"]![TelemetryEnvelopeFieldNames.ParticipantId] =
            "probe-ffffffffffffffffffffffffffffffff";
        checkpoint["identity"]![TelemetryEnvelopeFieldNames.RecordProvenance] =
            TelemetryRecordProvenances.OperationalProbe;
        var probeIdentity = new EvaluationTelemetryEnvelopeIdentity(
            checkpoint["identity"]![TelemetryEnvelopeFieldNames.ParticipantId]!.GetValue<string>(),
            checkpoint["identity"]![TelemetryEnvelopeFieldNames.Condition]!.GetValue<string>(),
            checkpoint["identity"]![TelemetryEnvelopeFieldNames.ApplicationId]!.GetValue<string>(),
            checkpoint["identity"]![TelemetryEnvelopeFieldNames.TaskId]!.GetValue<string>(),
            checkpoint["identity"]![TelemetryEnvelopeFieldNames.RecordProvenance]!.GetValue<string>());
        var reboundKey = EvaluationTelemetryOutboxKeys.BuildTaskStorageKey(
            checkpoint["session_id"]!.GetValue<string>(), probeIdentity);
        checkpoint["storage_key"] = reboundKey;
        store.Delete(key);
        store.Write(reboundKey, checkpoint.ToJsonString());
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        var session = await StartTaskAsync(restored, identity: probeIdentity);

        Assert.Equal("run-1", session.SessionId);
        Assert.Equal(checkpoint.ToJsonString(), store.Checkpoints[reboundKey]);
        Assert.Empty(transport.Bodies);
    }

    [Theory]
    [InlineData("extra")]
    [InlineData("missing")]
    [InlineData("wrong_type")]
    [InlineData("duplicate")]
    public async Task CorruptRestoredAcknowledgedEventData_FailsClosedBeforeTransport(string corruption)
    {
        var store = await CreatePersistedFocusCheckpointAsync();
        if (corruption == "duplicate")
        {
            MutateStoredBodyText(
                store,
                "acknowledged_events",
                1,
                body => body.Replace(
                    "\"field_id\":\"field-1\"",
                    "\"field_id\":\"field-1\",\"field_id\":\"field-2\"",
                    StringComparison.Ordinal));
        }
        else
        {
            MutateStoredBody(store, "acknowledged_events", 1, envelope =>
            {
                var data = envelope[TelemetryEnvelopeFieldNames.Data]!.AsObject();
                switch (corruption)
                {
                    case "extra":
                        data["unmodeled"] = true;
                        break;
                    case "missing":
                        data.Remove(TelemetryPayloadProperties.FieldId);
                        break;
                    case "wrong_type":
                        data[TelemetryPayloadProperties.FieldId] = 42;
                        break;
                }
            });
        }

        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(transport.Bodies);
    }

    [Theory]
    [InlineData("state_version")]
    [InlineData("adoption")]
    [InlineData("assessability")]
    public async Task CorruptRestoredDurableAdoption_FailsClosedBeforeTransport(string corruption)
    {
        var store = await CreatePersistedFocusCheckpointAsync();
        var key = Assert.Single(store.Checkpoints.Keys);
        var checkpoint = JsonNode.Parse(store.Checkpoints[key])!.AsObject();
        var transitionAdoption = ReadTransitionAdoption(checkpoint);
        switch (corruption)
        {
            case "state_version":
                transitionAdoption["last_state_version"] =
                    transitionAdoption["last_state_version"]!.GetValue<int>() + 1;
                break;
            case "adoption":
                transitionAdoption["adopted_transition_identities"] = null;
                break;
            case "assessability":
                checkpoint["step_metric_not_assessable"] = true;
                break;
        }
        store.Write(key, checkpoint.ToJsonString());
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(transport.Bodies);
    }

    [Fact]
    public async Task CorruptSerializedDetailedStatusBatchBoundary_FailsClosedBeforeTransport()
    {
        var store = await CreatePersistedDetailedStatusBatchCheckpointAsync();
        var checkpoint = JsonNode.Parse(Assert.Single(store.Checkpoints.Values))!.AsObject();
        var acknowledgedEvents = checkpoint["acknowledged_events"]!.AsArray();
        var batchIndexes = acknowledgedEvents
            .Select((node, index) => new
            {
                Index = index,
                EventName = node!["event_name"]!.GetValue<string>(),
                StateVersion = node["state_version"]!.GetValue<int>(),
            })
            .Where(item => string.Equals(
                item.EventName,
                TelemetryEventNames.FieldCompletionChanged,
                StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(2, batchIndexes.Length);
        Assert.Single(batchIndexes.Select(item => item.StateVersion).Distinct());

        MutateStoredEventStateVersion(
            store,
            "acknowledged_events",
            batchIndexes[1].Index,
            batchIndexes[1].StateVersion + 1);
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(transport.Bodies);
    }

    [Fact]
    public async Task CompletionBaseline_IsDurableAndSilent_ThenARealFlipAdvancesExactlyOnce()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var outbox = CreateOutbox(store, transport);
        var session = await StartTaskAsync(outbox);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 1);
        var baseline = new[]
        {
            Assert.Single(TelemetryCanonicalTransitionProjection.ForFieldCompletionChanged("field-1", completed: false)),
            Assert.Single(TelemetryCanonicalTransitionProjection.ForFieldGroupCompletionChanged("group-1", completed: false)),
        };

        outbox.PrimeTaskCompletionAdoption(session, Identity(), baseline);

        var primedCheckpoint = JsonNode.Parse(Assert.Single(store.Checkpoints.Values))!.AsObject();
        var primedTransitionAdoption = ReadTransitionAdoption(primedCheckpoint);
        Assert.Equal(1, primedTransitionAdoption["last_state_version"]!.GetValue<int>());
        Assert.Single(transport.Bodies);
        var primedEntries = primedTransitionAdoption["ledger_entries"]!.AsArray();
        Assert.Equal(2, primedEntries.Count);
        var baselineEntry = primedEntries[1]!.AsObject();
        Assert.Equal("completion_baseline", baselineEntry["kind"]!.GetValue<string>());
        Assert.Equal(1, baselineEntry["state_version"]!.GetValue<int>());
        Assert.Empty(baselineEntry["event_ids"]!.AsArray());
        Assert.Equal(2, baselineEntry["transition_identities"]!.AsArray().Count);

        outbox.EnqueueTaskEvents(
            session,
            Identity(),
            [CreateFieldCompletionInput("field-1", completed: true)]);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 2);

        Assert.Equal(2, ReadInt(transport.Bodies[1], TelemetryEnvelopeFieldNames.StateVersion));
        var flippedCheckpoint = JsonNode.Parse(Assert.Single(store.Checkpoints.Values))!.AsObject();
        Assert.Equal(
            2,
            ReadTransitionAdoption(flippedCheckpoint)["last_state_version"]!.GetValue<int>());
    }

    [Fact]
    public async Task AdjacentDetailedStatusBatchesWithTheSameStateVersion_RestoreAsDistinctOperations()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var outbox = CreateOutbox(store, new RecordingEvaluationTelemetryDeliveryTransport());
        var session = await StartTaskAsync(outbox);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 1);

        outbox.EnqueueTaskEvents(
            session,
            Identity(),
            [CreateFieldCompletionInput("field-1", completed: true)]);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 2);
        outbox.EnqueueTaskEvents(
            session,
            Identity(),
            [
                CreateFieldCompletionInput("field-1", completed: true),
                CreateFieldCompletionInput("field-2", completed: true),
            ]);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 4);

        var checkpoint = JsonNode.Parse(Assert.Single(store.Checkpoints.Values))!.AsObject();
        var eventBatches = ReadTransitionAdoption(checkpoint)["ledger_entries"]!.AsArray()
            .Select(static node => node!.AsObject())
            .Where(static entry => entry["kind"]!.GetValue<string>() == "event_batch")
            .ToArray();
        Assert.Equal(3, eventBatches.Length);
        Assert.Equal(
            eventBatches[1]["state_version"]!.GetValue<int>(),
            eventBatches[2]["state_version"]!.GetValue<int>());
        Assert.Single(eventBatches[1]["event_ids"]!.AsArray());
        Assert.Equal(2, eventBatches[2]["event_ids"]!.AsArray().Count);
        outbox.Dispose();

        var restoredTransport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, restoredTransport);
        var restoredSession = await StartTaskAsync(restored, Session() with { SessionId = "restored-proposal" });

        Assert.Equal(session.SessionId, restoredSession.SessionId);
        Assert.Equal(
            ParticipantTaskTerminationClassification.SessionAbandoned,
            Assert.IsType<EvaluationSessionRestoredTerminal>(restoredSession.RestoredTerminal).TerminationClassification);
    }

    [Fact]
    public async Task SplittingOnePersistedEventBatchIntoTwoLedgerEntries_FailsClosedBeforeTransport()
    {
        var store = await CreatePersistedDetailedStatusBatchCheckpointAsync();
        var key = Assert.Single(store.Checkpoints.Keys);
        var checkpoint = JsonNode.Parse(store.Checkpoints[key])!.AsObject();
        var entries = ReadTransitionAdoption(checkpoint)["ledger_entries"]!.AsArray();
        var batchIndex = entries
            .Select((node, index) => (Entry: node!.AsObject(), Index: index))
            .Single(item => item.Entry["event_ids"]!.AsArray().Count == 2)
            .Index;
        var batch = entries[batchIndex]!.AsObject();
        var eventIds = batch["event_ids"]!.AsArray().Select(static node => node!.GetValue<string>()).ToArray();
        entries.RemoveAt(batchIndex);
        entries.Insert(batchIndex, JsonNode.Parse(batch.ToJsonString()));
        entries.Insert(batchIndex + 1, JsonNode.Parse(batch.ToJsonString()));
        entries[batchIndex]!["event_ids"] = new JsonArray(eventIds[0]);
        entries[batchIndex + 1]!["event_ids"] = new JsonArray(eventIds[1]);
        store.Write(key, checkpoint.ToJsonString());
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(transport.Bodies);
    }

    [Theory]
    [InlineData("remove")]
    [InlineData("duplicate")]
    [InlineData("reorder")]
    [InlineData("unknown")]
    public async Task CorruptPersistedEventBatchMembership_FailsClosedBeforeTransport(string corruption)
    {
        var store = await CreatePersistedDetailedStatusBatchCheckpointAsync();
        var key = Assert.Single(store.Checkpoints.Keys);
        var checkpoint = JsonNode.Parse(store.Checkpoints[key])!.AsObject();
        var batch = ReadTransitionAdoption(checkpoint)["ledger_entries"]!.AsArray()
            .Select(static node => node!.AsObject())
            .Single(static entry => entry["event_ids"]!.AsArray().Count == 2);
        var eventIds = batch["event_ids"]!.AsArray();
        switch (corruption)
        {
            case "remove":
                eventIds.RemoveAt(1);
                break;
            case "duplicate":
                eventIds.Add(eventIds[0]!.GetValue<string>());
                break;
            case "reorder":
                var firstEventId = eventIds[0]!.GetValue<string>();
                var secondEventId = eventIds[1]!.GetValue<string>();
                eventIds[0] = secondEventId;
                eventIds[1] = firstEventId;
                break;
            case "unknown":
                eventIds[1] = new string('0', 32);
                break;
        }

        store.Write(key, checkpoint.ToJsonString());
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(transport.Bodies);
    }

    [Theory]
    [InlineData("remove")]
    [InlineData("partial")]
    [InlineData("object_id")]
    [InlineData("scope")]
    [InlineData("resulting_state")]
    [InlineData("state_version")]
    public async Task CorruptPersistedCompletionBaseline_FailsClosedBeforeTransport(string corruption)
    {
        var store = await CreatePersistedCompletionBaselineCheckpointAsync();
        var key = Assert.Single(store.Checkpoints.Keys);
        var checkpoint = JsonNode.Parse(store.Checkpoints[key])!.AsObject();
        var entries = ReadTransitionAdoption(checkpoint)["ledger_entries"]!.AsArray();
        var baselineIndex = entries
            .Select((node, index) => (Entry: node!.AsObject(), Index: index))
            .Single(static item => item.Entry["kind"]!.GetValue<string>() == "completion_baseline")
            .Index;
        var baseline = entries[baselineIndex]!.AsObject();
        switch (corruption)
        {
            case "remove":
                entries.RemoveAt(baselineIndex);
                break;
            case "partial":
                baseline["transition_identities"]!.AsArray().RemoveAt(1);
                break;
            case "object_id":
                baseline["transition_identities"]![0] =
                    "field_completion_changed\u001ffield\u001f\u001fincomplete";
                break;
            case "scope":
                baseline["transition_identities"]![0] =
                    "field_completion_changed\u001ffield_group\u001ffield-1\u001fincomplete";
                break;
            case "resulting_state":
                baseline["transition_identities"]![0] =
                    "field_completion_changed\u001ffield\u001ffield-1\u001ftargeted";
                break;
            case "state_version":
                baseline["state_version"] = baseline["state_version"]!.GetValue<int>() + 1;
                break;
        }

        store.Write(key, checkpoint.ToJsonString());
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(transport.Bodies);
    }

    [Fact]
    public async Task CorruptRestoredPendingTerminalBody_FailsClosedBeforeTransport()
    {
        var store = await CreatePersistedEndingCheckpointAsync();
        MutateStoredBody(store, "pending_events", 0, envelope =>
        {
            envelope[TelemetryEnvelopeFieldNames.Data]![TelemetryPayloadProperties.EndState] =
                TelemetryTaskEndStates.Aborted;
        });
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(transport.Bodies);
    }

    [Fact]
    public async Task CorruptRestoredFaultedTerminalBody_FailsClosedBeforeTransport()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var initialTransport = new RecordingEvaluationTelemetryDeliveryTransport();
        initialTransport.Enqueue(Accepted());
        initialTransport.Enqueue(
            new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.PermanentFailure, 422));
        var initial = CreateOutbox(store, initialTransport);
        var session = await StartTaskAsync(initial);
        await Assert.ThrowsAsync<EvaluationTelemetryDeliveryRejectedException>(
            () => initial.RequestTaskEndAsync(session, Identity(), SuccessfulOutcome(), CancellationToken.None));
        MutateStoredBody(store, "pending_events", 0, envelope =>
        {
            envelope[TelemetryEnvelopeFieldNames.Data]![TelemetryPayloadProperties.EndState] =
                TelemetryTaskEndStates.Aborted;
        });
        var restoredTransport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, restoredTransport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(restoredTransport.Bodies);
    }

    [Theory]
    [InlineData("acknowledged_body")]
    [InlineData("accepted_receipt_body")]
    public async Task CorruptRestoredAcceptedTerminalBodyOrReceipt_FailsClosedBeforeTransport(string target)
    {
        var store = await CreatePersistedAcceptedCheckpointAsync();
        if (target == "acknowledged_body")
        {
            MutateStoredBody(store, "acknowledged_events", 1, envelope =>
            {
                envelope[TelemetryEnvelopeFieldNames.Data]![TelemetryPayloadProperties.EndState] =
                TelemetryTaskEndStates.Aborted;
            });
        }
        else
        {
            var key = Assert.Single(store.Checkpoints.Keys);
            var checkpoint = JsonNode.Parse(store.Checkpoints[key])!.AsObject();
            var receipt = checkpoint["accepted_receipt"]!.AsObject();
            receipt["body"] = receipt["body"]!.GetValue<string>().Replace(
                "\"end_state\":\"process_submitted\"",
                "\"end_state\":\"aborted\"",
                StringComparison.Ordinal);
            store.Write(key, checkpoint.ToJsonString());
        }

        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));
        Assert.Empty(transport.Bodies);
    }

    [Fact]
    public async Task CorruptRestoredAcceptedTerminalBodyAndReceiptTogether_FailsAgainstDurableIntentBeforeTransport()
    {
        var store = await CreatePersistedAcceptedCheckpointAsync();
        var key = Assert.Single(store.Checkpoints.Keys);
        var checkpoint = JsonNode.Parse(store.Checkpoints[key])!.AsObject();
        var terminal = checkpoint["acknowledged_events"]!.AsArray()[1]!.AsObject();
        var mutatedBody = terminal["body"]!.GetValue<string>().Replace(
            "\"end_state\":\"process_submitted\"",
            "\"end_state\":\"aborted\"",
            StringComparison.Ordinal);
        var mutatedHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(mutatedBody)));
        terminal["body"] = mutatedBody;
        terminal["body_sha256"] = mutatedHash;
        var receipt = checkpoint["accepted_receipt"]!.AsObject();
        receipt["body"] = mutatedBody;
        receipt["body_sha256"] = mutatedHash;
        store.Write(key, checkpoint.ToJsonString());
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(transport.Bodies);
    }

    [Theory]
    [InlineData("participant_finished")]
    [InlineData(TelemetryTaskTerminationClassifications.StudyDirectedAbort)]
    public async Task CorruptRestoredParticipantTerminalClassification_FailsClosedBeforeTransport(
        string terminationClassification)
    {
        var store = await CreatePersistedAcceptedCheckpointAsync();
        var key = Assert.Single(store.Checkpoints.Keys);
        var checkpoint = JsonNode.Parse(store.Checkpoints[key])!.AsObject();
        checkpoint["terminal_intent"]!["termination_classification"] = terminationClassification;
        store.Write(key, checkpoint.ToJsonString());
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(transport.Bodies);
    }

    [Fact]
    public async Task AcceptedTerminalWithFailedLocalClear_RetainsExactReceiptAndRestoresAsCommittedWithoutRedelivery()
    {
        var (store, acceptedTerminalBody) = await CreateCommittedCheckpointAfterFailedClearAsync();
        var committed = Assert.Single(store.Checkpoints.Values);
        Assert.Contains("\"phase\":\"committed\"", committed, StringComparison.Ordinal);
        Assert.Contains(JsonSerializer.Serialize(acceptedTerminalBody), committed, StringComparison.Ordinal);
        var restoredTransport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, restoredTransport);

        var restoredSession = await StartTaskAsync(restored, Session() with { SessionId = "run-2" });
        var restoredTerminal = Assert.IsType<EvaluationSessionRestoredTerminal>(restoredSession.RestoredTerminal);
        var acceptance = await restoredTerminal.Acceptance;

        Assert.Empty(restoredTransport.Bodies);
        await restored.CommitTaskAsync(acceptance, CancellationToken.None);
        Assert.Empty(store.Checkpoints);

        _ = await StartTaskAsync(restored, Session() with { SessionId = "run-2" });

        var body = Assert.Single(restoredTransport.Bodies);
        Assert.Equal(TelemetryEventNames.TaskStart, ReadString(body, TelemetryEnvelopeFieldNames.Event));
    }

    [Fact]
    public async Task AcceptedTerminalAfterProcessLoss_IsAdoptedOnceAndCommittedWithoutRedelivery()
    {
        var store = await CreatePersistedAcceptedCheckpointAsync();
        var restoredTransport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, restoredTransport);

        var restoredSession = await StartTaskAsync(restored, Session() with { SessionId = "new-run" });
        var restoredTerminal = Assert.IsType<EvaluationSessionRestoredTerminal>(restoredSession.RestoredTerminal);
        var acceptance = await restoredTerminal.Acceptance;

        Assert.Equal(Session().SessionId, acceptance.SessionId);
        Assert.Equal(ParticipantTaskTerminationClassification.Completed, restoredTerminal.TerminationClassification);
        Assert.Empty(restoredTransport.Bodies);
        await Assert.ThrowsAsync<EvaluationSessionAlreadyActiveException>(
            () => StartTaskAsync(restored, Session() with { SessionId = "another-run" }));

        await restored.CommitTaskAsync(acceptance, CancellationToken.None);
        await WaitUntilAsync(() => store.Checkpoints.Count == 0);
    }

    [Fact]
    public async Task DuplicateRestoredIdentity_FailsClosedWithoutFetching()
    {
        var store = await CreatePersistedActiveCheckpointAsync();
        var originalKey = Assert.Single(store.Checkpoints.Keys);
        var duplicateSessionId = "duplicate-run";
        var duplicateKey = EvaluationTelemetryOutboxKeys.BuildTaskStorageKey(duplicateSessionId, Identity());
        var duplicate = JsonNode.Parse(store.Checkpoints[originalKey])!.AsObject();
        duplicate["storage_key"] = duplicateKey;
        duplicate["session_id"] = duplicateSessionId;
        store.Write(duplicateKey, duplicate.ToJsonString());
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(transport.Bodies);
    }

    [Fact]
    public async Task DuplicateRestoredCheckpointMember_FailsClosedWithoutFetching()
    {
        var store = await CreatePersistedActiveCheckpointAsync();
        var key = Assert.Single(store.Checkpoints.Keys);
        // Die aktuelle Speicherversion bestimmt den zu ersetzenden Membernamen. Die folgende Prüfung stellt sicher, dass die Mutation tatsächlich greift.
        var versionMember = "\"schema_version\":" + EvaluationTelemetryCheckpointValidator.SchemaVersion;
        var checkpoint = store.Checkpoints[key];
        Assert.Contains(versionMember, checkpoint, StringComparison.Ordinal);
        store.Write(
            key,
            checkpoint.Replace(
                versionMember,
                versionMember + "," + versionMember,
                StringComparison.Ordinal));
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));

        Assert.Empty(transport.Bodies);
    }

    [Fact]
    public async Task Restore_KeyDisappearsBetweenEnumerationAndLeasedRead_IsBenignAndStartsFresh()
    {
        var seed = await CreatePersistedActiveCheckpointAsync();
        var disappearingKey = Assert.Single(seed.Checkpoints.Keys);
        var store = new DisappearingAfterEnumerationStore(seed, disappearingKey);
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);

        var session = await StartTaskAsync(restored, Session() with { SessionId = "fresh-run" });

        Assert.Equal("fresh-run", session.SessionId);
        var body = Assert.Single(transport.Bodies);
        Assert.Equal(TelemetryEventNames.TaskStart, ReadString(body, TelemetryEnvelopeFieldNames.Event));
    }

    [Fact]
    public async Task Restore_CorruptIdentityIsQuarantinedWithoutBlockingAnotherIdentity()
    {
        var store = await CreatePersistedActiveCheckpointAsync();
        MutateStoredBody(store, "acknowledged_events", 0, envelope =>
        {
            envelope[TelemetryEnvelopeFieldNames.ParticipantId] = "P999";
        });
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var restored = CreateOutbox(store, transport);
        var otherIdentity = Identity() with { TaskId = "T02" };

        var session = await restored.StartTaskAsync(
            Session() with { SessionId = "other-run" },
            otherIdentity,
            EmptyPayload(),
            CancellationToken.None);

        Assert.Equal("other-run", session.SessionId);
        Assert.Single(transport.Bodies);
        await Assert.ThrowsAsync<InvalidDataException>(() => StartTaskAsync(restored));
    }

    [Fact]
    public async Task PermanentTerminalFailure_FaultsEndAndKeepsTheCheckpointQuarantined()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        transport.Enqueue(Accepted());
        transport.Enqueue(new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.PermanentFailure, 422));
        var outbox = CreateOutbox(store, transport);
        var session = await StartTaskAsync(outbox);

        var exception = await Assert.ThrowsAsync<EvaluationTelemetryDeliveryRejectedException>(
            () => outbox.RequestTaskEndAsync(session, Identity(), SuccessfulOutcome(), CancellationToken.None));

        Assert.Equal(422, exception.StatusCode);
        Assert.Contains("\"phase\":\"faulted\"", Assert.Single(store.Checkpoints.Values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SupersededV9Checkpoint_IsNotReadByTheV1Outbox()
    {
        // Das versionsabhängige Schlüsselpräfix hält ältere Speicherformen aus dem aktuellen Wiederanlauf heraus.
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        store.Write("sprechen.evaluation.outbox.v9:task:old", "{}");
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var outbox = CreateOutbox(store, transport);

        _ = await StartTaskAsync(outbox);

        Assert.Single(transport.Bodies);
        Assert.Contains(store.Checkpoints.Keys, key => key.StartsWith("sprechen.evaluation.outbox.v1:", StringComparison.Ordinal));
    }

    private static async Task<InMemoryEvaluationTelemetryOutboxStore> CreatePersistedActiveCheckpointAsync()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var outbox = CreateOutbox(store, transport);
        _ = await StartTaskAsync(outbox);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 1);
        outbox.Dispose();
        return store;
    }

    private static async Task<InMemoryEvaluationTelemetryOutboxStore> CreatePersistedFocusCheckpointAsync()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var outbox = CreateOutbox(store, new RecordingEvaluationTelemetryDeliveryTransport());
        var session = await StartTaskAsync(outbox);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 1);
        var attachment = EvaluationTelemetryTransitionAttacher.Attach(
            TelemetryEventNames.FocusChange,
            new Dictionary<string, object?>
            {
                [TelemetryPayloadProperties.FieldId] = "field-1",
            });
        outbox.EnqueueTaskEvent(
            session,
            Identity(),
            TelemetryEventNames.FocusChange,
            attachment.Payload,
            attachment.Transitions);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 2);
        outbox.Dispose();
        return store;
    }

    private static async Task<InMemoryEvaluationTelemetryOutboxStore>
        CreatePersistedDetailedStatusBatchCheckpointAsync()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var outbox = CreateOutbox(store, new RecordingEvaluationTelemetryDeliveryTransport());
        var session = await StartTaskAsync(outbox);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 1);

        var focusAttachment = EvaluationTelemetryTransitionAttacher.Attach(
            TelemetryEventNames.FocusChange,
            new Dictionary<string, object?>
            {
                [TelemetryPayloadProperties.FieldId] = "field-1",
            });
        outbox.EnqueueTaskEvent(
            session,
            Identity(),
            TelemetryEventNames.FocusChange,
            focusAttachment.Payload,
            focusAttachment.Transitions);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 2);

        var batch = new[] { "field-1", "field-2" }
            .Select(fieldId =>
            {
                var attachment = EvaluationTelemetryTransitionAttacher.Attach(
                    TelemetryEventNames.FieldCompletionChanged,
                    new Dictionary<string, object?>
                    {
                        [TelemetryPayloadProperties.FieldId] = fieldId,
                        [TelemetryPayloadProperties.FieldGroupId] = "group-1",
                        [TelemetryPayloadProperties.Relevant] = true,
                        [TelemetryPayloadProperties.Required] = true,
                        [TelemetryPayloadProperties.Completed] = true,
                    });
                return new EvaluationTelemetryTaskEventInput(
                    TelemetryEventNames.FieldCompletionChanged,
                    attachment.Payload,
                    attachment.Transitions);
            })
            .ToArray();
        outbox.EnqueueTaskEvents(session, Identity(), batch);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 4);
        outbox.Dispose();
        return store;
    }

    private static async Task<InMemoryEvaluationTelemetryOutboxStore>
        CreatePersistedCompletionBaselineCheckpointAsync()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var outbox = CreateOutbox(store, new RecordingEvaluationTelemetryDeliveryTransport());
        var session = await StartTaskAsync(outbox);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 1);
        outbox.PrimeTaskCompletionAdoption(
            session,
            Identity(),
            [
                Assert.Single(TelemetryCanonicalTransitionProjection.ForFieldCompletionChanged(
                    "field-1",
                    completed: false)),
                Assert.Single(TelemetryCanonicalTransitionProjection.ForFieldGroupCompletionChanged(
                    "group-1",
                    completed: false)),
            ]);
        outbox.Dispose();
        return store;
    }

    private static EvaluationTelemetryTaskEventInput CreateFieldCompletionInput(string fieldId, bool completed)
    {
        var attachment = EvaluationTelemetryTransitionAttacher.Attach(
            TelemetryEventNames.FieldCompletionChanged,
            new Dictionary<string, object?>
            {
                [TelemetryPayloadProperties.FieldId] = fieldId,
                [TelemetryPayloadProperties.FieldGroupId] = "group-1",
                [TelemetryPayloadProperties.Relevant] = true,
                [TelemetryPayloadProperties.Required] = true,
                [TelemetryPayloadProperties.Completed] = completed,
            });
        return new EvaluationTelemetryTaskEventInput(
            TelemetryEventNames.FieldCompletionChanged,
            attachment.Payload,
            attachment.Transitions);
    }

    private static async Task<InMemoryEvaluationTelemetryOutboxStore> CreatePersistedEndingCheckpointAsync()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var transport = new BlockingTerminalEvaluationTelemetryDeliveryTransport();
        var outbox = CreateOutbox(store, transport);
        var session = await StartTaskAsync(outbox);
        _ = outbox.RequestTaskEndAsync(session, Identity(), SuccessfulOutcome(), CancellationToken.None);
        await transport.TerminalObserved;
        outbox.Dispose();
        return store;
    }

    private static async Task<InMemoryEvaluationTelemetryOutboxStore> CreatePersistedAcceptedCheckpointAsync()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var outbox = CreateOutbox(store, new RecordingEvaluationTelemetryDeliveryTransport());
        var session = await StartTaskAsync(outbox);
        _ = Assert.IsType<EvaluationSessionAcceptance>(
            await outbox.RequestTaskEndAsync(session, Identity(), SuccessfulOutcome(), CancellationToken.None));
        outbox.Dispose();
        return store;
    }

    private static async Task<(InMemoryEvaluationTelemetryOutboxStore Store, string TerminalBody)>
        CreateCommittedCheckpointAfterFailedClearAsync()
    {
        var store = new InMemoryEvaluationTelemetryOutboxStore { DeleteFailuresRemaining = 1 };
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var cleanupRetry = new BlockingEvaluationTelemetryRetryDelay();
        var outbox = CreateOutbox(store, transport, retryDelay: cleanupRetry);
        var session = await StartTaskAsync(outbox);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 1);

        var acceptance = Assert.IsType<EvaluationSessionAcceptance>(
            await outbox.RequestTaskEndAsync(session, Identity(), SuccessfulOutcome(), CancellationToken.None));
        var commitTask = outbox.CommitTaskAsync(acceptance, CancellationToken.None);
        await cleanupRetry.WaitObserved;
        Assert.False(commitTask.IsCompleted);

        var terminalBody = transport.Bodies.Single(
            body => ReadString(body, TelemetryEnvelopeFieldNames.Event) == TelemetryEventNames.TaskEnd);
        Assert.Single(store.Checkpoints);
        outbox.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => commitTask);
        return (store, terminalBody);
    }

    private static int AcknowledgedEventCount(InMemoryEvaluationTelemetryOutboxStore store)
    {
        var checkpoint = JsonNode.Parse(Assert.Single(store.Checkpoints.Values))!.AsObject();
        return checkpoint["acknowledged_events"]!.AsArray().Count;
    }

    private static JsonObject ReadTransitionAdoption(JsonObject checkpoint)
        => checkpoint["transition_adoption_state"]!.AsObject();

    private static void MutateStoredBody(
        InMemoryEvaluationTelemetryOutboxStore store,
        string collectionName,
        int index,
        Action<JsonObject> mutation)
    {
        MutateStoredBodyText(store, collectionName, index, body =>
        {
            var envelope = JsonNode.Parse(body)!.AsObject();
            mutation(envelope);
            return envelope.ToJsonString();
        });
    }

    private static void MutateStoredBodyText(
        InMemoryEvaluationTelemetryOutboxStore store,
        string collectionName,
        int index,
        Func<string, string> mutation)
    {
        var key = Assert.Single(store.Checkpoints.Keys);
        var checkpoint = JsonNode.Parse(store.Checkpoints[key])!.AsObject();
        var storedEvent = checkpoint[collectionName]!.AsArray()[index]!.AsObject();
        var mutatedBody = mutation(storedEvent["body"]!.GetValue<string>());
        storedEvent["body"] = mutatedBody;
        storedEvent["body_sha256"] = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(mutatedBody)));
        store.Write(key, checkpoint.ToJsonString());
    }

    private static void MutateStoredEventStateVersion(
        InMemoryEvaluationTelemetryOutboxStore store,
        string collectionName,
        int index,
        int stateVersion)
    {
        var key = Assert.Single(store.Checkpoints.Keys);
        var checkpoint = JsonNode.Parse(store.Checkpoints[key])!.AsObject();
        var storedEvent = checkpoint[collectionName]!.AsArray()[index]!.AsObject();
        storedEvent["state_version"] = stateVersion;
        var envelope = JsonNode.Parse(storedEvent["body"]!.GetValue<string>())!.AsObject();
        envelope[TelemetryEnvelopeFieldNames.StateVersion] = stateVersion;
        var mutatedBody = envelope.ToJsonString();
        storedEvent["body"] = mutatedBody;
        storedEvent["body_sha256"] = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(mutatedBody)));
        store.Write(key, checkpoint.ToJsonString());
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    /// <summary>
    /// Prüft die Zuordnung der Hüllfelder im Zustellungsprotokoll.
    /// </summary>
    [Fact]
    public async Task LogEnqueued_BindsEveryEnvelopeMemberToItsOwnValue()
    {
        var (enqueued, envelope) = await EmitDeliveryEnqueuedRecordAsync();

        Assert.Equal("P123", Bound(enqueued, TelemetryEnvelopeFieldNames.ParticipantId));
        Assert.Equal("B", Bound(enqueued, TelemetryEnvelopeFieldNames.Condition));
        Assert.Equal("test-app", Bound(enqueued, TelemetryEnvelopeFieldNames.ApplicationId));
        Assert.Equal("T01", Bound(enqueued, TelemetryEnvelopeFieldNames.TaskId));
        Assert.Equal(
            TelemetryRecordProvenances.Participant,
            Bound(enqueued, TelemetryEnvelopeFieldNames.RecordProvenance));
        Assert.Equal(TelemetryEventNames.TaskStart, Bound(enqueued, TelemetryEnvelopeFieldNames.Event));

        // Die erzeugten Kennungen und die Zustandsversion werden mit der gesendeten Hülle verglichen; bloße Anwesenheit würde Vertauschungen übersehen.
        Assert.Equal(
            ReadString(envelope, TelemetryEnvelopeFieldNames.EventId),
            Bound(enqueued, TelemetryEnvelopeFieldNames.EventId));
        Assert.Equal(
            ReadString(envelope, TelemetryEnvelopeFieldNames.SessionId),
            Bound(enqueued, TelemetryEnvelopeFieldNames.SessionId));
        Assert.Equal(
            ReadInt(envelope, TelemetryEnvelopeFieldNames.ClientOffsetMs).ToString(CultureInfo.InvariantCulture),
            Bound(enqueued, TelemetryEnvelopeFieldNames.ClientOffsetMs));
        Assert.Equal(
            ReadInt(envelope, TelemetryEnvelopeFieldNames.StateVersion).ToString(CultureInfo.InvariantCulture),
            Bound(enqueued, TelemetryEnvelopeFieldNames.StateVersion));
        Assert.Contains(
            TelemetryPayloadProperties.CanonicalTransitions,
            Bound(enqueued, TelemetryEnvelopeFieldNames.Data),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Unterschiedliche Werte machen vertauschte Hüllfelder im Zuordnungstest erkennbar. Die Prüfung schließt auch erzeugte Werte ein.
    /// </summary>
    [Fact]
    public async Task LogEnqueued_BindsNoTwoEnvelopeMembersToTheSameValue()
    {
        var (enqueued, _) = await EmitDeliveryEnqueuedRecordAsync();

        var collisions = enqueued.Values
            .Where(bound => !string.Equals(bound.Key, "{OriginalFormat}", StringComparison.Ordinal))
            .GroupBy(
                bound => Convert.ToString(bound.Value, CultureInfo.InvariantCulture) ?? string.Empty,
                StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => string.Join(
                " and ",
                group.Select(bound => bound.Key).Order(StringComparer.Ordinal))
                + $" both carry \"{group.Key}\"")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            collisions.Length == 0,
            "These envelope members carry the same value, so the assertions holding them cannot "
            + "tell them apart and the emit call can bind that pair in either order without "
            + $"failing anything. Give each member a value of its own:{Environment.NewLine}"
            + string.Join(Environment.NewLine, collisions));
    }

    /// <summary>
    /// Zwei unterschiedliche Eingaben prüfen, ob jedes protokollierte Hüllfeld auf seine Eingabe reagiert.
    /// </summary>
    [Fact]
    public async Task LogEnqueued_BindsNoEnvelopeMemberToAValueItsInputDidNotSupply()
    {
        var (enqueued, _) = await EmitDeliveryEnqueuedRecordAsync();
        var otherEnqueued = await EmitOtherDeliveryEnqueuedRecordAsync();

        var boundToTheProducersOwnValue = BoundValues(enqueued)
            .Where(bound => string.Equals(
                bound.Value,
                BoundValues(otherEnqueued).GetValueOrDefault(bound.Key),
                StringComparison.Ordinal))
            .Select(bound => bound.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            boundToTheProducersOwnValue.Length == 0,
            "These envelope members carry one value under two inputs that share none, so the "
            + "emit call binds them to something of its own rather than to what it was given, "
            + "and every hold above passes on a value chosen here. Bind each member from the "
            + $"stored event the diagnostic reads:{Environment.NewLine}"
            + string.Join(Environment.NewLine, boundToTheProducersOwnValue));
    }

    /// <summary>
    /// Prüft die Vollständigkeit der protokollierten Hüllfelder getrennt von ihrer Wertzuordnung.
    /// </summary>
    [Fact]
    public async Task LogEnqueued_CarriesNoMemberBeyondTheEnvelope_AndDropsNone()
    {
        var (enqueued, _) = await EmitDeliveryEnqueuedRecordAsync();

        // OriginalFormat stammt vom Logging-Framework und gehört nicht zur Ereignishülle.
        var bound = enqueued.Values.Keys
            .Where(key => !string.Equals(key, "{OriginalFormat}", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(
            TelemetryEnvelopeFieldNames.All.OrderBy(name => name, StringComparer.Ordinal),
            bound.OrderBy(name => name, StringComparer.Ordinal));
    }

    private static async Task<(CapturingLogEntry Enqueued, string Envelope)>
        EmitDeliveryEnqueuedRecordAsync()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var transport = new RecordingEvaluationTelemetryDeliveryTransport();
        var outbox = CreateOutbox(
            new InMemoryEvaluationTelemetryOutboxStore(),
            transport,
            logger: logger);

        _ = await StartTaskAsync(outbox);

        var enqueued = Assert.Single(
            logger.Entries,
            entry => entry.Values.TryGetValue("{OriginalFormat}", out var format)
                && Convert.ToString(format, CultureInfo.InvariantCulture)!
                    .StartsWith("evaluation_delivery_enqueued ", StringComparison.Ordinal));

        return (enqueued, Assert.Single(transport.Bodies));
    }

    // Die zweite Zustellung nutzt eine andere Identität und andere Ereignisdaten. Ein Folgeereignis unterscheidet auch die Zustandsversion vom ersten Testdatensatz.
    private static async Task<CapturingLogEntry> EmitOtherDeliveryEnqueuedRecordAsync()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var store = new InMemoryEvaluationTelemetryOutboxStore();
        var outbox = CreateOutbox(
            store,
            new RecordingEvaluationTelemetryDeliveryTransport(),
            logger: logger);

        var session = await StartTaskAsync(outbox, OtherSession(), OtherIdentity());
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 1);

        // Der Wechsel gegenüber dem vorgegebenen Ausgangszustand erhöht die Zustandsversion.
        outbox.PrimeTaskCompletionAdoption(
            session,
            OtherIdentity(),
            [
                Assert.Single(
                    TelemetryCanonicalTransitionProjection.ForFieldCompletionChanged(
                        "field-9",
                        completed: false)),
                Assert.Single(
                    TelemetryCanonicalTransitionProjection.ForFieldGroupCompletionChanged(
                        "group-1",
                        completed: false)),
            ]);
        outbox.EnqueueTaskEvents(
            session,
            OtherIdentity(),
            [CreateFieldCompletionInput("field-9", completed: true)]);
        await WaitUntilAsync(() => AcknowledgedEventCount(store) == 2);

        return EnqueuedRecordsOf(logger)[^1];
    }

    private static IReadOnlyList<CapturingLogEntry> EnqueuedRecordsOf(
        CapturingLogger<EvaluationTelemetryImpl> logger)
        => logger.Entries
            .Where(entry => entry.Values.TryGetValue("{OriginalFormat}", out var format)
                && Convert.ToString(format, CultureInfo.InvariantCulture)!
                    .StartsWith("evaluation_delivery_enqueued ", StringComparison.Ordinal))
            .ToArray();

    private static IReadOnlyDictionary<string, string> BoundValues(CapturingLogEntry entry)
        => entry.Values
            .Where(bound => !string.Equals(bound.Key, "{OriginalFormat}", StringComparison.Ordinal))
            .ToDictionary(
                bound => bound.Key,
                bound => Convert.ToString(bound.Value, CultureInfo.InvariantCulture) ?? string.Empty,
                StringComparer.Ordinal);

    private static string Bound(CapturingLogEntry entry, string fieldName)
    {
        Assert.True(
            entry.Values.TryGetValue(fieldName, out var value),
            $"The enqueued record does not bind {fieldName}.");
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static EvaluationTelemetryTaskOutbox CreateOutbox(
        IEvaluationTelemetryOutboxStore store,
        IEvaluationTelemetryDeliveryTransport transport,
        IEvaluationTelemetryExclusiveLease? exclusiveLease = null,
        IEvaluationTelemetryRetryDelay? retryDelay = null,
        CapturingLogger<EvaluationTelemetryImpl>? logger = null)
    {
        return EvaluationTelemetryTestOutboxFactory.Create(
            new StudyTelemetryContext(),
            logger ?? new CapturingLogger<EvaluationTelemetryImpl>(),
            store,
            transport,
            exclusiveLease,
            retryDelay);
    }

    private static Task<EvaluationSession> StartTaskAsync(
        EvaluationTelemetryTaskOutbox outbox,
        EvaluationSession? session = null,
        EvaluationTelemetryEnvelopeIdentity? identity = null)
        => outbox.StartTaskAsync(
            session ?? Session(),
            identity ?? Identity(),
            EmptyPayload(),
            cancellationToken: CancellationToken.None);

    private static EvaluationSession Session()
        => new("run-1", EvaluationChannel.Voice, "test-app", "form-1");

    private static EvaluationTelemetryEnvelopeIdentity Identity()
        => new("P123", "B", "test-app", "T01", TelemetryRecordProvenances.Participant);

    // Auch Herkunft und Versuchsbedingung unterscheiden sich von der ersten Testidentität.
    private static EvaluationTelemetryEnvelopeIdentity OtherIdentity()
        => new(
            "P456",
            "A",
            "other-app",
            "T02",
            TelemetryRecordProvenances.Demonstration);

    private static EvaluationSession OtherSession()
        => new("run-2", EvaluationChannel.Voice, "other-app", "form-2");

    private static object EmptyPayload()
        => new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.CanonicalTransitions] = Array.Empty<object>(),
        };

    private static TelemetryTaskEndOutcome SuccessfulOutcome()
        => new(
            TelemetryTaskEndStates.ProcessSubmitted,
            TelemetryTaskTerminationClassifications.Completed,
            TelemetryMismatchCodes.None,
            TelemetryCorrectnessBases.LogAndSystemCheck);

    private static EvaluationTelemetryDeliveryResult Accepted()
        => new(EvaluationTelemetryDeliveryStatus.Accepted, 204);

    private static string ReadString(string body, string propertyName)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty(propertyName).GetString()!;
    }

    private static int ReadInt(string body, string propertyName)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty(propertyName).GetInt32();
    }

}
