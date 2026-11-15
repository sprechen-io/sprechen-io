using Microsoft.Extensions.Logging;
using Moq;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class BrowserDebugConsoleSpeechAssistantTraceTests
{
    [Fact]
    public async Task WriteAsync_WhenCalled_WritesStructuredMessageToBrowserAndLogger()
    {
        var browserDebugConsole = new Mock<IBrowserDebugConsole>(MockBehavior.Strict);
        browserDebugConsole
            .Setup(console => console.WriteAsync(
                "speech-assistant",
                "cid=cid-123 level=info scope=FormsCatalogEffect application catalog started",
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var logger = new CapturingLogger<BrowserDebugConsoleSpeechAssistantTrace>();

        var trace = new BrowserDebugConsoleSpeechAssistantTrace(browserDebugConsole.Object, logger);

        await trace.WriteAsync(
            "cid-123",
            SpeechAssistantTraceScope.FormsCatalogEffect,
            SpeechAssistantTraceLevel.Information,
            "application catalog started",
            CancellationToken.None);

        browserDebugConsole.VerifyAll();
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("cid-123", entry.Values["CorrelationId"]);
        Assert.Equal(SpeechAssistantTraceScope.FormsCatalogEffect, entry.Values["Scope"]);
        Assert.Equal("application catalog started", entry.Values["Message"]);
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

        var trace = new BrowserDebugConsoleSpeechAssistantTrace(
            browserDebugConsole.Object,
            new CapturingLogger<BrowserDebugConsoleSpeechAssistantTrace>());

        await trace.WriteAsync(
            "cid-123",
            SpeechAssistantTraceScope.WakeWordEffect,
            SpeechAssistantTraceLevel.Warning,
            "wake word detection failed",
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

        var trace = new BrowserDebugConsoleSpeechAssistantTrace(
            browserDebugConsole.Object,
            new CapturingLogger<BrowserDebugConsoleSpeechAssistantTrace>());

        await trace.WriteAsync(
            "cid-123",
            SpeechAssistantTraceScope.WakeWordEffect,
            SpeechAssistantTraceLevel.Warning,
            "wake word detection failed",
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
            .Returns((string _, string _, CancellationToken cancellationToken) =>
            {
                cancellation.Cancel();
                return ValueTask.CompletedTask;
            });

        var trace = new BrowserDebugConsoleSpeechAssistantTrace(
            browserDebugConsole.Object,
            new CapturingLogger<BrowserDebugConsoleSpeechAssistantTrace>());

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await trace.WriteAsync(
                "cid-123",
                SpeechAssistantTraceScope.WakeWordEffect,
                SpeechAssistantTraceLevel.Warning,
                "wake word detection cancelled",
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        browserDebugConsole.VerifyAll();
    }

    [Fact]
    public async Task WriteAsync_WhenLoggerThrows_StillWritesToBrowserAndDoesNotPropagate()
    {
        var browserDebugConsole = new Mock<IBrowserDebugConsole>(MockBehavior.Strict);
        browserDebugConsole
            .Setup(console => console.WriteAsync(
                "speech-assistant",
                "cid=cid-123 level=error scope=SelectionDialogRuntime selection failed",
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var trace = new BrowserDebugConsoleSpeechAssistantTrace(
            browserDebugConsole.Object,
            new ThrowingLogger<BrowserDebugConsoleSpeechAssistantTrace>(
                new InvalidOperationException("logger failed")));

        await trace.WriteAsync(
            "cid-123",
            SpeechAssistantTraceScope.SelectionDialogRuntime,
            SpeechAssistantTraceLevel.Error,
            "selection failed",
            CancellationToken.None);

        browserDebugConsole.VerifyAll();
    }

    [Fact]
    public async Task WriteAsync_WhenLoggerObservesCallerCancellation_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var browserDebugConsole = new Mock<IBrowserDebugConsole>(MockBehavior.Strict);
        var trace = new BrowserDebugConsoleSpeechAssistantTrace(
            browserDebugConsole.Object,
            new CancellingLogger<BrowserDebugConsoleSpeechAssistantTrace>(cancellation));

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await trace.WriteAsync(
                "cid-123",
                SpeechAssistantTraceScope.SelectionDialogRuntime,
                SpeechAssistantTraceLevel.Warning,
                "selection cancelled",
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        browserDebugConsole.VerifyNoOtherCalls();
    }

}
