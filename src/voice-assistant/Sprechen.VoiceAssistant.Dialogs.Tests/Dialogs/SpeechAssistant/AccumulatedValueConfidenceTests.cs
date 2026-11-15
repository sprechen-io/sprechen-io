namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Konfidenz eines zusammengesetzten Werts: das Minimum der gemeldeten
/// Konfidenzen oder unbekannt, wenn keine Konfidenz gemeldet wurde.
/// </summary>
public sealed class AccumulatedValueConfidenceTests
{
    [Fact]
    public void SurvivingMinimumPercent_IsTheMinimumOfTheReportedConfidences()
    {
        var confidence = new AccumulatedValueConfidence();

        Assert.Equal(40, confidence.SurvivingMinimumPercent([90, null, 40, 75]));
    }

    [Fact]
    public void SurvivingMinimumPercent_IsUnknown_WhenNoContributionReportedAConfidence()
    {
        var confidence = new AccumulatedValueConfidence();

        Assert.Null(confidence.SurvivingMinimumPercent([null, null]));
        Assert.Null(confidence.SurvivingMinimumPercent([]));
    }

    [Fact]
    public void SurvivingMinimumPercent_RejectsAMissingContributionSequence()
    {
        var confidence = new AccumulatedValueConfidence();

        Assert.Throws<ArgumentNullException>(() => confidence.SurvivingMinimumPercent(null!));
    }
}
