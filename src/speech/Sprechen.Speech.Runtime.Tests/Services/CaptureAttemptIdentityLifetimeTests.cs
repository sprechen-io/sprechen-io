namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft am Aufnahmekoordinator, dass ein vor dem Scharfstellen abgebrochener Versuch
/// seinen eigenen Modus und Startkontext meldet und keine Laufzeit des vorherigen Versuchs übernimmt.
/// </summary>
public sealed class CaptureAttemptIdentityLifetimeTests
{
    [Fact]
    public async Task AFaultBeforeArming_CarriesTheModeOfThisAttempt_NotThePreviousOne()
    {
        var reporter = new CapturingReporter();
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var micGate = new StubMicGateState(isHardOff: false);
        using var coordinator = ActiveCaptureCoordinatorTestFactory.Create(browserDebugConsole, reporter, micGate);

        using (var armedAttempt = new CancellationTokenSource())
        {
            var capture = coordinator.CaptureAsync(
                SpeechAudioMimeType.Pcm,
                silenceDebounceMs: 20,
                SpeechCaptureStartContext.CleanListening,
                SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening),
                initialSilenceTimeout: null,
                armedAttempt.Token);

            await browserDebugConsole.WaitForEntryAsync(
                "speech-capture: capture armed mode=Listening startContext=CleanListening",
                TimeSpan.FromSeconds(5));
            await armedAttempt.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => capture.WaitAsync(TimeSpan.FromSeconds(5)));
        }

        micGate.IsHardOff = true;

        using (var gatedAttempt = new CancellationTokenSource())
        {
            var gatedCapture = coordinator.CaptureAsync(
                SpeechAudioMimeType.Pcm,
                silenceDebounceMs: 20,
                SpeechCaptureStartContext.AssistantPlaybackActive,
                SpeechCaptureModeBehavior.For(SpeechCaptureMode.ShortConfirmation),
                initialSilenceTimeout: null,
                gatedAttempt.Token);

            await browserDebugConsole.WaitForEntryAsync(
                "speech-capture: record start mode=ShortConfirmation silenceDebounceMs=20 startContext=AssistantPlaybackActive bargeInEnabled=False",
                TimeSpan.FromSeconds(5));
            await gatedAttempt.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gatedCapture.WaitAsync(TimeSpan.FromSeconds(5)));
        }

        Assert.Equal(2, reporter.Reports.Count);
        var armed = reporter.Reports[0];
        Assert.Equal("Listening", armed.CaptureMode);
        Assert.True(armed.ArmedToOutcomeMs > 0);

        var gated = reporter.Reports[1];
        Assert.Equal("ShortConfirmation", gated.CaptureMode);
        Assert.Equal("AssistantPlaybackActive", gated.StartContext);
        Assert.Equal(0, gated.ArmedToOutcomeMs);
    }
}
