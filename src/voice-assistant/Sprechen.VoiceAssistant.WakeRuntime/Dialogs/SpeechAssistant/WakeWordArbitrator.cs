using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet geprüfte Eingaben ohne eigenen veränderlichen Zustand einer
/// Aktivierungsentscheidung zu. Ein Ersatzgrund wählt die textbasierte
/// Erkennung. Akustische Ergebnisse oder aktivierende Textauswertungen
/// liefern eine Aktivierung. Sonst wird eine mögliche Segmentzusammenführung
/// ausgewertet. Widersprüchliche oder fehlende Eingaben werden zurückgewiesen.
/// </summary>
internal sealed class WakeWordArbitrator : IWakeArbitrator
{
    private readonly WakeArbitrationInputGuard _inputGuard;
    private readonly WakeArbitrationRouteClassifier _routeClassifier;

    public WakeWordArbitrator()
        : this(
            new WakeArbitrationInputGuard(),
            new WakeArbitrationRouteClassifier())
    {
    }

    internal WakeWordArbitrator(
        WakeArbitrationInputGuard inputGuard,
        WakeArbitrationRouteClassifier routeClassifier)
    {
        _inputGuard = inputGuard ?? throw new ArgumentNullException(nameof(inputGuard));
        _routeClassifier = routeClassifier ?? throw new ArgumentNullException(nameof(routeClassifier));
    }

    public WakeArbitrationOutcome Arbitrate(WakeArbitrationInput input)
    {
        var shape = _inputGuard.Validate(input);
        return _routeClassifier.Classify(input, shape) switch
        {
            WakeArbitrationRoute.LexicalFallback => new WakeArbitrationOutcomeLexicalFallback(input.FallbackReason),
            WakeArbitrationRoute.AcousticActivation => BuildAcousticActivation(input.AcousticEvidence!),
            WakeArbitrationRoute.LexicalActivation => BuildLexicalActivation(input.LexicalEvaluation!),
            WakeArbitrationRoute.LexicalMissOrStitch => BuildLexicalMissOrStitch(input.LexicalEvaluation!, input.StitchResult),
            _ => throw new InvalidOperationException("Unsupported wake arbitration route."),
        };
    }

    private static WakeArbitrationOutcomeActivate BuildAcousticActivation(AcousticWakeWordEvidence evidence)
    {
        return new WakeArbitrationOutcomeActivate(
            WakeWordActivationSource.Acoustic,
            evidence,
            lexicalEvaluation: null);
    }

    private static WakeArbitrationOutcomeActivate BuildLexicalActivation(WakeWordActivationEvaluation evaluation)
    {
        return new WakeArbitrationOutcomeActivate(
            WakeWordActivationSource.Lexical,
            acousticEvidence: null,
            evaluation);
    }

    private static WakeArbitrationOutcome BuildLexicalMissOrStitch(
        WakeWordActivationEvaluation evaluation,
        WakeStitchResult? stitchResult)
    {
        return stitchResult is null
            ? new WakeArbitrationOutcomeMiss(evaluation, stitchStartedTranscriptText: null)
            : stitchResult.Accept(new WakeStitchResultArbitrationVisitor(evaluation));
    }
}
