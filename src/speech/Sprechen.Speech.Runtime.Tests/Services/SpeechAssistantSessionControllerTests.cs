using Moq;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechAssistantSessionControllerTests
{
    [Fact]
    public async Task StopAsync_WhenStopSucceeds_ResetsActivationState()
    {
        var speechAssistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var speechCaptureFeedback = new Mock<ISpeechCaptureFeedback>(MockBehavior.Strict);
        speechAssistant
            .Setup(service => service.StopAsync(CancellationToken.None))
            .Returns(ValueTask.CompletedTask);
        speechCaptureFeedback
            .Setup(feedback => feedback.SetActivationStateAsync(SpeechAssistantActivationState.NotStarted))
            .Returns(ValueTask.CompletedTask);

        var controller = new SpeechAssistantSessionController(
            speechAssistant.Object,
            speechCaptureFeedback.Object);

        await controller.StopAsync(CancellationToken.None);

        speechAssistant.VerifyAll();
        speechCaptureFeedback.VerifyAll();
    }

    [Fact]
    public async Task StopAsync_WhenStopThrows_StillResetsActivationState_AndRethrows()
    {
        var speechAssistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var speechCaptureFeedback = new Mock<ISpeechCaptureFeedback>(MockBehavior.Strict);
        speechAssistant
            .Setup(service => service.StopAsync(CancellationToken.None))
            .Returns(() => new ValueTask(Task.FromException(new InvalidOperationException("boom"))));
        speechCaptureFeedback
            .Setup(feedback => feedback.SetActivationStateAsync(SpeechAssistantActivationState.NotStarted))
            .Returns(ValueTask.CompletedTask);

        var controller = new SpeechAssistantSessionController(
            speechAssistant.Object,
            speechCaptureFeedback.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await controller.StopAsync(CancellationToken.None));

        Assert.Equal("boom", exception.Message);
        speechAssistant.VerifyAll();
        speechCaptureFeedback.VerifyAll();
    }
}
