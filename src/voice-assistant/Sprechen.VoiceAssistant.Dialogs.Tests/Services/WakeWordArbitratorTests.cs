using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Services;

/// <summary>
/// Prüft die Ergebnisarten der Aktivierungsentscheidung
/// und die Ablehnung strukturell ungültiger Eingaben.
/// </summary>
public sealed class WakeWordArbitratorTests
{
    private const string CorrelationId = "corr-1";

    private static readonly WakeWordInventory Inventory = new(
        ["Okay"],
        ["Ok"]);

    private static readonly IWakeArbitrator Arbitrator = new WakeWordArbitrator();

    [Fact]
    public void Arbitrate_WhenAcousticEvidencePresent_ReturnsActivateWithAcousticSource()
    {
        var evidence = new AcousticWakeWordEvidence("detector-1", 0.9, default);

        var outcome = Arbitrator.Arbitrate(
            new WakeArbitrationInput(CorrelationId, Inventory)
            {
                AcousticEvidence = evidence,
            });

        var activate = Assert.IsType<WakeArbitrationOutcomeActivate>(outcome);
        Assert.Equal(WakeWordActivationSource.Acoustic, activate.Source);
        Assert.Same(evidence, activate.AcousticEvidence);
        Assert.Null(activate.LexicalEvaluation);
    }

    [Fact]
    public void Arbitrate_WhenLexicalEvaluationActivated_ReturnsActivateWithLexicalSource()
    {
        var evaluation = new WakeWordActivationEvaluation(
            WakeWordActivationStatus.LeadingWakeWordDetected,
            "Okay help",
            "help",
            ConfidencePercent: 90);

        var outcome = Arbitrator.Arbitrate(
            new WakeArbitrationInput(CorrelationId, Inventory)
            {
                LexicalEvaluation = evaluation,
            });

        var activate = Assert.IsType<WakeArbitrationOutcomeActivate>(outcome);
        Assert.Equal(WakeWordActivationSource.Lexical, activate.Source);
        Assert.Same(evaluation, activate.LexicalEvaluation);
        Assert.Null(activate.AcousticEvidence);
    }

    [Fact]
    public void Arbitrate_WhenLexicalNotActivatedAndStitchAttached_ReturnsStitch()
    {
        var nonActivation = new WakeWordActivationEvaluation(
            WakeWordActivationStatus.MissingLeadingWakeWord,
            "help",
            string.Empty,
            ConfidencePercent: 70);

        var stitchedEvaluation = new WakeWordActivationEvaluation(
            WakeWordActivationStatus.LeadingWakeWordDetected,
            "Okay help",
            "help",
            ConfidencePercent: 70);

        var stitch = new WakeStitchAttachedResult("Okay help", "help", stitchedEvaluation);

        var outcome = Arbitrator.Arbitrate(
            new WakeArbitrationInput(CorrelationId, Inventory)
            {
                LexicalEvaluation = nonActivation,
                StitchResult = stitch,
            });

        var stitchOutcome = Assert.IsType<WakeArbitrationOutcomeStitch>(outcome);
        Assert.Equal("Okay help", stitchOutcome.StitchedTranscriptText);
        Assert.Equal("help", stitchOutcome.CommandText);
        Assert.Same(stitchedEvaluation, stitchOutcome.Evaluation);
    }

    [Fact]
    public void Arbitrate_WhenLexicalNotActivatedAndStitchStarted_ReturnsMissWithStitchStarted()
    {
        var nonActivation = new WakeWordActivationEvaluation(
            WakeWordActivationStatus.MissingLeadingWakeWord,
            "Okay",
            string.Empty,
            ConfidencePercent: null);

        var outcome = Arbitrator.Arbitrate(
            new WakeArbitrationInput(CorrelationId, Inventory)
            {
                LexicalEvaluation = nonActivation,
                StitchResult = new WakeStitchStartedResult("Okay"),
            });

        var miss = Assert.IsType<WakeArbitrationOutcomeMiss>(outcome);
        Assert.True(miss.StitchStarted);
        Assert.Equal(WakeWordStitchPhase.Started, miss.StitchPhase);
        Assert.Equal("Okay".Length, miss.StitchTranscriptLength);
        Assert.Equal("Okay", miss.StitchStartedTranscriptText);
        Assert.Same(nonActivation, miss.Evaluation);
    }

    [Fact]
    public void Arbitrate_WhenLexicalNotActivatedAndNoOpStitch_ReturnsMissWithNoOpPhase()
    {
        var nonActivation = new WakeWordActivationEvaluation(
            WakeWordActivationStatus.NoRecognizedSpeech,
            string.Empty,
            string.Empty,
            ConfidencePercent: null);

        var outcome = Arbitrator.Arbitrate(
            new WakeArbitrationInput(CorrelationId, Inventory)
            {
                LexicalEvaluation = nonActivation,
                StitchResult = WakeStitchNoOpResult.Instance,
            });

        var miss = Assert.IsType<WakeArbitrationOutcomeMiss>(outcome);
        Assert.False(miss.StitchStarted);
        Assert.Equal(WakeWordStitchPhase.NoOp, miss.StitchPhase);
        Assert.Equal(0, miss.StitchTranscriptLength);
        Assert.Null(miss.StitchStartedTranscriptText);
    }

    [Fact]
    public void Arbitrate_WhenLexicalNotActivatedAndStitchExpired_ReturnsMissWithExpiredPhase()
    {
        var nonActivation = new WakeWordActivationEvaluation(
            WakeWordActivationStatus.MissingLeadingWakeWord,
            "help",
            string.Empty,
            ConfidencePercent: 50);

        var outcome = Arbitrator.Arbitrate(
            new WakeArbitrationInput(CorrelationId, Inventory)
            {
                LexicalEvaluation = nonActivation,
                StitchResult = new WakeStitchExpiredResult("Okay"),
            });

        var miss = Assert.IsType<WakeArbitrationOutcomeMiss>(outcome);
        Assert.False(miss.StitchStarted);
        Assert.Equal(WakeWordStitchPhase.Expired, miss.StitchPhase);
        Assert.Equal("Okay".Length, miss.StitchTranscriptLength);
        Assert.Null(miss.StitchStartedTranscriptText);
    }

    [Fact]
    public void Arbitrate_WhenFallbackReasonProvided_ReturnsLexicalFallback()
    {
        var outcome = Arbitrator.Arbitrate(
            new WakeArbitrationInput(CorrelationId, Inventory)
            {
                FallbackReason = LexicalWakeFallbackReason.DetectorWarmup,
            });

        var fallback = Assert.IsType<WakeArbitrationOutcomeLexicalFallback>(outcome);
        Assert.Equal(LexicalWakeFallbackReason.DetectorWarmup, fallback.FallbackReason);
    }

    [Fact]
    public void Arbitrate_WhenInputIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Arbitrator.Arbitrate(null!));
    }

    [Fact]
    public void Arbitrate_WhenCorrelationIdBlank_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Arbitrator.Arbitrate(
            new WakeArbitrationInput("   ", Inventory)));
    }

    [Fact]
    public void Arbitrate_WhenFallbackCombinedWithDetectionInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Arbitrator.Arbitrate(
            new WakeArbitrationInput(CorrelationId, Inventory)
            {
                FallbackReason = LexicalWakeFallbackReason.DetectorFailure,
                AcousticEvidence = new AcousticWakeWordEvidence("d", 0.5, default),
            }));
    }

    [Fact]
    public void Arbitrate_WhenAcousticAndLexicalBothPresent_ThrowsArgumentException()
    {
        var evidence = new AcousticWakeWordEvidence("detector", 0.5, default);
        var evaluation = new WakeWordActivationEvaluation(
            WakeWordActivationStatus.LeadingWakeWordDetected,
            "Okay",
            string.Empty,
            ConfidencePercent: 80);

        Assert.Throws<ArgumentException>(() => Arbitrator.Arbitrate(
            new WakeArbitrationInput(CorrelationId, Inventory)
            {
                AcousticEvidence = evidence,
                LexicalEvaluation = evaluation,
            }));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void Arbitrate_WhenAcousticConfidenceOutOfRange_ThrowsArgumentException(double score)
    {
        Assert.Throws<ArgumentException>(() => Arbitrator.Arbitrate(
            new WakeArbitrationInput(CorrelationId, Inventory)
            {
                AcousticEvidence = new AcousticWakeWordEvidence("d", score, default),
            }));
    }

    [Fact]
    public void Arbitrate_WhenNoDecisionInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Arbitrator.Arbitrate(
            new WakeArbitrationInput(CorrelationId, Inventory)));
    }
}
