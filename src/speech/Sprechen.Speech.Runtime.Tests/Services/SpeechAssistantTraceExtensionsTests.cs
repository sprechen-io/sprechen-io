namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechAssistantTraceExtensionsTests
{
    [Fact]
    public async Task WriteIfConfiguredAsync_WhenTraceIsNull_RemainsNoOp()
    {
        ISpeechAssistantTrace? trace = null;

        await trace.WriteIfConfiguredAsync(
            "cid-123",
            SpeechAssistantTraceScope.SelectionDialogRuntime,
            SpeechAssistantTraceLevel.Debug,
            "selection started",
            CancellationToken.None);

        Assert.Null(trace);
    }

    [Fact]
    public async Task WriteIfConfiguredAsync_WhenTraceIsNullAndCallerAlreadyCancelled_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        ISpeechAssistantTrace? trace = null;

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await trace.WriteIfConfiguredAsync(
                "cid-123",
                SpeechAssistantTraceScope.SelectionDialogRuntime,
                SpeechAssistantTraceLevel.Debug,
                "selection started",
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Theory]
    [InlineData(null, "selection started")]
    [InlineData("", "selection started")]
    [InlineData("   ", "selection started")]
    [InlineData("cid-123", null)]
    [InlineData("cid-123", "")]
    [InlineData("cid-123", "   ")]
    public async Task WriteIfConfiguredAsync_WhenInputIsInvalid_RemainsNoOp(
        string? correlationId,
        string? message)
    {
        var trace = new DelegatingSpeechAssistantTrace(_ => ValueTask.CompletedTask);

        await trace.WriteIfConfiguredAsync(
            correlationId,
            SpeechAssistantTraceScope.SelectionDialogRuntime,
            SpeechAssistantTraceLevel.Debug,
            message!,
            CancellationToken.None);

        Assert.Equal(0, trace.WriteCount);
    }

    [Fact]
    public async Task WriteIfConfiguredAsync_WhenInputIsInvalidAndCallerAlreadyCancelled_PropagatesBeforeNoOp()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var trace = new DelegatingSpeechAssistantTrace(_ => ValueTask.CompletedTask);

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await trace.WriteIfConfiguredAsync(
                " ",
                SpeechAssistantTraceScope.SelectionDialogRuntime,
                SpeechAssistantTraceLevel.Debug,
                "selection started",
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, trace.WriteCount);
    }

    [Fact]
    public async Task WriteIfConfiguredAsync_WhenTraceThrowsSynchronously_IsolatesFailure()
    {
        var trace = new DelegatingSpeechAssistantTrace(
            _ => throw new InvalidOperationException("trace failed"));

        await trace.WriteIfConfiguredAsync(
            "cid-123",
            SpeechAssistantTraceScope.SelectionDialogRuntime,
            SpeechAssistantTraceLevel.Warning,
            "selection failed",
            CancellationToken.None);

        Assert.Equal(1, trace.WriteCount);
    }

    [Fact]
    public async Task WriteIfConfiguredAsync_WhenTraceFaultsAsynchronously_IsolatesFailure()
    {
        var trace = new DelegatingSpeechAssistantTrace(
            _ => new ValueTask(Task.FromException(new InvalidOperationException("trace failed"))));

        await trace.WriteIfConfiguredAsync(
            "cid-123",
            SpeechAssistantTraceScope.SelectionDialogRuntime,
            SpeechAssistantTraceLevel.Warning,
            "selection failed",
            CancellationToken.None);

        Assert.Equal(1, trace.WriteCount);
    }

    [Fact]
    public async Task WriteIfConfiguredAsync_WhenTraceCancelsWithoutCallerCancellation_IsolatesFailure()
    {
        using var sinkCancellation = new CancellationTokenSource();
        sinkCancellation.Cancel();
        var trace = new DelegatingSpeechAssistantTrace(
            _ => new ValueTask(Task.FromCanceled(sinkCancellation.Token)));

        await trace.WriteIfConfiguredAsync(
            "cid-123",
            SpeechAssistantTraceScope.SelectionDialogRuntime,
            SpeechAssistantTraceLevel.Warning,
            "selection failed",
            CancellationToken.None);

        Assert.Equal(1, trace.WriteCount);
    }

    [Fact]
    public async Task WriteIfConfiguredAsync_WhenSinkCancelsCallerButCompletesSuccessfully_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var trace = new DelegatingSpeechAssistantTrace(
            cancellationToken =>
            {
                cancellation.Cancel();
                return ValueTask.CompletedTask;
            });

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await trace.WriteIfConfiguredAsync(
                "cid-123",
                SpeechAssistantTraceScope.SelectionDialogRuntime,
                SpeechAssistantTraceLevel.Warning,
                "selection cancelled",
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(1, trace.WriteCount);
    }

}
