namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SelectionDialogConfirmationHandlerTests
{
    [Fact]
    public async Task ParseAsync_WhenParserReturnsTypedResult_PreservesCompleteResult()
    {
        var parsed = new SelectionDialogCommonIntentParseResult(
            SelectionDialogCommonIntent.SelectByName,
            TargetName: "selected",
            Number: 3);
        string? observedTranscript = null;
        var handler = new SelectionDialogConfirmationHandler<string>(transcript =>
        {
            observedTranscript = transcript;
            return parsed;
        });

        var result = await handler.ParseAsync("open selected", CancellationToken.None);

        Assert.Same(parsed, result);
        Assert.Equal(SelectionDialogCommonIntent.SelectByName, result.Intent);
        Assert.Equal("selected", result.TargetName);
        Assert.Equal(3, result.Number);
        Assert.Equal("open selected", observedTranscript);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ParseAsync_WhenTranscriptIsMissing_FailsFastBeforeParser(string? transcript)
    {
        var parseCalls = 0;
        var handler = new SelectionDialogConfirmationHandler<string>(_ =>
        {
            parseCalls++;
            return new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.Unknown);
        });

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => handler.ParseAsync(transcript!, CancellationToken.None));
        Assert.Equal(0, parseCalls);
    }

    [Fact]
    public async Task ParseAsync_WhenCallerTokenIsCancelled_PropagatesBeforeParser()
    {
        using var callerCts = new CancellationTokenSource();
        callerCts.Cancel();
        var parseCalls = 0;
        var handler = new SelectionDialogConfirmationHandler<string>(_ =>
        {
            parseCalls++;
            return new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.ConfirmYes);
        });

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.ParseAsync("yes", callerCts.Token));
        Assert.Equal(callerCts.Token, exception.CancellationToken);
        Assert.Equal(0, parseCalls);
    }

    [Fact]
    public async Task ParseAsync_WhenParserReturnsNull_FailsFast()
    {
        var handler = new SelectionDialogConfirmationHandler<string>(_ => null!);

        var exception = await Assert.ThrowsAsync<SelectionCandidateStateException>(
            () => handler.ParseAsync("yes", CancellationToken.None));
        Assert.Contains("returned no result", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WhenParserIsMissing_FailsFast()
    {
        Assert.Throws<ArgumentNullException>(() => new SelectionDialogConfirmationHandler<string>(null!));
    }
}
