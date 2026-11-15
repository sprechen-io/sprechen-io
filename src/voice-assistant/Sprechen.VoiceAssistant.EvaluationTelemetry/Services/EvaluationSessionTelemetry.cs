using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Führt Beginn, Annahme und lokalen Abschluss einer Auswertungsaufgabe zusammen.
/// Unterbrechungen erhalten das zuvor gemerkte Feld als Bezug.
/// </summary>
internal sealed class EvaluationSessionTelemetry(
    IStudyTelemetryContext studyTelemetryContext,
    EvaluationTelemetrySessionStateStore sessionStateStore,
    EvaluationTelemetryEnvelopeEmitter envelopeEmitter,
    EvaluationSessionTelemetryPayloads payloads,
    EvaluationConversationalPacingTelemetry pacingTelemetry) : IEvaluationSessionTelemetry
{
    private readonly EvaluationTelemetryEnvelopeEmitter _envelopeEmitter =
        envelopeEmitter ?? throw new ArgumentNullException(nameof(envelopeEmitter));
    private readonly EvaluationConversationalPacingTelemetry _pacingTelemetry =
        pacingTelemetry ?? throw new ArgumentNullException(nameof(pacingTelemetry));
    private readonly EvaluationSessionTelemetryPayloads _payloads =
        payloads ?? throw new ArgumentNullException(nameof(payloads));
    private readonly EvaluationTelemetrySessionStateStore _sessionStateStore =
        sessionStateStore ?? throw new ArgumentNullException(nameof(sessionStateStore));
    private readonly IStudyTelemetryContext _studyTelemetryContext =
        studyTelemetryContext ?? throw new ArgumentNullException(nameof(studyTelemetryContext));

    public async Task<EvaluationSession> StartSessionAsync(
        string applicationId,
        string formId,
        string requestedLanguage,
        EvaluationChannel channel,
        CancellationToken cancellationToken)
    {
        var session = new EvaluationSession(Guid.NewGuid().ToString("N"), channel, applicationId, formId);
        return await _envelopeEmitter.StartTaskAsync(session, cancellationToken).ConfigureAwait(false);
    }

    public async Task<EvaluationSessionAcceptance?> EndSessionAsync(
        EvaluationSession session,
        EvaluationOutcome outcome,
        EvaluationTaskCompletion completion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(completion);

        var projected = TelemetryTaskEndProjection.Project(
            outcome == EvaluationOutcome.Submitted,
            completion.RequiredFieldsTotal,
            completion.RequiredFieldsCompleted,
            completion.OpenBlockingValidationCount,
            completion.TerminationClassification.ToModelValue());
        var acceptance = await _envelopeEmitter.RequestTaskEndAsync(
                session,
                TelemetryTaskEndProjection.WithComparison(
                    projected.EndState,
                    projected.TerminationClassification,
                    completion.MismatchCode),
                cancellationToken)
            .ConfigureAwait(false);

        _sessionStateStore.ClearSession(session.SessionId);
        return acceptance;
    }

    public Task CommitSessionAsync(
        EvaluationSessionAcceptance acceptance,
        CancellationToken cancellationToken)
        => _envelopeEmitter.CommitTaskAsync(acceptance, cancellationToken);

    public void TrackStateEntry(EvaluationSession session, string state)
    {
        ArgumentNullException.ThrowIfNull(session);

        _pacingTelemetry.TrackStateEntry(session, state);
    }

    public void TrackNoSpeechDetected()
    {
        _envelopeEmitter.EmitOutsideSession(
            TelemetryEventNames.NoSpeechDetected,
            new Dictionary<string, object?>());
    }

    public void TrackBargeIn(EvaluationSession session, string? interruptedPromptId = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!_sessionStateStore.TryGetFocusedField(session.SessionId, out var fieldId)
            || string.IsNullOrWhiteSpace(fieldId))
        {
            throw new InvalidOperationException("Barge-in telemetry requires a previously tracked focused field.");
        }

        Emit(
            session,
            TelemetryEventNames.BargeIn,
            _payloads.CreateBargeIn(fieldId, interruptedPromptId));
        _pacingTelemetry.TrackBargeIn(session, fieldId, interruptedPromptId);
    }

    private void Emit(EvaluationSession session, string eventName, object? data)
    {
        _envelopeEmitter.Emit(session, eventName, data);
    }
}
