namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Erfasst Auswertungsereignisse zu Feldfokus, Eingabeversuchen, fehlender Sprache,
/// Bedingungsverstößen, Reparaturversuchen und Wechseln des Eingabemodus.
/// </summary>
public interface IEvaluationFieldTelemetry
{
    void TrackFieldFocus(EvaluationSession session, string fieldId);

    void TrackFieldAttempt(EvaluationSession session, string fieldId, string inputMode, bool success, int? sttConfidencePercent = null);

    void TrackNoSpeechDetected(EvaluationSession session, string fieldId);

    /// <summary>
    /// Zeichnet einen Bedingungsverstoß auf. <paramref name="trigger"/> benennt die auslösende
    /// Handlung. Der Aufrufer unterscheidet damit einen beim Laden erfassten Zustand
    /// von der Prüfung einer gerade eingegebenen Antwort.
    /// </summary>
    void TrackConstraintViolation(EvaluationSession session, string fieldId, string errorCode, string severity, string trigger);

    void TrackConstraintViolation(EvaluationSession session, string fieldId, string errorCode, string severity, string scope, string? fieldGroupId, string trigger);

    void TrackRepairAttempt(EvaluationSession session, string fieldId, string repairMode);

    void TrackInputModeChanged(
        EvaluationSession session,
        string fieldId,
        string fromMode,
        string toMode,
        string trigger);
}
