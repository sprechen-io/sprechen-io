using Microsoft.Extensions.Logging;
using Moq;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class BrowserDebugConsoleSpeechClientTraceTests
{
    [Fact]
    public async Task WriteAsync_WhenCorrelationIdIsPresent_WritesStructuredMessageToBrowserAndLogger()
    {
        var browserDebugConsole = new Mock<IBrowserDebugConsole>(MockBehavior.Strict);
        browserDebugConsole
            .Setup(console => console.WriteAsync(
                "speech-conversation",
                "cid=cid-123 speech-to-conversation-stream started",
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var logger = new CapturingLogger<BrowserDebugConsoleSpeechClientTrace>();
        var trace = new BrowserDebugConsoleSpeechClientTrace(browserDebugConsole.Object, logger);

        await trace.WriteAsync(
            "speech-conversation",
            LogLevel.Information,
            "cid-123",
            "speech-to-conversation-stream started",
            CancellationToken.None);

        browserDebugConsole.VerifyAll();
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("speech-conversation", entry.Values["Scope"]);
        Assert.Equal("cid-123", entry.Values["CorrelationId"]);
        Assert.Equal("speech-to-conversation-stream started", entry.Values["Message"]);
    }

    [Fact]
    public async Task WriteAsync_WhenBrowserConsoleThrows_DoesNotPropagate()
    {
        var browserDebugConsole = new Mock<IBrowserDebugConsole>(MockBehavior.Strict);
        browserDebugConsole
            .Setup(console => console.WriteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("boom"));

        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole.Object,
            new CapturingLogger<BrowserDebugConsoleSpeechClientTrace>());

        await trace.WriteAsync(
            "speech-conversation",
            LogLevel.Warning,
            "cid-123",
            "speech-to-conversation-stream failed",
            CancellationToken.None);

        browserDebugConsole.VerifyAll();
    }

    [Fact]
    public async Task WriteAsync_WhenBrowserConsoleCancelsWithoutCallerCancellation_DoesNotPropagate()
    {
        using var sinkCancellation = new CancellationTokenSource();
        sinkCancellation.Cancel();
        var browserDebugConsole = new Mock<IBrowserDebugConsole>(MockBehavior.Strict);
        browserDebugConsole
            .Setup(console => console.WriteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask(Task.FromCanceled(sinkCancellation.Token)));

        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole.Object,
            new CapturingLogger<BrowserDebugConsoleSpeechClientTrace>());

        await trace.WriteAsync(
            "speech-conversation",
            LogLevel.Warning,
            "cid-123",
            "speech-to-conversation-stream failed",
            CancellationToken.None);

        browserDebugConsole.VerifyAll();
    }

    [Fact]
    public async Task WriteAsync_WhenLoggerThrows_StillWritesToBrowserAndDoesNotPropagate()
    {
        var browserDebugConsole = new Mock<IBrowserDebugConsole>(MockBehavior.Strict);
        browserDebugConsole
            .Setup(console => console.WriteAsync(
                "speech-conversation",
                "cid=cid-123 speech-to-conversation-stream failed",
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole.Object,
            new ThrowingLogger<BrowserDebugConsoleSpeechClientTrace>(
                new InvalidOperationException("logger failed")));

        await trace.WriteAsync(
            "speech-conversation",
            LogLevel.Error,
            "cid-123",
            "speech-to-conversation-stream failed",
            CancellationToken.None);

        browserDebugConsole.VerifyAll();
    }

    [Fact]
    public async Task WriteAsync_WhenBrowserConsoleCancelsCallerButCompletesSuccessfully_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var browserDebugConsole = new Mock<IBrowserDebugConsole>(MockBehavior.Strict);
        browserDebugConsole
            .Setup(console => console.WriteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                cancellation.Token))
            .Returns((string _, string _, CancellationToken _) =>
            {
                cancellation.Cancel();
                return ValueTask.CompletedTask;
            });

        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole.Object,
            new CapturingLogger<BrowserDebugConsoleSpeechClientTrace>());

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await trace.WriteAsync(
                "speech-conversation",
                LogLevel.Warning,
                "cid-123",
                "speech-to-conversation-stream cancelled",
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        browserDebugConsole.VerifyAll();
    }

    [Fact]
    public async Task WriteAsync_WhenLoggerCancelsCaller_PropagatesBeforeBrowserWrite()
    {
        using var cancellation = new CancellationTokenSource();
        var browserDebugConsole = new Mock<IBrowserDebugConsole>(MockBehavior.Strict);
        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole.Object,
            new CancellingLogger<BrowserDebugConsoleSpeechClientTrace>(cancellation));

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await trace.WriteAsync(
                "speech-conversation",
                LogLevel.Warning,
                "cid-123",
                "speech-to-conversation-stream cancelled",
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        browserDebugConsole.VerifyNoOtherCalls();
    }

}
