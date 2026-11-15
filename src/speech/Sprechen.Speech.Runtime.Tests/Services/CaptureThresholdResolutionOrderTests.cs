using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class CaptureThresholdResolutionOrderTests
{
    private const string ThresholdStep = "threshold";
    private const string MicGateStep = "micgate";

    [Fact]
    public async Task ThresholdIsResolved_BeforeTheAttemptGateAndTheMicGate()
    {
        var steps = new ConcurrentQueue<string>();
        var micGateOpen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var coordinator = CreateCoordinator(steps, micGateOpen);
        using var cancellation = new CancellationTokenSource();

        var firstCapture = StartListeningCapture(coordinator, cancellation.Token);
        await WaitForStepCountAsync(steps, MicGateStep, expected: 1);

        // Der zweite Aufruf haengt am Aufnahmesemaphor, das der erste haelt. Seine Schwelle
        // ist trotzdem schon aufgeloest - genau das belegt die Reihenfolge.
        var secondCapture = StartListeningCapture(coordinator, cancellation.Token);
        await WaitForStepCountAsync(steps, ThresholdStep, expected: 2);

        Assert.Equal([ThresholdStep, MicGateStep, ThresholdStep], steps.ToArray());

        await cancellation.CancelAsync();
        micGateOpen.TrySetCanceled();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Task.WhenAll(firstCapture, secondCapture).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    private static Task<CapturedSpeechAudio> StartListeningCapture(
        SpeechCaptureActiveCaptureCoordinator coordinator,
        CancellationToken cancellationToken)
        => coordinator.CaptureAsync(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening),
            initialSilenceTimeout: null,
            cancellationToken);

    private static async Task WaitForStepCountAsync(
        ConcurrentQueue<string> steps,
        string step,
        int expected)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);
        while (steps.Count(recorded => recorded == step) < expected)
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException($"Observed steps: {string.Join(", ", steps)}");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }
    }

    private static SpeechCaptureActiveCaptureCoordinator CreateCoordinator(
        ConcurrentQueue<string> steps,
        TaskCompletionSource micGateOpen)
    {
        var amplitudeThresholdSettings = new Mock<ISpeechCaptureAmplitudeThresholdSettings>();
        amplitudeThresholdSettings
            .Setup(settings => settings.GetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                steps.Enqueue(ThresholdStep);
                return ValueTask.FromResult(350);
            });

        var micGateState = new Mock<IMicGateState>();
        micGateState
            .Setup(gate => gate.WaitUntilOpenAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken cancellationToken) =>
            {
                steps.Enqueue(MicGateStep);
                return new ValueTask(micGateOpen.Task.WaitAsync(cancellationToken));
            });

        var optionsFactory = new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(new SpeechCaptureClientOptions()),
            Options.Create(new PcmSpeechCaptureSegmenterOptions()));

        return new SpeechCaptureActiveCaptureCoordinator(
            new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict).Object,
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            new InMemoryBrowserDebugConsole(),
            new SpeechCaptureSegmenterFactory(optionsFactory),
            new InMemorySpeechSampleRing(maxLookbackMs: 5_000),
            micGateState.Object,
            speechAmplitudeThresholdResolver: new SpeechCaptureSpeechAmplitudeThresholdResolver(
                amplitudeThresholdSettings.Object));
    }
}
