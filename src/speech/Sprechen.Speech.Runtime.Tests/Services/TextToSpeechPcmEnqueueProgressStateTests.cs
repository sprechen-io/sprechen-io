namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class TextToSpeechPcmEnqueueProgressStateTests
{
    [Fact]
    public void NewProgress_StartsNotStarted()
    {
        Assert.Equal(TextToSpeechPcmEnqueueState.NotStarted, new TextToSpeechPcmEnqueueProgressState().State);
    }

    [Fact]
    public void MarkEnqueueStarted_ThenMarkEnqueued_IsMonotonic()
    {
        var progress = new TextToSpeechPcmEnqueueProgressState();
        progress.MarkEnqueueStarted();
        Assert.Equal(TextToSpeechPcmEnqueueState.PossiblyEnqueued, progress.State);
        progress.MarkEnqueued();
        Assert.Equal(TextToSpeechPcmEnqueueState.Enqueued, progress.State);
        progress.MarkEnqueueStarted();
        progress.MarkEnqueued();
        Assert.Equal(TextToSpeechPcmEnqueueState.Enqueued, progress.State);
    }

    [Fact]
    public void MarkEnqueued_WithoutStart_LeavesNotStarted()
    {
        var progress = new TextToSpeechPcmEnqueueProgressState();
        progress.MarkEnqueued();
        Assert.Equal(TextToSpeechPcmEnqueueState.NotStarted, progress.State);
    }
}
