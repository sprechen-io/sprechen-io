namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Erfasst sitzungsweite Auswertungsereignisse zu Lebenszyklus, Zustandswechseln und Unterbrechungen.
/// </summary>
public interface IEvaluationSessionTelemetry
{
    Task<EvaluationSession> StartSessionAsync(
        string applicationId,
        string formId,
        string requestedLanguage,
        EvaluationChannel channel,
        CancellationToken cancellationToken);

    Task<EvaluationSessionAcceptance?> EndSessionAsync(
        EvaluationSession session,
        EvaluationOutcome outcome,
        EvaluationTaskCompletion completion,
        CancellationToken cancellationToken);

    Task CommitSessionAsync(
        EvaluationSessionAcceptance acceptance,
        CancellationToken cancellationToken);

    void TrackStateEntry(EvaluationSession session, string state);

    void TrackBargeIn(EvaluationSession session, string? interruptedPromptId = null);

    /// <summary>
    /// Zeichnet einen Spracheingabeversuch ohne verwertbares Transkript auf,
    /// solange noch keine Formularsitzung existiert, etwa während der Einstiegsfragen.
    /// </summary>
    void TrackNoSpeechDetected();
}
