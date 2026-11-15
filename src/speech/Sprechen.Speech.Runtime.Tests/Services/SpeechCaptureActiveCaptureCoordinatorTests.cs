using System.Buffers.Binary;
using Bunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechCaptureActiveCaptureCoordinatorTests : TestContext
{
    private const int SampleRateHz = 16000;
    private const int BytesPerSample = 2;

    [Fact]
    public async Task CaptureAsync_WhenNoChunkEverArrives_FailsWithStallAfterLivenessWindow()
    {
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        using var coordinator = CreateCoordinator(
            browserDebugConsole,
            out _,
            CreateFastSegmenterOptions(),
            TimeSpan.FromMilliseconds(200));

        var capture = StartListeningCaptureWithInitialSilence(
            coordinator,
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        await Assert.ThrowsAsync<SpeechCaptureStreamStalledException>(
            () => capture.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task CaptureAsync_WhenChunksStopMidUtterance_FailsWithStallAfterLivenessWindow()
    {
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        using var coordinator = CreateCoordinator(
            browserDebugConsole,
            out var ring,
            CreateFastSegmenterOptions(),
            TimeSpan.FromMilliseconds(200));
        var capture = StartListeningCapture(coordinator, CancellationToken.None);

        for (var i = 0; i < 8; i++)
        {
            await ProcessChunkAsync(coordinator, ring, CreateSpeechChunkMs(20));
        }

        await Assert.ThrowsAsync<SpeechCaptureStreamStalledException>(
            () => capture.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task CaptureAsync_WhenPromptInterruptionModeDetectsSpeech_TriggersBargeInHandlerOnce()
    {
        var bargeInHandler = new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict);
        bargeInHandler
            .Setup(handler => handler.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        var optionsFactory = new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(new SpeechCaptureClientOptions()),
            Options.Create(new PcmSpeechCaptureSegmenterOptions()));
        var factory = new SpeechCaptureSegmenterFactory(optionsFactory);
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        using var coordinator = new SpeechCaptureActiveCaptureCoordinator(
            bargeInHandler.Object,
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            browserDebugConsole,
            factory,
            ring,
            TestMicGateInfrastructure.NoOpState());
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.PromptInterruption);

        var captureTask = coordinator.CaptureAsync(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.AssistantPlaybackActive,
            behavior,
            initialSilenceTimeout: null,
            CancellationToken.None);

        await browserDebugConsole.WaitForEntryAsync(
            "speech-capture: capture armed mode=PromptInterruption startContext=AssistantPlaybackActive",
            TimeSpan.FromSeconds(1));

        for (var i = 0; i < 8; i++)
        {
            await ProcessChunkAsync(coordinator, ring, CreatePcmChunk(20, sampleValue: 600));
        }

        for (var i = 0; i < 6; i++)
        {
            await ProcessChunkAsync(coordinator, ring, CreateSpeechChunkMs(20));
        }

        await ProcessChunkAsync(coordinator, ring, CreateSilenceChunkMs(20));

        var utterance = await captureTask;

        Assert.Equal(SpeechAudioMimeType.Pcm, utterance.MimeType);
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => string.Equals(
                entry,
                "speech-capture: speech onset confirmed; requesting prompt interruption",
                StringComparison.Ordinal));
        bargeInHandler.Verify(
            handler => handler.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CaptureAsync_WhenSpeechCompletes_ClearsPendingCaptureAndAllowsNextCapture()
    {
        var browserDebugConsole = await CreateEnabledDebugConsoleAsync();
        using var coordinator = CreateCoordinator(browserDebugConsole, out var ring);

        var firstCapture = StartListeningCapture(coordinator, cancellationToken: CancellationToken.None);
        await WaitForListeningCaptureArmedAsync(browserDebugConsole, expectedCount: 1);

        await CompleteListeningCaptureAsync(coordinator, ring);

        var firstUtterance = await firstCapture.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEmpty(firstUtterance.Chunks);

        var secondCapture = StartListeningCapture(coordinator, cancellationToken: CancellationToken.None);
        await WaitForListeningCaptureArmedAsync(browserDebugConsole, expectedCount: 2);

        await CompleteListeningCaptureAsync(coordinator, ring);

        var secondUtterance = await secondCapture.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEmpty(secondUtterance.Chunks);
    }

    [Fact]
    public async Task CaptureAsync_WhenCallerCancellationIsRequested_CancelsCaptureAndClearsPendingCapture()
    {
        var browserDebugConsole = await CreateEnabledDebugConsoleAsync();
        using var coordinator = CreateCoordinator(browserDebugConsole, out var ring);
        using var cancellation = new CancellationTokenSource();

        var capture = StartListeningCapture(coordinator, cancellation.Token);
        await WaitForListeningCaptureArmedAsync(browserDebugConsole, expectedCount: 1);

        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(() => capture.WaitAsync(TimeSpan.FromSeconds(5)));

        var nextCapture = StartListeningCapture(coordinator, cancellationToken: CancellationToken.None);
        await WaitForListeningCaptureArmedAsync(browserDebugConsole, expectedCount: 2);
        await CompleteListeningCaptureAsync(coordinator, ring);

        var utterance = await nextCapture.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEmpty(utterance.Chunks);
    }

    [Fact]
    public async Task CancelActiveCapture_WhenDrainHasPartialAudio_CompletesWithPartialAudio()
    {
        var browserDebugConsole = await CreateEnabledDebugConsoleAsync();
        using var coordinator = CreateCoordinator(browserDebugConsole, out var ring);

        var capture = StartListeningCapture(coordinator, cancellationToken: CancellationToken.None);
        await WaitForListeningCaptureArmedAsync(browserDebugConsole, expectedCount: 1);

        for (var i = 0; i < 2; i++)
        {
            await ProcessChunkAsync(coordinator, ring, CreateSpeechChunkMs(20));
        }

        coordinator.CancelActiveCapture(CaptureCancellationKind.Drain, CancellationToken.None);

        var utterance = await capture.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEmpty(utterance.Chunks);
        Assert.True(utterance.Chunks.Sum(chunk => chunk.Length) > 0);
    }

    [Fact]
    public async Task CancelActiveCapture_WhenDrainHasNoPartialAudio_CancelsCapture()
    {
        var browserDebugConsole = await CreateEnabledDebugConsoleAsync();
        using var coordinator = CreateCoordinator(browserDebugConsole, out _);
        using var cancellation = new CancellationTokenSource();

        var capture = StartListeningCapture(coordinator, cancellationToken: CancellationToken.None);
        await WaitForListeningCaptureArmedAsync(browserDebugConsole, expectedCount: 1);

        await cancellation.CancelAsync();
        coordinator.CancelActiveCapture(CaptureCancellationKind.Drain, cancellation.Token);

        await Assert.ThrowsAsync<TaskCanceledException>(() => capture.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task FailActiveCapture_WhenCaptureIsArmed_FailsCaptureAndClearsPendingCapture()
    {
        var browserDebugConsole = await CreateEnabledDebugConsoleAsync();
        using var coordinator = CreateCoordinator(browserDebugConsole, out var ring);
        var failure = new InvalidOperationException("forced active capture failure");

        var capture = StartListeningCapture(coordinator, cancellationToken: CancellationToken.None);
        await WaitForListeningCaptureArmedAsync(browserDebugConsole, expectedCount: 1);

        coordinator.FailActiveCapture(failure);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => capture.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Same(failure, actual);

        var nextCapture = StartListeningCapture(coordinator, cancellationToken: CancellationToken.None);
        await WaitForListeningCaptureArmedAsync(browserDebugConsole, expectedCount: 2);
        await CompleteListeningCaptureAsync(coordinator, ring);

        var utterance = await nextCapture.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEmpty(utterance.Chunks);
    }

    [Fact]
    public async Task CaptureAsync_WhenSegmenterCompletesWithNoSpeechException_FailsCaptureAndClearsPendingCapture()
    {
        var browserDebugConsole = await CreateEnabledDebugConsoleAsync();
        using var coordinator = CreateCoordinator(
            browserDebugConsole,
            out var ring,
            new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(20),
                MaxInitialSilence = TimeSpan.FromMilliseconds(40),
            });

        var capture = StartListeningCaptureWithInitialSilence(
            coordinator,
            initialSilenceTimeout: TimeSpan.FromMilliseconds(40),
            cancellationToken: CancellationToken.None);
        await WaitForListeningCaptureArmedAsync(browserDebugConsole, expectedCount: 1);

        for (var i = 0; i < 3; i++)
        {
            await ProcessChunkAsync(coordinator, ring, CreateSilenceChunkMs(20));
        }

        await Assert.ThrowsAsync<NoSpeechDetectedException>(() => capture.WaitAsync(TimeSpan.FromSeconds(5)));

        var nextCapture = StartListeningCapture(coordinator, cancellationToken: CancellationToken.None);
        await WaitForListeningCaptureArmedAsync(browserDebugConsole, expectedCount: 2);
        await CompleteListeningCaptureAsync(coordinator, ring);

        var utterance = await nextCapture.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEmpty(utterance.Chunks);
    }

    [Fact]
    public async Task FailOrCancelActiveCapture_WhenNoCaptureIsArmed_IsExplicitNoOp()
    {
        var browserDebugConsole = await CreateEnabledDebugConsoleAsync();
        using var coordinator = CreateCoordinator(browserDebugConsole, out var ring);

        coordinator.CancelActiveCapture(CaptureCancellationKind.Abort, CancellationToken.None);
        coordinator.CancelActiveCapture(CaptureCancellationKind.Drain, CancellationToken.None);
        coordinator.FailActiveCapture(new InvalidOperationException("ignored without an active capture"));
        await ProcessChunkAsync(coordinator, ring, CreateSpeechChunkMs(20));

        var capture = StartListeningCapture(coordinator, cancellationToken: CancellationToken.None);
        await WaitForListeningCaptureArmedAsync(browserDebugConsole, expectedCount: 1);
        await CompleteListeningCaptureAsync(coordinator, ring);

        var utterance = await capture.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEmpty(utterance.Chunks);
    }

    [Fact]
    public async Task CaptureAsync_WhenArmedWithSettleWindowCursor_ReplaysBacklogAnsweredDuringSettleDelay()
    {
        var browserDebugConsole = await CreateEnabledDebugConsoleAsync();
        using var coordinator = CreateCoordinator(browserDebugConsole, out var ring);

        // Den Cursor vor den Testdaten erfassen, damit Capture anschließend bereits gepuffertes Audio liest.
        var settleCursor = coordinator.OpenLiveCaptureCursor();
        ring.WriteSamples(CreatePcmChunk(120, sampleValue: 1200));
        ring.WriteSamples(CreateSilenceChunkMs(80));

        var capture = coordinator.CaptureAsync(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening),
            initialSilenceTimeout: null,
            startCursor: settleCursor,
            CancellationToken.None);

        var utterance = await capture.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotEmpty(utterance.Chunks);
        Assert.Equal(
            1200,
            BinaryPrimitives.ReadInt16LittleEndian(utterance.Chunks[0].AsSpan(0, BytesPerSample)));
    }

    [Fact]
    public async Task CaptureAsync_WhenSettleWindowBacklogLeadsWithLeakageEcho_DoesNotFalseStartOnEcho()
    {
        var browserDebugConsole = await CreateEnabledDebugConsoleAsync();
        using var coordinator = CreateCoordinator(
            browserDebugConsole,
            out var ring,
            new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(20),
                MaxInitialSilence = TimeSpan.FromMilliseconds(200),
            });

        var settleCursor = coordinator.OpenLiveCaptureCursor();

        // Die niedrigere Testamplitude darf auch beim Lesen aus dem Vorlauf keinen Sprachbeginn auslösen.
        for (var i = 0; i < 12; i++)
        {
            ring.WriteSamples(CreatePcmChunk(20, sampleValue: 600));
        }

        var capture = coordinator.CaptureAsync(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle,
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening),
            initialSilenceTimeout: TimeSpan.FromMilliseconds(200),
            startCursor: settleCursor,
            CancellationToken.None);

        await Assert.ThrowsAsync<NoSpeechDetectedException>(() => capture.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task CaptureAsync_WhenSettleWindowBacklogCarriesGenuineAnswerUnderLeakageGuard_CapturesIt()
    {
        var browserDebugConsole = await CreateEnabledDebugConsoleAsync();
        using var coordinator = CreateCoordinator(browserDebugConsole, out var ring);

        var settleCursor = coordinator.OpenLiveCaptureCursor();

        // Nach den niedrigeren Blöcken muss die höhere Testamplitude im gepufferten Audio erfasst werden.
        for (var i = 0; i < 4; i++)
        {
            ring.WriteSamples(CreatePcmChunk(20, sampleValue: 600));
        }

        for (var i = 0; i < 16; i++)
        {
            ring.WriteSamples(CreatePcmChunk(20, sampleValue: 1400));
        }

        ring.WriteSamples(CreateSilenceChunkMs(80));

        var capture = coordinator.CaptureAsync(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle,
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening),
            initialSilenceTimeout: TimeSpan.FromMilliseconds(400),
            startCursor: settleCursor,
            CancellationToken.None);

        var utterance = await capture.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEmpty(utterance.Chunks);
        Assert.Contains(
            utterance.Chunks,
            chunk => BinaryPrimitives.ReadInt16LittleEndian(chunk.AsSpan(0, BytesPerSample)) == 1400);
    }

    [Fact]
    public async Task CaptureAsync_WhenSpeechCompletes_UtteranceAndReportShareTheSameTurnId()
    {
        // Dieselbe TurnId verbindet das erfasste Audio mit seinem Diagnosebericht.
        var reporter = new CapturingReporter();
        var browserDebugConsole = await CreateEnabledDebugConsoleAsync();
        var optionsFactory = new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(new SpeechCaptureClientOptions()),
            Options.Create(CreateFastSegmenterOptions()));
        var factory = new SpeechCaptureSegmenterFactory(optionsFactory);
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        using var coordinator = new SpeechCaptureActiveCaptureCoordinator(
            new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict).Object,
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            browserDebugConsole,
            factory,
            ring,
            TestMicGateInfrastructure.NoOpState(),
            reporter);

        var capture = StartListeningCapture(coordinator, cancellationToken: CancellationToken.None);
        await WaitForListeningCaptureArmedAsync(browserDebugConsole, expectedCount: 1);
        await CompleteListeningCaptureAsync(coordinator, ring);

        var utterance = await capture.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(utterance.TurnId);
        var report = Assert.Single(reporter.Reports);
        Assert.Equal(utterance.TurnId, report.TurnId);
    }

    private static byte[] CreateSpeechChunkMs(int durationMs)
        => CreatePcmChunk(durationMs, sampleValue: 1000);

    private static byte[] CreateSilenceChunkMs(int durationMs)
        => CreatePcmChunk(durationMs, sampleValue: 0);

    private static byte[] CreatePcmChunk(int durationMs, short sampleValue)
    {
        var samples = durationMs * SampleRateHz / 1000;
        var buffer = new byte[samples * BytesPerSample];

        for (var i = 0; i < samples; i++)
        {
            var offset = i * BytesPerSample;
            BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(offset, BytesPerSample), sampleValue);
        }

        return buffer;
    }

    private static async Task<InMemoryBrowserDebugConsole> CreateEnabledDebugConsoleAsync()
    {
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        return browserDebugConsole;
    }

    private static SpeechCaptureActiveCaptureCoordinator CreateCoordinator(
        InMemoryBrowserDebugConsole browserDebugConsole)
    {
        return CreateCoordinator(browserDebugConsole, out _, CreateFastSegmenterOptions());
    }

    private static SpeechCaptureActiveCaptureCoordinator CreateCoordinator(
        InMemoryBrowserDebugConsole browserDebugConsole,
        out InMemorySpeechSampleRing ring)
    {
        return CreateCoordinator(browserDebugConsole, out ring, CreateFastSegmenterOptions());
    }

    private static SpeechCaptureActiveCaptureCoordinator CreateCoordinator(
        InMemoryBrowserDebugConsole browserDebugConsole,
        out InMemorySpeechSampleRing ring,
        PcmSpeechCaptureSegmenterOptions segmenterOptions,
        TimeSpan? chunkLivenessTimeout = null)
    {
        var optionsFactory = new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(new SpeechCaptureClientOptions()),
            Options.Create(segmenterOptions));
        var factory = new SpeechCaptureSegmenterFactory(optionsFactory);
        ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        return new SpeechCaptureActiveCaptureCoordinator(
            new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict).Object,
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            browserDebugConsole,
            factory,
            ring,
            TestMicGateInfrastructure.NoOpState(),
            captureOptions: Options.Create(new SpeechCaptureClientOptions
            {
                ChunkArrivalLivenessTimeout = chunkLivenessTimeout
                    ?? new SpeechCaptureClientOptions().ChunkArrivalLivenessTimeout
            }));
    }

    private static PcmSpeechCaptureSegmenterOptions CreateFastSegmenterOptions()
    {
        return new PcmSpeechCaptureSegmenterOptions
        {
            SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
            TailSilence = TimeSpan.FromMilliseconds(20),
            MaxInitialSilence = TimeSpan.FromSeconds(10),
        };
    }

    private static Task<CapturedSpeechAudio> StartListeningCapture(
        SpeechCaptureActiveCaptureCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        return StartListeningCaptureWithInitialSilence(
            coordinator,
            initialSilenceTimeout: null,
            cancellationToken);
    }

    private static Task<CapturedSpeechAudio> StartListeningCaptureWithInitialSilence(
        SpeechCaptureActiveCaptureCoordinator coordinator,
        TimeSpan? initialSilenceTimeout,
        CancellationToken cancellationToken)
    {
        return coordinator.CaptureAsync(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening),
            initialSilenceTimeout,
            cancellationToken);
    }

    private static async Task CompleteListeningCaptureAsync(
        SpeechCaptureActiveCaptureCoordinator coordinator,
        InMemorySpeechSampleRing ring)
    {
        for (var i = 0; i < 2; i++)
        {
            await ProcessChunkAsync(coordinator, ring, CreateSpeechChunkMs(20));
        }

        await ProcessChunkAsync(coordinator, ring, CreateSilenceChunkMs(20));
    }

    private static async Task ProcessChunkAsync(
        SpeechCaptureActiveCaptureCoordinator coordinator,
        InMemorySpeechSampleRing ring,
        byte[] chunk)
    {
        ring.WriteSamples(chunk);
        await coordinator.ProcessChunkAsync(chunk, CancellationToken.None);
    }

    private static Task WaitForListeningCaptureArmedAsync(
        InMemoryBrowserDebugConsole browserDebugConsole,
        int expectedCount)
    {
        return WaitForEntryCountAsync(
            browserDebugConsole,
            "speech-capture: capture armed mode=Listening startContext=CleanListening",
            expectedCount);
    }

    private static async Task WaitForEntryCountAsync(
        InMemoryBrowserDebugConsole browserDebugConsole,
        string expectedEntry,
        int expectedCount)
    {
        var deadlineUtc = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1);

        while (DateTimeOffset.UtcNow < deadlineUtc)
        {
            if (browserDebugConsole.Entries.Count(entry => string.Equals(entry, expectedEntry, StringComparison.Ordinal)) >= expectedCount)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }

        throw new TimeoutException($"Browser debug entry was not observed {expectedCount} times within 1s: {expectedEntry}");
    }
}
