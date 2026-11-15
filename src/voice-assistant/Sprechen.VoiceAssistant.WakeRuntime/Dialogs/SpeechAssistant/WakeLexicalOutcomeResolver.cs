using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.Runtime.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet lexikalische Aktivierungsprüfung und das Zusammensetzen benachbarter Äußerungen mit der
/// zentralen Entscheidung und der Rückmeldung bei ausbleibender Aktivierung.
/// </summary>
internal sealed class WakeLexicalOutcomeResolver
{
    private readonly IWakeArbitrator _arbitrator;
    private readonly WakeWordMissOutcomeHandler _missOutcomeHandler;
    private readonly WakeUtteranceStitcher _wakeUtteranceStitcher = new();

    public WakeLexicalOutcomeResolver(
        IWakeArbitrator arbitrator,
        WakeWordMissOutcomeHandler missOutcomeHandler)
    {
        _arbitrator = arbitrator ?? throw new ArgumentNullException(nameof(arbitrator));
        _missOutcomeHandler = missOutcomeHandler ?? throw new ArgumentNullException(nameof(missOutcomeHandler));
    }

    internal async ValueTask<(WakeWordActivationEvaluation? Activation, bool ContinueListening)> ResolveAsync(
        WakeRuntimeContext context,
        IWakeRuntimeOutcomeSink outcomeSink,
        IWakeWordTelemetrySink telemetry,
        string correlationId,
        WakeWordInventory wakeWordInventory,
        SpeechTranscript transcript,
        DateTimeOffset capturedAt,
        CancellationToken cancellationToken)
    {
        var lexicalEvaluation = WakeWordActivationRule.EvaluateLeadingWakeWord(transcript.Text, transcript.ConfidencePercent, wakeWordInventory);
        var stitchResult = lexicalEvaluation.IsActivated
            ? null
            : _wakeUtteranceStitcher.AttachOrStart(
                transcript,
                capturedAt,
                wakeWordInventory,
                wakeWordInventory.WakeUtteranceWindow);

        var lexicalOutcome = _arbitrator.Arbitrate(
            new WakeArbitrationInput(correlationId, wakeWordInventory)
            {
                LexicalEvaluation = lexicalEvaluation,
                StitchResult = stitchResult,
            });

        switch (lexicalOutcome)
        {
            case WakeArbitrationOutcomeActivate activateLexical
                when activateLexical.Source == WakeWordActivationSource.Lexical
                    && activateLexical.LexicalEvaluation is not null:
                _wakeUtteranceStitcher.Reset();
                return (activateLexical.LexicalEvaluation, ContinueListening: false);
            case WakeArbitrationOutcomeStitch stitch:
                await telemetry
                    .RecordStitchAsync(
                        new WakeWordStitchEvent(
                            correlationId,
                            WakeWordStitchPhase.Attached,
                            TranscriptTextLength: 0,
                            stitch.StitchedTranscriptText.Length,
                            stitch.CommandText.Length),
                        cancellationToken)
                    .ConfigureAwait(false);
                return (stitch.Evaluation, ContinueListening: false);
            case WakeArbitrationOutcomeMiss miss:
                var continueListening = await _missOutcomeHandler.HandleAsync(
                        context,
                        outcomeSink,
                        telemetry,
                        correlationId,
                        wakeWordInventory,
                        miss,
                        cancellationToken)
                    .ConfigureAwait(false);
                return (Activation: null, continueListening);
            default:
                throw new InvalidOperationException(
                    $"WakeWordArbitrator returned an unexpected outcome '{lexicalOutcome.GetType().Name}' for lexical-cycle input.");
        }
    }

    internal void Reset()
        => _wakeUtteranceStitcher.Reset();
}
