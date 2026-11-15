namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Erfasst Auswertungsereignisse für erkannte Kommandos, Erkennungsfehler,
/// aufgelöste Mehrdeutigkeiten und bestätigte oder abgelehnte destruktive Kommandos.
/// </summary>
public interface IEvaluationCommandTelemetry
{
    void TrackGlobalCommand(EvaluationSession session, string commandId);

    void TrackCommandRecognitionFailure(EvaluationSession session, string failureReason, string? commandId);

    void TrackDestructiveCommandResolved(
        EvaluationSession session,
        string commandId,
        bool confirmed,
        bool refractoryBlocked = false);

    void TrackAmbiguityResolved(
        EvaluationSession session,
        string ambiguityType,
        string resolutionStrategy,
        string signalClass,
        string result,
        string? fieldId,
        string? commandId);
}
