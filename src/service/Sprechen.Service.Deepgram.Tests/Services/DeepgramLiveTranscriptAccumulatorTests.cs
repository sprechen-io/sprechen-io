using Sprechen.Service.Deepgram.Services;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class DeepgramLiveTranscriptAccumulatorTests
{
    [Fact]
    public void Apply_WhenInterimRepeats_DoesNotEmitDuplicate()
    {
        var accumulator = new DeepgramLiveTranscriptAccumulator();

        var first = accumulator.Apply("hello", confidencePercent: 80, isFinal: false);
        var duplicate = accumulator.Apply("hello", confidencePercent: 80, isFinal: false);

        Assert.NotNull(first);
        Assert.Equal("hello", first.Text);
        Assert.Equal(80, first.ConfidencePercent);
        Assert.Null(duplicate);
    }

    [Fact]
    public void Apply_WhenFinalAndNewInterimArrive_ComposesCommittedAndInterimText()
    {
        var accumulator = new DeepgramLiveTranscriptAccumulator();

        var committed = accumulator.Apply("hello", confidencePercent: 90, isFinal: true);
        var combined = accumulator.Apply("world", confidencePercent: 70, isFinal: false);

        Assert.NotNull(committed);
        Assert.Equal("hello", committed.Text);
        Assert.NotNull(combined);
        Assert.Equal("hello world", combined.Text);
        Assert.Equal(70, combined.ConfidencePercent);
    }
}
