using Moq;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantCaptureDependenciesTests
{
    [Fact]
    public void WrapsRecognitionClientInDefaultWorker_WhenNoneProvided()
    {
        var capture = new SpeechAssistantCaptureDependencies(
            Mock.Of<ISpeechCaptureClient>(),
            Mock.Of<ISpeechRecognitionClient>());

        Assert.NotNull(capture.SpeechRecognitionWorker);
    }

    [Fact]
    public void UsesProvidedWorker_WhenSupplied()
    {
        var worker = new SpeechRecognitionWorker(Mock.Of<ISpeechRecognitionClient>());

        var capture = new SpeechAssistantCaptureDependencies(
            Mock.Of<ISpeechCaptureClient>(),
            Mock.Of<ISpeechRecognitionClient>(),
            worker);

        Assert.Same(worker, capture.SpeechRecognitionWorker);
    }

    [Fact]
    public void RoundTripsRequiredCollaborators()
    {
        var captureClient = Mock.Of<ISpeechCaptureClient>();
        var recognitionClient = Mock.Of<ISpeechRecognitionClient>();

        var capture = new SpeechAssistantCaptureDependencies(captureClient, recognitionClient);

        Assert.Same(captureClient, capture.SpeechCaptureClient);
        Assert.Same(recognitionClient, capture.SpeechRecognitionClient);
    }

    [Fact]
    public void RequiredCollaborators_NullThrows()
    {
        var captureClient = Mock.Of<ISpeechCaptureClient>();
        var recognitionClient = Mock.Of<ISpeechRecognitionClient>();

        Assert.Throws<ArgumentNullException>(() => new SpeechAssistantCaptureDependencies(null!, recognitionClient));
        Assert.Throws<ArgumentNullException>(() => new SpeechAssistantCaptureDependencies(captureClient, null!));
    }
}
