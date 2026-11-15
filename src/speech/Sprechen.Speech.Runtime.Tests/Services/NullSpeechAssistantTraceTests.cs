namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class NullSpeechAssistantTraceTests
{
    [Fact]
    public async Task WriteAsync_WhenCallerAlreadyCancelled_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await NullSpeechAssistantTrace.Instance.WriteAsync(
                "cid-123",
                SpeechAssistantTraceScope.SelectionDialogRuntime,
                SpeechAssistantTraceLevel.Debug,
                "selection started",
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }
}
