using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Services;

public sealed class WakeArbitrationRouteClassifierTests
{
    private const string CorrelationId = "corr-1";

    private static readonly WakeWordInventory Inventory = new(
        ["Okay"],
        ["Ok"]);

    private readonly WakeArbitrationInputShapeReader _shapeReader = new();
    private readonly WakeArbitrationRouteClassifier _sut = new();

    [Fact]
    public void Classify_WithFallback_ReturnsLexicalFallback()
    {
        var input = new WakeArbitrationInput(CorrelationId, Inventory)
        {
            FallbackReason = LexicalWakeFallbackReason.DetectorWarmup,
        };

        var route = _sut.Classify(input, _shapeReader.Read(input));

        Assert.Equal(WakeArbitrationRoute.LexicalFallback, route);
    }

    [Fact]
    public void Classify_WithAcousticEvidence_ReturnsAcousticActivation()
    {
        var input = new WakeArbitrationInput(CorrelationId, Inventory)
        {
            AcousticEvidence = new AcousticWakeWordEvidence("detector", 0.8, default),
        };

        var route = _sut.Classify(input, _shapeReader.Read(input));

        Assert.Equal(WakeArbitrationRoute.AcousticActivation, route);
    }

    [Fact]
    public void Classify_WithActivatedLexicalEvaluation_ReturnsLexicalActivation()
    {
        var input = new WakeArbitrationInput(CorrelationId, Inventory)
        {
            LexicalEvaluation = new WakeWordActivationEvaluation(
                WakeWordActivationStatus.LeadingWakeWordDetected,
                "Okay help",
                "help",
                ConfidencePercent: 90),
        };

        var route = _sut.Classify(input, _shapeReader.Read(input));

        Assert.Equal(WakeArbitrationRoute.LexicalActivation, route);
    }

    [Fact]
    public void Classify_WithNonActivatedLexicalEvaluation_ReturnsLexicalMissOrStitch()
    {
        var input = new WakeArbitrationInput(CorrelationId, Inventory)
        {
            LexicalEvaluation = new WakeWordActivationEvaluation(
                WakeWordActivationStatus.NoRecognizedSpeech,
                string.Empty,
                string.Empty,
                ConfidencePercent: null),
        };

        var route = _sut.Classify(input, _shapeReader.Read(input));

        Assert.Equal(WakeArbitrationRoute.LexicalMissOrStitch, route);
    }
}
