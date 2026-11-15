using System.Collections.Concurrent;
using Sprechen.Speech.App.Contracts.Evaluation;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

/// <summary>
/// Leitet Telemetrieaufrufe weiter und sammelt ausgewählte Argumente als Text.
/// Workflowtests können diese Aufzeichnungen auf unerwünschten Klartext prüfen.
/// </summary>
internal sealed class RecordingEvaluationTelemetry : IEvaluationTelemetry
{
    private readonly IEvaluationTelemetry _inner;
    private readonly ConcurrentQueue<RecordedTelemetryCall> _calls = new();

    public RecordingEvaluationTelemetry(IEvaluationTelemetry inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IReadOnlyList<RecordedTelemetryCall> Calls => _calls.ToArray();

    /// <summary>
    /// Die ausdrücklich aufgezeichneten Texte der Telemetrieaufrufe.
    /// In übergebenen Objekten enthaltene Texte werden nicht rekursiv erfasst.
    /// </summary>
    public IReadOnlyList<string> StringArguments => _calls
        .SelectMany(call => call.StringArguments)
        .ToArray();

    public async Task<EvaluationSession> StartSessionAsync(
        string applicationId,
        string formId,
        string requestedLanguage,
        EvaluationChannel channel,
        CancellationToken cancellationToken)
    {
        Record(nameof(StartSessionAsync), applicationId, formId, requestedLanguage);
        return await _inner.StartSessionAsync(
            applicationId,
            formId,
            requestedLanguage,
            channel,
            cancellationToken);
    }

    public async Task<EvaluationSessionAcceptance?> EndSessionAsync(
        EvaluationSession session,
        EvaluationOutcome outcome,
        EvaluationTaskCompletion completion,
        CancellationToken cancellationToken)
    {
        Record(nameof(EndSessionAsync), outcome.ToString());
        return await _inner.EndSessionAsync(session, outcome, completion, cancellationToken);
    }

    public Task CommitSessionAsync(EvaluationSessionAcceptance acceptance, CancellationToken cancellationToken)
        => _inner.CommitSessionAsync(acceptance, cancellationToken);

    public void TrackStateEntry(EvaluationSession session, string state)
    {
        Record(nameof(TrackStateEntry), state);
        _inner.TrackStateEntry(session, state);
    }

    public void TrackBargeIn(EvaluationSession session, string? interruptedPromptId = null)
    {
        Record(nameof(TrackBargeIn), interruptedPromptId);
        _inner.TrackBargeIn(session, interruptedPromptId);
    }

    public void TrackNoSpeechDetected()
    {
        Record(nameof(TrackNoSpeechDetected));
        _inner.TrackNoSpeechDetected();
    }

    public void TrackFieldFocus(EvaluationSession session, string fieldId)
    {
        Record(nameof(TrackFieldFocus), fieldId);
        _inner.TrackFieldFocus(session, fieldId);
    }

    public void TrackFieldAttempt(EvaluationSession session, string fieldId, string inputMode, bool success, int? sttConfidencePercent = null)
    {
        Record(nameof(TrackFieldAttempt), fieldId, inputMode, success.ToString());
        _inner.TrackFieldAttempt(session, fieldId, inputMode, success, sttConfidencePercent);
    }

    public void TrackNoSpeechDetected(EvaluationSession session, string fieldId)
    {
        Record(nameof(TrackNoSpeechDetected), fieldId);
        _inner.TrackNoSpeechDetected(session, fieldId);
    }

    public void TrackConstraintViolation(
        EvaluationSession session,
        string fieldId,
        string errorCode,
        string severity,
        string trigger)
    {
        Record(nameof(TrackConstraintViolation), fieldId, errorCode, severity, trigger);
        _inner.TrackConstraintViolation(session, fieldId, errorCode, severity, trigger);
    }

    public void TrackConstraintViolation(
        EvaluationSession session,
        string fieldId,
        string errorCode,
        string severity,
        string scope,
        string? fieldGroupId,
        string trigger)
    {
        Record(nameof(TrackConstraintViolation), fieldId, errorCode, severity, scope, fieldGroupId, trigger);
        _inner.TrackConstraintViolation(session, fieldId, errorCode, severity, scope, fieldGroupId, trigger);
    }

    public void TrackNavigationError(EvaluationSession session, string errorCode, string scope)
    {
        Record(nameof(TrackNavigationError), errorCode, scope);
        _inner.TrackNavigationError(session, errorCode, scope);
    }

    public void TrackNavigationAidNoTarget(EvaluationSession session, string commandId)
    {
        Record(nameof(TrackNavigationAidNoTarget), commandId);
        _inner.TrackNavigationAidNoTarget(session, commandId);
    }

    public void TrackStatusSnapshot(EvaluationSession session, EvaluationFormStatusSnapshot snapshot)
    {
        Record(nameof(TrackStatusSnapshot));
        _inner.TrackStatusSnapshot(session, snapshot);
    }

    public void TrackFieldAndGroupStatus(EvaluationSession session, EvaluationDetailedFormStatus snapshot)
    {
        Record(nameof(TrackFieldAndGroupStatus));
        _inner.TrackFieldAndGroupStatus(session, snapshot);
    }

    public void TrackRepairAttempt(EvaluationSession session, string fieldId, string repairMode)
    {
        Record(nameof(TrackRepairAttempt), fieldId, repairMode);
        _inner.TrackRepairAttempt(session, fieldId, repairMode);
    }

    public void TrackInputModeChanged(EvaluationSession session, string fieldId, string fromMode, string toMode, string trigger)
    {
        Record(nameof(TrackInputModeChanged), fieldId, fromMode, toMode, trigger);
        _inner.TrackInputModeChanged(session, fieldId, fromMode, toMode, trigger);
    }

    public void TrackGlobalCommand(EvaluationSession session, string commandId)
    {
        Record(nameof(TrackGlobalCommand), commandId);
        _inner.TrackGlobalCommand(session, commandId);
    }

    public void TrackCommandRecognitionFailure(EvaluationSession session, string failureReason, string? commandId)
    {
        Record(nameof(TrackCommandRecognitionFailure), failureReason, commandId);
        _inner.TrackCommandRecognitionFailure(session, failureReason, commandId);
    }

    public void TrackDestructiveCommandResolved(EvaluationSession session, string commandId, bool confirmed, bool refractoryBlocked = false)
    {
        Record(nameof(TrackDestructiveCommandResolved), commandId, confirmed.ToString(), refractoryBlocked.ToString());
        _inner.TrackDestructiveCommandResolved(session, commandId, confirmed, refractoryBlocked);
    }

    public void TrackAmbiguityResolved(
        EvaluationSession session,
        string ambiguityType,
        string resolutionStrategy,
        string signalClass,
        string result,
        string? fieldId,
        string? commandId)
    {
        Record(nameof(TrackAmbiguityResolved), ambiguityType, resolutionStrategy, signalClass, result, fieldId, commandId);
        _inner.TrackAmbiguityResolved(session, ambiguityType, resolutionStrategy, signalClass, result, fieldId, commandId);
    }

    private void Record(string method, params string?[] arguments)
        => _calls.Enqueue(new RecordedTelemetryCall(
            method,
            arguments.Where(argument => argument is not null).Select(argument => argument!).ToArray()));
}
