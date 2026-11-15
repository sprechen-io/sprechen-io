using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt das typisierte Ergebnis des Zusammensetzens in eine Aktivierungsentscheidung oder einen
/// Fehlversuch mit der passenden Phase und Textlänge.
/// </summary>
internal sealed class WakeStitchResultArbitrationVisitor : IWakeStitchResultVisitor<WakeArbitrationOutcome>
{
    private readonly WakeWordActivationEvaluation _evaluation;

    public WakeStitchResultArbitrationVisitor(WakeWordActivationEvaluation evaluation)
    {
        _evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
    }

    public WakeArbitrationOutcome VisitAttached(WakeStitchAttachedResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new WakeArbitrationOutcomeStitch(
            result.StitchedTranscriptText,
            result.CommandText,
            result.Evaluation);
    }

    public WakeArbitrationOutcome VisitStarted(WakeStitchStartedResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new WakeArbitrationOutcomeMiss(
            _evaluation,
            WakeWordStitchPhase.Started,
            result.TranscriptText.Length,
            result.TranscriptText);
    }

    public WakeArbitrationOutcome VisitExpired(WakeStitchExpiredResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new WakeArbitrationOutcomeMiss(
            _evaluation,
            WakeWordStitchPhase.Expired,
            result.PreviousTranscriptText.Length,
            stitchStartedTranscriptText: null);
    }

    public WakeArbitrationOutcome VisitNoOp(WakeStitchNoOpResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new WakeArbitrationOutcomeMiss(
            _evaluation,
            WakeWordStitchPhase.NoOp,
            (_evaluation.TranscriptText ?? string.Empty).Length,
            stitchStartedTranscriptText: null);
    }
}
