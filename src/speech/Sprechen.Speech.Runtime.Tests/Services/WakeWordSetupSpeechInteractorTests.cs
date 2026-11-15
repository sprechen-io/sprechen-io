using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class WakeWordSetupSpeechInteractorTests
{
    [Fact]
    public async Task PromptSpeechToTextAsync_WhenSpeechRecognitionQueueFull_ReturnsRecoverableFailureCode()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var recordingIndicator = new SynchronousClientMessageBus();
        var slowClient = new SlowSpeechRecognitionClient();
        await using var worker = new SpeechRecognitionWorker(
            slowClient,
            backoff: new RecordingBackoffPolicy(maxRetries: 1),
            maxQueueDepth: 1);
        var interactor = CreateInteractor(speechClient, worker, recordingIndicator);

        var firstTask = worker.EnqueueAsync(EmptyAudio(), "de", CancellationToken.None);
        await slowClient.WaitForFirstCallAsync(TimeSpan.FromSeconds(2));
        var secondTask = worker.EnqueueAsync(EmptyAudio(), "de", CancellationToken.None);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(EmptyAudio()));

        var result = await interactor.PromptSpeechToTextAsync("Bitte sprechen", "de", CancellationToken.None);

        Assert.Null(result.Transcript);
        Assert.Null(result.SttConfidencePercent);
        Assert.Equal(SpeechRecognitionQueueFullException.CodeValue, result.FailureCode);
        Assert.True(result.Recoverable);
        Assert.False(recordingIndicator.IsRecording);

        slowClient.ReleaseAll();
        await firstTask;
        await secondTask;
    }

    [Fact]
    public async Task PromptSpeechToTextAsync_WhenSpeechRecognitionBackoffExhausted_ReturnsRecoverableFailureCode()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var recordingIndicator = new SynchronousClientMessageBus();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(EmptyAudio()));
        await using var worker = new SpeechRecognitionWorker(
            new AlwaysFailingSpeechRecognitionClient(),
            backoff: new RecordingBackoffPolicy(maxRetries: 1),
            maxQueueDepth: 1);
        var interactor = CreateInteractor(speechClient, worker, recordingIndicator);

        var result = await interactor.PromptSpeechToTextAsync("Bitte sprechen", "de", CancellationToken.None);

        Assert.Null(result.Transcript);
        Assert.Null(result.SttConfidencePercent);
        Assert.Equal(SpeechRecognitionBackoffExhaustedException.CodeValue, result.FailureCode);
        Assert.True(result.Recoverable);
        Assert.False(recordingIndicator.IsRecording);
    }

    [Fact]
    public async Task PromptSpeechToTextAsync_WhenTheFarSideCancels_ReturnsRecoverableFailureRatherThanEscaping()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var recordingIndicator = new SynchronousClientMessageBus();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(EmptyAudio()));

        // Dieser Abbruch stammt vom Erkenner, nicht vom weiterhin gültigen Aufrufertoken.
        speechClient.EnqueueSpeechToTextFault(new OperationCanceledException("backend cancelled"));

        await using var worker = new SpeechRecognitionWorker(
            speechClient,
            backoff: new RecordingBackoffPolicy(maxRetries: 1),
            maxQueueDepth: 1);
        var interactor = CreateInteractor(speechClient, worker, recordingIndicator);

        var result = await interactor.PromptSpeechToTextAsync("Bitte sprechen", "de", CancellationToken.None);

        // Ein Erkennungsabbruch ist ein wiederholbarer Fehler und kein Nachweis fehlender Spracheingabe.
        Assert.Null(result.Transcript);
        Assert.Equal(SpeechCaptureFailureCodes.CaptureInterrupted, result.FailureCode);
        Assert.True(result.Recoverable);
        Assert.False(recordingIndicator.IsRecording);
    }

    private static WakeWordSetupSpeechInteractor CreateInteractor(
        ISpeechCaptureClient captureClient,
        SpeechRecognitionWorker speechRecognitionWorker,
        ISpeechCaptureRecordingIndicator recordingIndicator)
    {
        return new WakeWordSetupSpeechInteractor(
            captureClient,
            speechRecognitionWorker,
            new RecordingTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            recordingIndicator,
            NullLogger<WakeWordSetupSpeechInteractor>.Instance);
    }

    private static CapturedSpeechAudio EmptyAudio() =>
        new(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
}
