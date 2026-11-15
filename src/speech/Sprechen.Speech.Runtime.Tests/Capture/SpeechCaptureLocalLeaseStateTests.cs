namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class SpeechCaptureLocalLeaseStateTests
{
    [Fact]
    public void ReleaseAndIsIdle_WhenLastLeaseIsReleased_ReturnsTrue()
    {
        var state = new SpeechCaptureLocalLeaseState();
        var generation = state.Acquire();

        Assert.True(state.ReleaseAndIsIdle(generation));
        Assert.True(state.IsIdle);
    }

    [Fact]
    public void ReleaseAndIsIdle_WhenNoLeaseExists_IsNoOp()
    {
        var state = new SpeechCaptureLocalLeaseState();

        Assert.False(state.ReleaseAndIsIdle(generation: 0));
        Assert.True(state.IsIdle);
    }

    [Fact]
    public void Clear_RemovesAllActiveLeases()
    {
        var state = new SpeechCaptureLocalLeaseState();

        state.Acquire();
        state.Acquire();
        state.Clear();

        Assert.True(state.IsIdle);
    }

    [Fact]
    public void ReleaseAndIsIdle_FromClearedGeneration_DoesNotReleaseCurrentLease()
    {
        var state = new SpeechCaptureLocalLeaseState();
        var staleGeneration = state.Acquire();
        state.Clear();
        var currentGeneration = state.Acquire();

        Assert.False(state.ReleaseAndIsIdle(staleGeneration));
        Assert.False(state.IsIdle);
        Assert.True(state.ReleaseAndIsIdle(currentGeneration));
    }
}
