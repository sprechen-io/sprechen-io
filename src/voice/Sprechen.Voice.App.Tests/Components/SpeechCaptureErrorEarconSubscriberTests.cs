using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Web.SpeechComponents;
using Sprechen.Voice.App.Tests.TestDoubles;
using Xunit;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class SpeechCaptureErrorEarconSubscriberTests
{
    [Fact]
    public async Task OnFailedAsync_PlaysErrorEarcon()
    {
        using var ctx = new TestContext();

        var messageBusLogger = new RecordingLogger<ClientMessageBus>();
        var messageBus = new ClientMessageBus(messageBusLogger);
        ctx.Services.AddSpeechInteractionSignals(messageBus);

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.PlayAsync(EarconId.ValidationAsrFail, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);
        ctx.Services.AddSingleton(earconPlayer.Object);

        ctx.Services.AddSingleton<ILogger<SpeechCaptureErrorEarconSubscriber>>(
            NullLogger<SpeechCaptureErrorEarconSubscriber>.Instance);

        using var cut = ctx.Render<SpeechCaptureErrorEarconSubscriber>();

        await messageBus.PublishAsync(new SpeechCaptureFailed("boom"));

        earconPlayer.VerifyAll();
    }

    [Fact]
    public async Task OnFailedAsync_WhenEarconFails_IsLoggedAndDoesNotThrow()
    {
        using var ctx = new TestContext();

        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        ctx.Services.AddSpeechInteractionSignals(messageBus);

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.PlayAsync(EarconId.ValidationAsrFail, CancellationToken.None))
            .Throws(new InvalidOperationException("boom"));
        ctx.Services.AddSingleton(earconPlayer.Object);

        var logger = new RecordingLogger<SpeechCaptureErrorEarconSubscriber>();
        ctx.Services.AddSingleton<ILogger<SpeechCaptureErrorEarconSubscriber>>(logger);

        using var cut = ctx.Render<SpeechCaptureErrorEarconSubscriber>();

        await messageBus.PublishAsync(new SpeechCaptureFailed("boom"));

        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Debug
                     && entry.Message.Contains("Failed to play error earcon.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Dispose_UnsubscribesFromMessageBus()
    {
        using var ctx = new TestContext();

        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        ctx.Services.AddSpeechInteractionSignals(messageBus);

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.PlayAsync(EarconId.ValidationAsrFail, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);
        ctx.Services.AddSingleton(earconPlayer.Object);

        ctx.Services.AddSingleton<ILogger<SpeechCaptureErrorEarconSubscriber>>(
            NullLogger<SpeechCaptureErrorEarconSubscriber>.Instance);

        var cut = ctx.Render<SpeechCaptureErrorEarconSubscriber>();

        await messageBus.PublishAsync(new SpeechCaptureFailed("first"));
        cut.Instance.Dispose();
        await messageBus.PublishAsync(new SpeechCaptureFailed("second"));

        earconPlayer.Verify(player => player.PlayAsync(EarconId.ValidationAsrFail, CancellationToken.None), Times.Once);
    }
}


