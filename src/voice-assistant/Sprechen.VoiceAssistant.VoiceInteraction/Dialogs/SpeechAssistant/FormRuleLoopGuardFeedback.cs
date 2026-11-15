using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert die Fehlerrückmeldung bei überschrittener Regeliterationsgrenze an
/// und meldet den Laufzeitfehler als Navigationstelemetrie ohne Feldwerte.
/// Auf den asynchronen Abschluss der Fehlerrückmeldung wartet dieser Adapter nicht.
/// </summary>
internal sealed class FormRuleLoopGuardFeedback : IFormRuleLoopGuardFeedback
{
    private readonly IEvaluationFormStatusTelemetry _evaluationTelemetry;
    private readonly ISpeechCaptureFeedback _speechCaptureFeedback;
    private readonly EvaluationSession _session;

    public FormRuleLoopGuardFeedback(
        IEvaluationFormStatusTelemetry evaluationTelemetry,
        ISpeechCaptureFeedback speechCaptureFeedback,
        EvaluationSession session)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechCaptureFeedback = speechCaptureFeedback ?? throw new ArgumentNullException(nameof(speechCaptureFeedback));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public void OnRuleLoopGuardBreach()
    {
        // Die Fehlerrückmeldung vor dem Telemetrieaufruf anfordern,
        // ohne auf ihren asynchronen Abschluss zu warten.
        _ = _speechCaptureFeedback.ReportFailureAsync(
            SpeechErrorCode.FormRuleLoopGuardExceeded,
            RuntimeErrorCodes.RuleLoopMaxIterationsExceeded);

        try
        {
            _evaluationTelemetry.TrackNavigationError(_session, NavigationErrorCodes.LostContext, ConstraintScopes.CrossGroup);
        }
        catch
        {
            // Ausnahmen dieses Telemetrieaufrufs nicht an die Regelverarbeitung weitergeben.
        }
    }
}
