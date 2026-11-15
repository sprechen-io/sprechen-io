namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Erfasst Auswertungsereignisse zur Formularnavigation und zum Formularstand:
/// Navigationsfehler, Hilfekommandos ohne Ziel sowie Status von Feldern und Feldgruppen.
/// </summary>
public interface IEvaluationFormStatusTelemetry
{
    void TrackNavigationError(EvaluationSession session, string errorCode, string scope);

    /// <summary>
    /// Zeichnet für Bedingung B auf, dass ein Sprachnavigationskommando wie <c>nextError</c>
    /// oder <c>nextOpenRequiredField</c> kein Ziel fand. Die <c>system_feedback</c>-Diagnose
    /// enthält die Kennung des Hilfekommandos.
    /// </summary>
    void TrackNavigationAidNoTarget(EvaluationSession session, string commandId);

    void TrackStatusSnapshot(EvaluationSession session, EvaluationFormStatusSnapshot snapshot);

    void TrackFieldAndGroupStatus(EvaluationSession session, EvaluationDetailedFormStatus snapshot);
}
