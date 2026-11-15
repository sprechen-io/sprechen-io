using Microsoft.Extensions.Logging.Abstractions;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class CaptureAttemptModeSourceTests
{
    [Theory]
    [InlineData(SpeechCaptureMode.Listening)]
    [InlineData(SpeechCaptureMode.ListeningContinuation)]
    [InlineData(SpeechCaptureMode.ActivationListening)]
    [InlineData(SpeechCaptureMode.ActivationListeningContinuation)]
    [InlineData(SpeechCaptureMode.ShortConfirmation)]
    [InlineData(SpeechCaptureMode.PromptInterruption)]
    [InlineData(SpeechCaptureMode.PromptInterruptionDetection)]
    public async Task SingleTurnCapture_ReportsTheModeItWasAskedFor(SpeechCaptureMode mode)
    {
        var reporter = new CapturingReporter();
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        using var coordinator = ActiveCaptureCoordinatorTestFactory.Create(
            browserDebugConsole,
            reporter,
            TestMicGateInfrastructure.NoOpState());
        using var cancellation = new CancellationTokenSource();

        var capture = coordinator.CaptureAsync(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            SpeechCaptureModeBehavior.For(mode),
            initialSilenceTimeout: null,
            cancellation.Token);

        await browserDebugConsole.WaitForEntryAsync(
            $"speech-capture: capture armed mode={mode} startContext=CleanListening",
            TimeSpan.FromSeconds(5));
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => capture.WaitAsync(TimeSpan.FromSeconds(5)));

        var report = Assert.Single(reporter.Reports);
        Assert.Equal(mode.ToString(), report.CaptureMode);
    }

    [Theory]
    [InlineData(SpeechCaptureMode.Listening)]
    [InlineData(SpeechCaptureMode.ShortConfirmation)]
    [InlineData(SpeechCaptureMode.PromptInterruption)]
    public async Task TraceStart_NamesTheModeOfTheBehaviorItWasGiven(SpeechCaptureMode mode)
    {
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var trace = new ActiveCaptureTrace(browserDebugConsole, NullLogger.Instance);

        await trace.RecordStartAsync(
            silenceDebounceMs: 900,
            SpeechCaptureStartContext.CleanListening,
            SpeechCaptureModeBehavior.For(mode));

        Assert.Contains(
            $"speech-capture: record start mode={mode} silenceDebounceMs=900 startContext=CleanListening bargeInEnabled=",
            browserDebugConsole.Entries.Single(),
            StringComparison.Ordinal);
    }
}
