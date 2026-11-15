using Moq;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechRecognitionWorkItemTerminalFlowTests
{
    [Fact]
    public async Task ProcessAsync_WhenRequestAlreadyCancelled_CancelsWithoutInvokingClient()
    {
        var client = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var terminalFlow = new SpeechRecognitionWorkItemTerminalFlow(
            client.Object,
            new RecordingBackoffPolicy(maxRetries: 3));
        using var requestCts = new CancellationTokenSource();
        await requestCts.CancelAsync();

        var item = CreateWorkItem(requestCts.Token);

        await terminalFlow.ProcessAsync(item, CancellationToken.None);

        await Assert.ThrowsAsync<TaskCanceledException>(() => item.Tcs.Task);
        client.Verify(
            owner => owner.SpeechToTextAsync(
                It.IsAny<CapturedSpeechAudio>(),
                It.IsAny<string?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenNoSpeechDetected_DeliversDomainExceptionWithoutRetry()
    {
        var client = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        client
            .Setup(owner => owner.SpeechToTextAsync(
                It.IsAny<CapturedSpeechAudio>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NoSpeechDetectedException());
        var policy = new RecordingBackoffPolicy(maxRetries: 3);
        var terminalFlow = new SpeechRecognitionWorkItemTerminalFlow(client.Object, policy);

        var item = CreateWorkItem(CancellationToken.None);

        await terminalFlow.ProcessAsync(item, CancellationToken.None);

        await Assert.ThrowsAsync<NoSpeechDetectedException>(() => item.Tcs.Task);
        Assert.Empty(policy.RequestedAttemptIndices);
        client.Verify(
            owner => owner.SpeechToTextAsync(
                It.IsAny<CapturedSpeechAudio>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenFailuresExhaustRetries_DeliversBackoffExhausted()
    {
        var client = new AlwaysFailingSpeechRecognitionClient();
        var policy = new RecordingBackoffPolicy(maxRetries: 2);
        var terminalFlow = new SpeechRecognitionWorkItemTerminalFlow(client, policy);

        var item = CreateWorkItem(CancellationToken.None);

        await terminalFlow.ProcessAsync(item, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<SpeechRecognitionBackoffExhaustedException>(() => item.Tcs.Task);
        Assert.Equal(2, ex.MaxRetries);
        Assert.Equal(2, client.CallCount);
        Assert.Equal([0], policy.RequestedAttemptIndices);
    }

    private static SpeechRecognitionWorkItem CreateWorkItem(CancellationToken cancellationToken)
    {
        return new SpeechRecognitionWorkItem(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]),
            "de",
            RecognitionProfile.Conversational,
            Keyterms: null,
            cancellationToken,
            new TaskCompletionSource<SpeechTranscript>(TaskCreationOptions.RunContinuationsAsynchronously));
    }
}
