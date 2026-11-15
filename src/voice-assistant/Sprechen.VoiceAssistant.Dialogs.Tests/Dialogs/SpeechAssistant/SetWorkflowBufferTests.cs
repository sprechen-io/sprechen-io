using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SetWorkflowBufferTests
{
    [Fact]
    public void TryAdd_AddsEveryNewCode_AndReportsTheContribution()
    {
        var buffer = new SetWorkflowBuffer();

        var contributed = buffer.TryAdd(["email", "sms"], 80);

        Assert.True(contributed);
        Assert.True(buffer.HasContent);
        Assert.Equal("email|sms", buffer.GetFinalizedValue());
        Assert.Equal(80, buffer.GetSurvivingMinimumConfidencePercent());
    }

    [Fact]
    public void TryAdd_RenamingAnAlreadySelectedOption_LeavesSetAndConfidenceUntouched()
    {
        var buffer = new SetWorkflowBuffer();
        buffer.TryAdd(["email"], 90);

        var contributed = buffer.TryAdd(["email"], 20);

        Assert.False(contributed);
        Assert.Equal("email", buffer.GetFinalizedValue());
        Assert.Equal(90, buffer.GetSurvivingMinimumConfidencePercent());
    }

    [Fact]
    public void GetSurvivingMinimumConfidencePercent_IsTheMinimumOverContributingTurns()
    {
        var buffer = new SetWorkflowBuffer();
        buffer.TryAdd(["email"], 90);
        buffer.TryAdd(["sms"], 55);
        buffer.TryAdd(["phone"], 75);

        Assert.Equal(55, buffer.GetSurvivingMinimumConfidencePercent());
    }

    [Fact]
    public void GetSurvivingMinimumConfidencePercent_IgnoresContributionsWithoutAReportedConfidence()
    {
        var buffer = new SetWorkflowBuffer();
        buffer.TryAdd(["email"], null);

        Assert.Null(buffer.GetSurvivingMinimumConfidencePercent());

        buffer.TryAdd(["sms"], 60);

        Assert.Equal(60, buffer.GetSurvivingMinimumConfidencePercent());
    }

    [Fact]
    public void TryAdd_WithNoNewCode_IsATurnWithoutContribution()
    {
        var buffer = new SetWorkflowBuffer();
        buffer.TryAdd(["email", "sms"], 70);

        var contributed = buffer.TryAdd([], 10);

        Assert.False(contributed);
        Assert.Equal("email|sms", buffer.GetFinalizedValue());
        Assert.Equal(70, buffer.GetSurvivingMinimumConfidencePercent());
    }

    [Fact]
    public void GetFinalizedValue_IsTheCanonicalSortedWireForm()
    {
        var buffer = new SetWorkflowBuffer();
        buffer.TryAdd(["sms", "email"], 70);
        buffer.TryAdd(["phone"], 70);

        Assert.Equal("email|phone|sms", buffer.GetFinalizedValue());
    }

    [Fact]
    public void GetSpokenSelection_ReadsTheOptionsBackInSelectionOrder()
    {
        var buffer = new SetWorkflowBuffer();
        buffer.TryAdd(["sms", "email"], 70);
        var options = ImmutableArray.Create(
            new FormChoiceOption("email", "E-Mail"),
            new FormChoiceOption("sms", "SMS"));

        Assert.Equal("SMS, E-Mail", buffer.GetSpokenSelection(options));
    }
}
