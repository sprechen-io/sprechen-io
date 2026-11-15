using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Erfasst Entwicklungsdiagnostik zum Gesprächsablauf: Hilfe, Wiederholung, Aufnahmeversuche und gegebenenfalls Erkennungssicherheit.
/// Bei gebundener Teilnehmendenkennung werden diese zusätzlichen Signale nicht versendet. Sie sind keine primären Interaktionsschritte. Der flüchtige Versuchszustand wird auch ohne Versand weitergeführt.
/// </summary>
internal sealed class EvaluationConversationalPacingTelemetry(
    EvaluationTelemetrySessionStateStore sessionStateStore,
    EvaluationTelemetryEnvelopeEmitter envelopeEmitter,
    EvaluationConversationalPacingTelemetryPayloads payloads,
    IStudyTelemetryContext studyTelemetryContext)
{
    private const string ListeningState = "LISTENING";
    private const string HelpCommandId = "help";
    private const string RepeatCommandId = "repeat";

    private readonly EvaluationTelemetryEnvelopeEmitter _envelopeEmitter =
        envelopeEmitter ?? throw new ArgumentNullException(nameof(envelopeEmitter));
    private readonly EvaluationConversationalPacingTelemetryPayloads _payloads =
        payloads ?? throw new ArgumentNullException(nameof(payloads));
    private readonly EvaluationTelemetrySessionStateStore _sessionStateStore =
        sessionStateStore ?? throw new ArgumentNullException(nameof(sessionStateStore));
    private readonly IStudyTelemetryContext _studyTelemetryContext =
        studyTelemetryContext ?? throw new ArgumentNullException(nameof(studyTelemetryContext));

    public void TrackStateEntry(EvaluationSession session, string state)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!IsVoice(session)
            || !string.Equals(state, ListeningState, StringComparison.Ordinal))
        {
            return;
        }

        var attemptNumber = _sessionStateStore.RecordListeningStarted(session.SessionId);
        if (attemptNumber <= 1)
        {
            return;
        }

        Emit(
            session,
            _payloads.Create(
                TelemetryConversationalPacingSignalTypes.CaptureRetry,
                attemptNumber: attemptNumber));
    }

    public void TrackFieldAttempt(
        EvaluationSession session,
        string fieldId,
        string inputMode,
        bool success,
        int? sttConfidencePercent)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!IsVoice(session))
        {
            return;
        }

        var attemptNumber = ConsumeListeningAttempt(session);
        Emit(
            session,
            _payloads.Create(
                TelemetryConversationalPacingSignalTypes.FieldAttempt,
                fieldId: fieldId,
                inputMode: inputMode,
                success: success,
                attemptNumber: attemptNumber,
                sttConfidencePercent: sttConfidencePercent));

        if (success)
        {
            _sessionStateStore.ResetListeningAttempts(session.SessionId);
        }
    }

    public void TrackNoSpeechDetected(EvaluationSession session, string fieldId)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!IsVoice(session))
        {
            return;
        }

        var attemptNumber = ConsumeListeningAttempt(session);
        Emit(
            session,
            _payloads.Create(
                TelemetryConversationalPacingSignalTypes.NoInput,
                fieldId: fieldId,
                attemptNumber: attemptNumber));
    }

    public void TrackRepairAttempt(EvaluationSession session, string fieldId, string repairMode)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!IsVoice(session))
        {
            return;
        }

        Emit(
            session,
            _payloads.Create(
                TelemetryConversationalPacingSignalTypes.RepairAttempt,
                fieldId: fieldId,
                repairMode: repairMode));
    }

    public void TrackGlobalCommand(EvaluationSession session, string commandId)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!IsVoice(session))
        {
            return;
        }

        var signalType = commandId switch
        {
            HelpCommandId => TelemetryConversationalPacingSignalTypes.HelpRequest,
            RepeatCommandId => TelemetryConversationalPacingSignalTypes.RepeatRequest,
            _ => null,
        };

        if (signalType is null)
        {
            _sessionStateStore.ResetListeningAttempts(session.SessionId);
            return;
        }

        var attemptNumber = ConsumeListeningAttempt(session);
        Emit(
            session,
            _payloads.Create(
                signalType,
                commandId: commandId,
                attemptNumber: attemptNumber));
    }

    public void TrackCommandRecognitionFailure(EvaluationSession session, string failureReason, string? commandId)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!IsVoice(session))
        {
            return;
        }

        var attemptNumber = ConsumeListeningAttempt(session);
        Emit(
            session,
            _payloads.Create(
                TelemetryConversationalPacingSignalTypes.CommandRecognitionFailure,
                commandId: commandId,
                failureReason: failureReason,
                attemptNumber: attemptNumber));
    }

    public void TrackBargeIn(EvaluationSession session, string fieldId, string? interruptedPromptId)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!IsVoice(session))
        {
            return;
        }

        var attemptNumber = ConsumeListeningAttempt(session);
        Emit(
            session,
            _payloads.Create(
                TelemetryConversationalPacingSignalTypes.BargeIn,
                fieldId: fieldId,
                interruptedPromptId: interruptedPromptId,
                attemptNumber: attemptNumber));
    }

    private int? ConsumeListeningAttempt(EvaluationSession session)
    {
        if (_sessionStateStore.TryConsumeListeningAttempt(
                session.SessionId,
                out var attemptNumber))
        {
            return attemptNumber;
        }

        return null;
    }

    private void Emit(EvaluationSession session, object data)
    {
        if (IsStudyRun)
        {
            return;
        }

        _envelopeEmitter.Emit(session, TelemetryEventNames.ConversationalPacingSignal, data);
    }

    private bool IsStudyRun => !string.IsNullOrWhiteSpace(_studyTelemetryContext.ParticipantId);

    private static bool IsVoice(EvaluationSession session) => session.Channel == EvaluationChannel.Voice;
}
