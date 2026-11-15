using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Services;

public sealed class WakeArbitrationInputGuardTests
{
    private const string CorrelationId = "corr-1";

    private static readonly WakeWordInventory Inventory = new(
        ["Okay"],
        ["Ok"]);

    private readonly WakeArbitrationInputGuard _sut = new();

    [Fact]
    public void Validate_WithValidAcousticInput_ReturnsShape()
    {
        var shape = _sut.Validate(new WakeArbitrationInput(CorrelationId, Inventory)
        {
            AcousticEvidence = new AcousticWakeWordEvidence("detector", 0.8, default),
        });

        Assert.True(shape.HasAcoustic);
        Assert.False(shape.HasFallback);
        Assert.False(shape.HasLexical);
        Assert.False(shape.HasStitch);
    }

    [Fact]
    public void Validate_WithFallbackAndDetectionInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _sut.Validate(new WakeArbitrationInput(CorrelationId, Inventory)
        {
            FallbackReason = LexicalWakeFallbackReason.DetectorFailure,
            StitchResult = WakeStitchNoOpResult.Instance,
        }));
    }

    [Fact]
    public void Validate_WithNoDecisionInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _sut.Validate(new WakeArbitrationInput(CorrelationId, Inventory)));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void Validate_WithAcousticConfidenceOutOfRange_ThrowsArgumentException(double score)
    {
        Assert.Throws<ArgumentException>(() => _sut.Validate(new WakeArbitrationInput(CorrelationId, Inventory)
        {
            AcousticEvidence = new AcousticWakeWordEvidence("detector", score, default),
        }));
    }
}
