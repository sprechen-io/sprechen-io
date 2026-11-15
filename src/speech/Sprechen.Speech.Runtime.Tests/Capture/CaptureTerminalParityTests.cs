using System.Buffers.Binary;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.Runtime.Tests.Services;

namespace Sprechen.Speech.Runtime.Tests.Capture;

/// <summary>
/// Prüft beide Aufnahmekoordinatoren mit denselben Abschlussereignissen gegen
/// eine erwartete Diagnosespur. Zusätzliche Prüfungen sichern, dass die
/// Fensterbeobachtungen vor der Entsorgung ausgelesen werden.
/// </summary>
public sealed class CaptureTerminalParityTests
{
    private const int SampleRateHz = InMemorySpeechSampleRing.SampleRateHz;
    private const int BytesPerSample = InMemorySpeechSampleRing.BytesPerSample;

    private const string StartTrace =
        "speech-capture: record start mode=ActivationListening silenceDebounceMs=20 "
        + "startContext=CleanListening bargeInEnabled=False";

    private const string ArmedTrace =
        "speech-capture: capture armed mode=ActivationListening startContext=CleanListening";

    private static readonly TimeSpan PatientLivenessWindow = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TerminalWait = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(CaptureTerminalProbe.NoSpeech, "speech-capture: record no-speech-detected")]
    [InlineData(CaptureTerminalProbe.Failure, "speech-capture: record failed InvalidOperationException: probe")]
    [InlineData(CaptureTerminalProbe.Cancellation, "speech-capture: record canceled by caller")]
    public async Task BothCapturePaths_OnTheSameTerminalInput_WriteTheSameTraceLine(
        CaptureTerminalProbe probe,
        string expectedTerminalTrace)
    {
        var singleAttempt = await RunSingleAttemptAsync(probe);
        var segment = await RunSegmentAsync(probe);

        // Beide Seiten gegen dieselbe erwartete Folge, nicht nur gegeneinander: zwei gleich
        // abweichende Wege wären sonst weiterhin gleich.
        var expected = new[] { StartTrace, ArmedTrace, expectedTerminalTrace };

        Assert.Equal(expected, singleAttempt.Entries);
        Assert.Equal(expected, segment.Entries);
        Assert.Equal(singleAttempt.TerminalKind, segment.TerminalKind);
    }

    [Fact]
    public async Task SegmentCapture_WhenTheStreamStalls_WritesAFailureTrace()
    {
        // Auch ein ausgebliebener Audiostrom muss eine Fehlermeldung in der Diagnosespur erzeugen.
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        using var coordinator = CreateSegmentCoordinator(
            browserDebugConsole,
            TimeSpan.FromMilliseconds(200),
            out _);
        coordinator.ResetToLiveEdge();

        var capture = StartSegmentCapture(coordinator, CancellationToken.None);
        var terminal = await ObserveTerminalAsync(capture);

        Assert.Equal(nameof(SpeechCaptureStreamStalledException), terminal);
        Assert.Contains(
            "speech-capture: record failed SpeechCaptureStreamStalledException: ",
            browserDebugConsole.Entries.Last(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void CaptureTerminalFlow_ReadsTheWindowObservationsBeforeDisposingIt()
    {
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        using var terminalFlow = new CaptureTerminalFlow(
            new ActiveCaptureTrace(browserDebugConsole, NullLogger<CaptureTerminalParityTests>.Instance));
        var window = new RecordingArmedCaptureWindow();
        var cleared = 0;

        terminalFlow.Release(window, _ => cleared++);

        Assert.Equal(
            new[] { "MaxAmplitudeObserved", "ObservedFrameCount", "EstimatedFloor", "AppliedOnsetThreshold", "Dispose" },
            window.Calls);
        Assert.Equal(1, cleared);
    }

    [Fact]
    public async Task CaptureTerminalFlow_RejectsNullBodyWindowAndClearCallback()
    {
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        using var terminalFlow = new CaptureTerminalFlow(
            new ActiveCaptureTrace(browserDebugConsole, NullLogger<CaptureTerminalParityTests>.Instance));

        Assert.Throws<ArgumentNullException>(() => new CaptureTerminalFlow(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => terminalFlow.RunAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await terminalFlow.CompleteAsync(null!, new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [])));
        Assert.Throws<ArgumentNullException>(
            () => terminalFlow.Release(new RecordingArmedCaptureWindow(), null!));
    }

    [Fact]
    public async Task SingleAttemptCapture_OnNoSpeech_ReportsThePeakOfItsWindow()
    {
        // Der Spitzenpegel bleibt im Bericht nur erhalten, wenn das Fenster vor der Entsorgung gelesen wird.
        var reporter = new CapturingReporter();
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        using var coordinator = CreateSingleAttemptCoordinator(browserDebugConsole, reporter, out var ring);

        var capture = StartSingleAttemptCapture(coordinator, CancellationToken.None);
        await browserDebugConsole.WaitForEntryAsync(ArmedTrace, TerminalWait);

        for (var i = 0; i < 8; i++)
        {
            var chunk = CreatePcmChunk(20, sampleValue: 1000);
            ring.WriteSamples(chunk);
            await coordinator.ProcessChunkAsync(chunk, CancellationToken.None);
        }

        coordinator.FailActiveCapture(new NoSpeechDetectedException());
        Assert.Equal(nameof(NoSpeechDetectedException), await ObserveTerminalAsync(capture));

        var report = Assert.Single(reporter.Reports);
        Assert.Equal("no_speech", report.Outcome);
        Assert.True(report.PeakAmplitude > 0, "Expected the report to carry the peak of its own window.");
        Assert.NotNull(report.Window);
        Assert.True(report.Window.ObservedFrames > 0, "Expected the report to carry the frames of its own window.");
    }

    [Fact]
    public void BothSlotTypes_AreArmedCaptureWindows()
    {
        Assert.True(typeof(IArmedCaptureWindow).IsAssignableFrom(typeof(ActiveCaptureSlot)));
        Assert.True(typeof(IArmedCaptureWindow).IsAssignableFrom(typeof(ActiveSegmentSlot)));
    }

    private static async Task<CaptureTerminalObservation> RunSingleAttemptAsync(CaptureTerminalProbe probe)
    {
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        using var coordinator = CreateSingleAttemptCoordinator(browserDebugConsole, new CapturingReporter(), out _);
        using var cancellation = new CancellationTokenSource();

        var capture = StartSingleAttemptCapture(coordinator, cancellation.Token);
        await browserDebugConsole.WaitForEntryAsync(ArmedTrace, TerminalWait);
        await TriggerTerminalAsync(probe, cancellation, coordinator.FailActiveCapture);

        return new CaptureTerminalObservation(
            await ObserveTerminalAsync(capture),
            browserDebugConsole.Entries);
    }

    private static async Task<CaptureTerminalObservation> RunSegmentAsync(CaptureTerminalProbe probe)
    {
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        using var coordinator = CreateSegmentCoordinator(browserDebugConsole, PatientLivenessWindow, out _);
        coordinator.ResetToLiveEdge();
        using var cancellation = new CancellationTokenSource();

        var capture = StartSegmentCapture(coordinator, cancellation.Token);
        await browserDebugConsole.WaitForEntryAsync(ArmedTrace, TerminalWait);
        await TriggerTerminalAsync(probe, cancellation, coordinator.FailActiveCapture);

        return new CaptureTerminalObservation(
            await ObserveTerminalAsync(capture),
            browserDebugConsole.Entries);
    }

    private static async Task TriggerTerminalAsync(
        CaptureTerminalProbe probe,
        CancellationTokenSource cancellation,
        Action<Exception> failActiveCapture)
    {
        switch (probe)
        {
            case CaptureTerminalProbe.NoSpeech:
                failActiveCapture(new NoSpeechDetectedException());
                break;
            case CaptureTerminalProbe.Failure:
                failActiveCapture(new InvalidOperationException("probe"));
                break;
            default:
                await cancellation.CancelAsync();
                break;
        }
    }

    private static async Task<string> ObserveTerminalAsync(Task<CapturedSpeechAudio> capture)
    {
        try
        {
            await capture.WaitAsync(TerminalWait);
            return "completed";
        }
        catch (Exception exception)
        {
            // Die Ausnahme ist hier das erwartete Ergebnis; für den Vergleich genügt ihr Typname.
            return exception.GetType().Name;
        }
    }

    private static Task<CapturedSpeechAudio> StartSingleAttemptCapture(
        SpeechCaptureActiveCaptureCoordinator coordinator,
        CancellationToken cancellationToken)
        => coordinator.CaptureAsync(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.ActivationListening),
            initialSilenceTimeout: null,
            cancellationToken);

    private static Task<CapturedSpeechAudio> StartSegmentCapture(
        ActiveSegmentCoordinator coordinator,
        CancellationToken cancellationToken)
        => coordinator.CaptureNextSegmentAsync(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.ActivationListening),
            cancellationToken);

    private static SpeechCaptureActiveCaptureCoordinator CreateSingleAttemptCoordinator(
        IBrowserDebugConsole browserDebugConsole,
        ICaptureDiagnosticsReporter reporter,
        out InMemorySpeechSampleRing ring)
    {
        ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        return new SpeechCaptureActiveCaptureCoordinator(
            new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict).Object,
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            browserDebugConsole,
            CreateSegmenterFactory(),
            ring,
            new StubMicGateState(isHardOff: false),
            reporter,
            speechAmplitudeThresholdResolver: null,
            Options.Create(new SpeechCaptureClientOptions
            {
                ChunkArrivalLivenessTimeout = PatientLivenessWindow,
            }));
    }

    private static ActiveSegmentCoordinator CreateSegmentCoordinator(
        IBrowserDebugConsole browserDebugConsole,
        TimeSpan livenessWindow,
        out InMemorySpeechSampleRing ring)
    {
        ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        return new ActiveSegmentCoordinator(
            CreateSegmenterFactory(),
            ring,
            browserDebugConsole,
            Options.Create(new SpeechCaptureClientOptions { ChunkArrivalLivenessTimeout = livenessWindow }));
    }

    private static SpeechCaptureSegmenterFactory CreateSegmenterFactory()
        => new(new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(new SpeechCaptureClientOptions()),
            Options.Create(new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(20),
                MaxInitialSilence = TimeSpan.FromSeconds(30),
            })));

    private static byte[] CreatePcmChunk(int durationMs, short sampleValue)
    {
        var bytes = new byte[durationMs * SampleRateHz * BytesPerSample / 1000];
        for (var i = 0; i < bytes.Length; i += BytesPerSample)
        {
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i, BytesPerSample), sampleValue);
        }

        return bytes;
    }
}
