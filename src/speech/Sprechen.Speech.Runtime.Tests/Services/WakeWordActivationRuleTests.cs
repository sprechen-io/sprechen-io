using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class WakeWordActivationRuleTests
{
    private static readonly WakeWordInventory VariantInventory = new(
        ["Okay"],
        ["Ok", "OK"]);

    [Fact]
    public void EvaluateLeadingWakeWord_WhenTranscriptIsBlank_ReturnsNoRecognizedSpeech()
    {
        var evaluation = WakeWordActivationRule.EvaluateLeadingWakeWord(
            "   ",
            90,
            new WakeWordInventory(["Okay"], []));

        Assert.Equal(WakeWordActivationStatus.NoRecognizedSpeech, evaluation.Status);
        Assert.False(evaluation.HasRecognizedSpeech);
        Assert.False(evaluation.IsActivated);
        Assert.Equal(string.Empty, evaluation.CommandText);
    }

    [Fact]
    public void EvaluateLeadingWakeWord_WhenConfidenceIsLow_StillUsesWakeWordParsing()
    {
        var evaluation = WakeWordActivationRule.EvaluateLeadingWakeWord(
            "Okay bitte Hilfe vorlesen",
            69,
            new WakeWordInventory(["Okay"], []));

        Assert.Equal(WakeWordActivationStatus.LeadingWakeWordDetected, evaluation.Status);
        Assert.True(evaluation.HasRecognizedSpeech);
        Assert.True(evaluation.IsActivated);
        Assert.Equal("Okay bitte Hilfe vorlesen", evaluation.TranscriptText);
        Assert.Equal("bitte hilfe vorlesen", evaluation.CommandText);
    }

    [Fact]
    public void EvaluateLeadingWakeWord_WhenLeadingWakeWordIsPresent_ReturnsActivatedCommandText()
    {
        var evaluation = WakeWordActivationRule.EvaluateLeadingWakeWord(
            "Okay bitte Hilfe vorlesen",
            90,
            new WakeWordInventory(["Okay"], []));

        Assert.Equal(WakeWordActivationStatus.LeadingWakeWordDetected, evaluation.Status);
        Assert.True(evaluation.HasRecognizedSpeech);
        Assert.True(evaluation.IsActivated);
        Assert.Equal("bitte hilfe vorlesen", evaluation.CommandText);
    }

    [Fact]
    public void EvaluateLeadingWakeWord_WhenWakeWordAppearsOnlyLater_ReturnsMissingLeadingWakeWord()
    {
        var evaluation = WakeWordActivationRule.EvaluateLeadingWakeWord(
            "ich sagte vorher okay",
            90,
            new WakeWordInventory(["Okay"], []));

        Assert.Equal(WakeWordActivationStatus.MissingLeadingWakeWord, evaluation.Status);
        Assert.True(evaluation.HasRecognizedSpeech);
        Assert.False(evaluation.IsActivated);
        Assert.Equal(string.Empty, evaluation.CommandText);
    }

    [Theory]
    [InlineData("Okay", "")]
    [InlineData("Ok", "")]
    [InlineData("OK Hilfe", "hilfe")]
    public void EvaluateLeadingWakeWord_WhenRecognitionOnlyOrCanonicalActivationMatches_ReturnsActivatedCommandText(
        string transcript,
        string expectedCommandText)
    {
        var evaluation = WakeWordActivationRule.EvaluateLeadingWakeWord(
            transcript,
            90,
            VariantInventory);

        Assert.Equal(WakeWordActivationStatus.LeadingWakeWordDetected, evaluation.Status);
        Assert.True(evaluation.IsActivated);
        Assert.Equal(expectedCommandText, evaluation.CommandText);
    }

    [Theory]
    [InlineData("Hallo")]
    [InlineData("Luma")]
    public void EvaluateLeadingWakeWord_WhenActivationFormsDoNotMatch_ReturnsMissingLeadingWakeWord(string transcript)
    {
        var evaluation = WakeWordActivationRule.EvaluateLeadingWakeWord(
            transcript,
            90,
            VariantInventory);

        Assert.Equal(WakeWordActivationStatus.MissingLeadingWakeWord, evaluation.Status);
        Assert.False(evaluation.IsActivated);
        Assert.Equal(string.Empty, evaluation.CommandText);
    }
}
