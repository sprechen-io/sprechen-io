using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Erstellt und versendet Befehlsereignisse. Hilfe, Wiederholung und Erkennungsfehler werden zusätzlich an die Gesprächsdiagnostik gemeldet.
/// </summary>
internal sealed class EvaluationCommandTelemetry(
    EvaluationTelemetryEnvelopeEmitter envelopeEmitter,
    EvaluationCommandTelemetryPayloads payloads,
    EvaluationConversationalPacingTelemetry pacingTelemetry) : IEvaluationCommandTelemetry
{
    private readonly EvaluationTelemetryEnvelopeEmitter _envelopeEmitter =
        envelopeEmitter ?? throw new ArgumentNullException(nameof(envelopeEmitter));
    private readonly EvaluationConversationalPacingTelemetry _pacingTelemetry =
        pacingTelemetry ?? throw new ArgumentNullException(nameof(pacingTelemetry));
    private readonly EvaluationCommandTelemetryPayloads _payloads =
        payloads ?? throw new ArgumentNullException(nameof(payloads));

    public void TrackGlobalCommand(EvaluationSession session, string commandId)
    {
        ArgumentNullException.ThrowIfNull(session);

        Emit(
            session,
            TelemetryEventNames.IntentRecognized,
            _payloads.CreateGlobalCommand(commandId));
        _pacingTelemetry.TrackGlobalCommand(session, commandId);
    }

    public void TrackCommandRecognitionFailure(EvaluationSession session, string failureReason, string? commandId)
    {
        ArgumentNullException.ThrowIfNull(session);

        Emit(
            session,
            TelemetryEventNames.CommandRecognitionFailed,
            _payloads.CreateCommandRecognitionFailure(failureReason, commandId));
        _pacingTelemetry.TrackCommandRecognitionFailure(session, failureReason, commandId);
    }

    public void TrackDestructiveCommandResolved(
        EvaluationSession session,
        string commandId,
        bool confirmed,
        bool refractoryBlocked = false)
    {
        ArgumentNullException.ThrowIfNull(session);

        Emit(
            session,
            TelemetryEventNames.DestructiveCommandResolved,
            _payloads.CreateDestructiveCommandResolved(
                commandId,
                confirmed,
                refractoryBlocked));
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
        ArgumentNullException.ThrowIfNull(session);

        Emit(
            session,
            TelemetryEventNames.AmbiguityResolved,
            _payloads.CreateAmbiguityResolved(
                ambiguityType,
                resolutionStrategy,
                signalClass,
                result,
                fieldId,
                commandId));
    }

    private void Emit(EvaluationSession session, string eventName, object? data)
    {
        _envelopeEmitter.Emit(session, eventName, data);
    }
}
