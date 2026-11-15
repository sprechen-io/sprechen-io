using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Ordnet Ereignisse ihrer Studien- oder Prüfidentität zu und übergibt sie an die Aufgaben-Outbox.
/// </summary>
internal sealed class EvaluationTelemetryEnvelopeEmitter(
    IStudyTelemetryContext studyTelemetryContext,
    EvaluationTelemetryTaskOutbox taskOutbox,
    IOperationalProbeSession operationalProbeSession)
{
    private readonly IOperationalProbeSession _operationalProbeSession = operationalProbeSession;
    private readonly IStudyTelemetryContext _studyTelemetryContext = studyTelemetryContext ?? throw new ArgumentNullException(nameof(studyTelemetryContext));
    private readonly EvaluationTelemetryTaskOutbox _taskOutbox = taskOutbox ?? throw new ArgumentNullException(nameof(taskOutbox));

    internal Task<EvaluationSession> StartTaskAsync(
        EvaluationSession proposedSession,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(proposedSession);

        var payload = EvaluationTelemetryTransitionAttacher.Attach(TelemetryEventNames.TaskStart, null).Payload;
        return _taskOutbox.StartTaskAsync(
            proposedSession,
            BuildIdentity(proposedSession),
            payload,
            cancellationToken);
    }

    internal void Emit(EvaluationSession session, string eventName, object? data)
    {
        ArgumentNullException.ThrowIfNull(session);

        var attachment = EvaluationTelemetryTransitionAttacher.Attach(eventName, data);
        _taskOutbox.EnqueueTaskEvent(
            session,
            BuildIdentity(session),
            eventName,
            attachment.Payload,
            attachment.Transitions);
    }

    internal void EmitBatch(
        EvaluationSession session,
        IReadOnlyList<EvaluationTelemetryDetailedStatusChange> changes)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(changes);

        var inputs = changes.Select(change =>
        {
            var attachment = EvaluationTelemetryTransitionAttacher.Attach(change.EventName, change.Data);
            return new EvaluationTelemetryTaskEventInput(
                change.EventName,
                attachment.Payload,
                attachment.Transitions);
        }).ToArray();

        this._taskOutbox.EnqueueTaskEvents(
            session,
            this.BuildIdentity(session),
            inputs);
    }

    internal void PrimeFieldAndGroupCompletionAdoption(
        EvaluationSession session,
        IReadOnlyList<TelemetryCanonicalTransition> completionTransitions)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(completionTransitions);

        this._taskOutbox.PrimeTaskCompletionAdoption(
            session,
            this.BuildIdentity(session),
            completionTransitions);
    }

    internal Task<EvaluationSessionAcceptance?> RequestTaskEndAsync(
        EvaluationSession session,
        TelemetryTaskEndOutcome outcome,
        CancellationToken cancellationToken)
        => _taskOutbox.RequestTaskEndAsync(session, BuildIdentity(session), outcome, cancellationToken);

    internal Task CommitTaskAsync(
        EvaluationSessionAcceptance acceptance,
        CancellationToken cancellationToken)
        => _taskOutbox.CommitTaskAsync(acceptance, cancellationToken);

    /// <summary>
    /// Gibt Ereignisse außerhalb einer Formularsitzung mit den festgelegten Platzhaltern für Anwendung und Aufgabe weiter. Angeforderte Prüfläufe unterdrücken diese Ereignisse.
    /// </summary>
    internal void EmitOutsideSession(string eventName, object? data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        if (_operationalProbeSession.IsRequested)
        {
            return;
        }

        _taskOutbox.EnqueueOutsideTask(
            new EvaluationTelemetryEnvelopeIdentity(
                GetParticipantId(),
                "B",
                TelemetryEnvelopeContextDefaults.UnknownApplicationId,
                TelemetryEnvelopeContextDefaults.UnknownTaskId,
                GetRecordProvenance()),
            eventName,
            EvaluationTelemetryTransitionAttacher.Attach(eventName, data).Payload);
    }

    private string GetParticipantId()
    {
        return HasBoundParticipantToken()
            ? _studyTelemetryContext.ParticipantId!.Trim()
            : TelemetryEnvelopeContextDefaults.UnknownParticipantId;
    }

    private string GetTaskId(EvaluationSession session)
    {
        return string.IsNullOrWhiteSpace(session.FormId)
            ? TelemetryEnvelopeContextDefaults.UnknownTaskId
            : session.FormId.Trim();
    }

    private EvaluationTelemetryEnvelopeIdentity BuildIdentity(EvaluationSession session)
    {
        if (_operationalProbeSession.IsRequested && _operationalProbeSession.Context is { } probe)
        {
            return new EvaluationTelemetryEnvelopeIdentity(
                probe.ProbeId,
                MapCondition(session.Channel),
                session.ApplicationId,
                GetTaskId(session),
                TelemetryRecordProvenances.OperationalProbe);
        }

        return new EvaluationTelemetryEnvelopeIdentity(
            GetParticipantId(),
            MapCondition(session.Channel),
            session.ApplicationId,
            GetTaskId(session),
            GetRecordProvenance());
    }

    private bool HasBoundParticipantToken()
        => !string.IsNullOrWhiteSpace(_studyTelemetryContext.ParticipantId);

    private string GetRecordProvenance()
    {
        return HasBoundParticipantToken()
            ? TelemetryRecordProvenances.Participant
            : TelemetryRecordProvenances.Demonstration;
    }

    private static string MapCondition(EvaluationChannel channel)
    {
        return channel switch
        {
            EvaluationChannel.Voice => "B",
            EvaluationChannel.Html => "A",
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Unknown evaluation channel."),
        };
    }
}
