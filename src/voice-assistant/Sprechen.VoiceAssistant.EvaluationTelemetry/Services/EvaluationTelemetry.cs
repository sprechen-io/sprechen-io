namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Bündelt die Schnittstellen für Sitzungs-, Formular- und Befehlsereignisse. Der Zustand bleibt in den jeweils zuständigen Diensten.
/// </summary>
internal sealed class EvaluationTelemetry(
    EvaluationSessionTelemetry sessionTelemetry,
    EvaluationFormTelemetry formTelemetry,
    EvaluationCommandTelemetry commandTelemetry) : IEvaluationTelemetry
{
    private readonly EvaluationCommandTelemetry _commandTelemetry =
        commandTelemetry ?? throw new ArgumentNullException(nameof(commandTelemetry));
    private readonly EvaluationFormTelemetry _formTelemetry =
        formTelemetry ?? throw new ArgumentNullException(nameof(formTelemetry));
    private readonly EvaluationSessionTelemetry _sessionTelemetry =
        sessionTelemetry ?? throw new ArgumentNullException(nameof(sessionTelemetry));

    Task<EvaluationSession> IEvaluationSessionTelemetry.StartSessionAsync(
        string applicationId,
        string formId,
        string requestedLanguage,
        EvaluationChannel channel,
        CancellationToken cancellationToken)
        => _sessionTelemetry.StartSessionAsync(
            applicationId,
            formId,
            requestedLanguage,
            channel,
            cancellationToken);

    Task<EvaluationSessionAcceptance?> IEvaluationSessionTelemetry.EndSessionAsync(
        EvaluationSession session,
        EvaluationOutcome outcome,
        EvaluationTaskCompletion completion,
        CancellationToken cancellationToken)
        => _sessionTelemetry.EndSessionAsync(session, outcome, completion, cancellationToken);

    Task IEvaluationSessionTelemetry.CommitSessionAsync(
        EvaluationSessionAcceptance acceptance,
        CancellationToken cancellationToken)
        => _sessionTelemetry.CommitSessionAsync(acceptance, cancellationToken);

    void IEvaluationSessionTelemetry.TrackStateEntry(EvaluationSession session, string state)
        => _sessionTelemetry.TrackStateEntry(session, state);

    void IEvaluationSessionTelemetry.TrackBargeIn(EvaluationSession session, string? interruptedPromptId)
        => _sessionTelemetry.TrackBargeIn(session, interruptedPromptId);

    void IEvaluationSessionTelemetry.TrackNoSpeechDetected()
        => _sessionTelemetry.TrackNoSpeechDetected();

    void IEvaluationFieldTelemetry.TrackFieldFocus(EvaluationSession session, string fieldId)
        => _formTelemetry.TrackFieldFocus(session, fieldId);

    void IEvaluationFieldTelemetry.TrackFieldAttempt(
        EvaluationSession session,
        string fieldId,
        string inputMode,
        bool success,
        int? sttConfidencePercent)
        => _formTelemetry.TrackFieldAttempt(session, fieldId, inputMode, success, sttConfidencePercent);

    void IEvaluationFieldTelemetry.TrackNoSpeechDetected(EvaluationSession session, string fieldId)
        => _formTelemetry.TrackNoSpeechDetected(session, fieldId);

    void IEvaluationFieldTelemetry.TrackConstraintViolation(
        EvaluationSession session,
        string fieldId,
        string errorCode,
        string severity,
        string trigger)
        => _formTelemetry.TrackConstraintViolation(session, fieldId, errorCode, severity, trigger);

    void IEvaluationFieldTelemetry.TrackConstraintViolation(
        EvaluationSession session,
        string fieldId,
        string errorCode,
        string severity,
        string scope,
        string? fieldGroupId,
        string trigger)
        => _formTelemetry.TrackConstraintViolation(session, fieldId, errorCode, severity, scope, fieldGroupId, trigger);

    void IEvaluationFormStatusTelemetry.TrackNavigationError(EvaluationSession session, string errorCode, string scope)
        => _formTelemetry.TrackNavigationError(session, errorCode, scope);

    void IEvaluationFormStatusTelemetry.TrackNavigationAidNoTarget(EvaluationSession session, string commandId)
        => _formTelemetry.TrackNavigationAidNoTarget(session, commandId);

    void IEvaluationFormStatusTelemetry.TrackStatusSnapshot(EvaluationSession session, EvaluationFormStatusSnapshot snapshot)
        => _formTelemetry.TrackStatusSnapshot(session, snapshot);

    void IEvaluationFormStatusTelemetry.TrackFieldAndGroupStatus(
        EvaluationSession session,
        EvaluationDetailedFormStatus snapshot)
        => _formTelemetry.TrackFieldAndGroupStatus(session, snapshot);

    void IEvaluationFieldTelemetry.TrackRepairAttempt(EvaluationSession session, string fieldId, string repairMode)
        => _formTelemetry.TrackRepairAttempt(session, fieldId, repairMode);

    void IEvaluationFieldTelemetry.TrackInputModeChanged(
        EvaluationSession session,
        string fieldId,
        string fromMode,
        string toMode,
        string trigger)
        => _formTelemetry.TrackInputModeChanged(session, fieldId, fromMode, toMode, trigger);

    void IEvaluationCommandTelemetry.TrackGlobalCommand(EvaluationSession session, string commandId)
        => _commandTelemetry.TrackGlobalCommand(session, commandId);

    void IEvaluationCommandTelemetry.TrackCommandRecognitionFailure(
        EvaluationSession session,
        string failureReason,
        string? commandId)
        => _commandTelemetry.TrackCommandRecognitionFailure(session, failureReason, commandId);

    void IEvaluationCommandTelemetry.TrackDestructiveCommandResolved(
        EvaluationSession session,
        string commandId,
        bool confirmed,
        bool refractoryBlocked)
        => _commandTelemetry.TrackDestructiveCommandResolved(session, commandId, confirmed, refractoryBlocked);

    void IEvaluationCommandTelemetry.TrackAmbiguityResolved(
        EvaluationSession session,
        string ambiguityType,
        string resolutionStrategy,
        string signalClass,
        string result,
        string? fieldId,
        string? commandId)
        => _commandTelemetry.TrackAmbiguityResolved(
            session,
            ambiguityType,
            resolutionStrategy,
            signalClass,
            result,
            fieldId,
            commandId);
}
