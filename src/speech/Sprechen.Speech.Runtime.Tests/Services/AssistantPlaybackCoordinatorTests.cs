using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class AssistantPlaybackCoordinatorTests
{
    [Fact]
    public async Task ResolveCaptureStartContextAsync_NoPlaybackActive_ReturnsCleanListening()
    {
        var earcon = CreateIdleEarconState();
        var tts = CreateIdleTtsState();
        var coordinator = CreateCoordinator(earcon.Object, tts.Object);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.CleanListening, result.Context);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_ContinuationMode_WhenNoPlayback_ReturnsCleanListening()
    {
        var earcon = CreateIdleEarconState();
        var tts = CreateIdleTtsState();
        var coordinator = CreateCoordinator(earcon.Object, tts.Object);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.ListeningContinuation);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.CleanListening, result.Context);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_PromptInterruption_WhenTtsActive_ReturnsAssistantPlaybackActive()
    {
        var earcon = CreateIdleEarconState();
        var tts = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        tts.SetupGet(s => s.IsPlaying).Returns(true);
        tts.Setup(s => s.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>())).Returns(true);
        var coordinator = CreateCoordinator(earcon.Object, tts.Object);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.PromptInterruption);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.AssistantPlaybackActive, result.Context);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_PromptInterruption_WhenRecentlyCompleted_ReturnsContinuation()
    {
        var earcon = CreateIdleEarconState();
        var tts = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        tts.SetupGet(s => s.IsPlaying).Returns(false);
        tts.Setup(s => s.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>())).Returns(true);
        var coordinator = CreateCoordinator(earcon.Object, tts.Object);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.PromptInterruption);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledContinuation, result.Context);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_PromptInterruption_WhenNoPlayback_ReturnsCleanListening()
    {
        var earcon = CreateIdleEarconState();
        var tts = CreateIdleTtsState();
        var coordinator = CreateCoordinator(earcon.Object, tts.Object);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.PromptInterruption);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.CleanListening, result.Context);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_EarconPlaying_WaitsAndReturnsContinuationContext()
    {
        var earcon = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        var earconPlaying = true;
        earcon.SetupGet(s => s.IsPlaying).Returns(() => earconPlaying);
        earcon.Setup(s => s.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>())).Returns(false);
        earcon.Setup(s => s.WaitForIdleAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                earconPlaying = false;
                return Task.CompletedTask;
            });
        var tts = CreateIdleTtsState();
        var coordinator = CreateCoordinator(earcon.Object, tts.Object);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle, result.Context);
        earcon.Verify(s => s.WaitForIdleAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_ContinuationMode_WhenEarconWasPlaying_ReturnsContinuationContext()
    {
        var earcon = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        var earconPlaying = true;
        earcon.SetupGet(s => s.IsPlaying).Returns(() => earconPlaying);
        earcon.Setup(s => s.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>())).Returns(false);
        earcon.Setup(s => s.WaitForIdleAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                earconPlaying = false;
                return Task.CompletedTask;
            });
        var tts = CreateIdleTtsState();
        var coordinator = CreateCoordinator(earcon.Object, tts.Object);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.ListeningContinuation);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledContinuation, result.Context);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_PostPlaybackIdle_DefaultsLookBackCursorToPlaybackSettlePoint()
    {
        // Nach kürzlich beendeter Wiedergabe soll Listening einen Startcursor für den Nachlauf erhalten.
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);

        // Die vor dem Aufruf gepufferten 100 Bytes gehören zur bereits beendeten Wiedergabe.
        // Der Cursor soll dahinter liegen; bei einem noch laufenden Signalton gilt ein früherer Bezugspunkt.
        ring.WriteSamples(new byte[100]);

        var earcon = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earcon.SetupGet(s => s.IsPlaying).Returns(false);
        earcon
            .SetupSequence(s => s.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(true)   // Nachlauf noch aktiv
            .Returns(false); // Nachlauf beendet
        var tts = CreateIdleTtsState();
        var coordinator = CreateCoordinator(earcon.Object, tts.Object, sampleRing: ring);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle, result.Context);
        Assert.Equal(new SampleCursor(100), result.PlaybackSettleLookbackCursor);
        Assert.NotEqual(new SampleCursor(0), result.PlaybackSettleLookbackCursor);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_WakeFallbackActivationListening_InheritsBoundedSettleLookBackCursor()
    {
        // ActivationListening soll wie Listening den begrenzten Nachlaufcursor erhalten.
        // Damit kann ein Aufruf ohne eigenen Cursor auch während des Wartens eingegangene Daten nutzen.
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);

        // Bereits vor dem Aufruf gepufferte Wiedergabedaten dürfen nicht erneut gelesen werden.
        ring.WriteSamples(new byte[100]);

        var earcon = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earcon.SetupGet(s => s.IsPlaying).Returns(false);
        earcon
            .SetupSequence(s => s.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(true)   // Nachlauf noch aktiv
            .Returns(false); // Nachlauf beendet
        var tts = CreateIdleTtsState();
        var coordinator = CreateCoordinator(earcon.Object, tts.Object, sampleRing: ring);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.ActivationListening);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle, result.Context);
        Assert.Equal(new SampleCursor(100), result.PlaybackSettleLookbackCursor);
        Assert.NotEqual(new SampleCursor(0), result.PlaybackSettleLookbackCursor);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_BargeIn_WhenPlaybackActive_DoesNotDefaultLookBackCursor()
    {
        // Eine Unterbrechungsaufnahme beginnt bei laufender Ausgabe und übernimmt keinen Nachlaufcursor.
        var earcon = CreateIdleEarconState();
        var tts = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        tts.SetupGet(s => s.IsPlaying).Returns(true);
        tts.Setup(s => s.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>())).Returns(true);
        var coordinator = CreateCoordinator(earcon.Object, tts.Object);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.PromptInterruption);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.AssistantPlaybackActive, result.Context);
        Assert.Null(result.PlaybackSettleLookbackCursor);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_PostPlaybackContinuation_DoesNotDefaultLookBackCursor()
    {
        // Fortsetzungsaufnahmen übernehmen diesen Standardcursor für neue Aufnahmen nicht.
        var earcon = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earcon.SetupGet(s => s.IsPlaying).Returns(false);
        earcon
            .SetupSequence(s => s.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(true)
            .Returns(false);
        var tts = CreateIdleTtsState();
        var coordinator = CreateCoordinator(earcon.Object, tts.Object);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.ListeningContinuation);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledContinuation, result.Context);
        Assert.Null(result.PlaybackSettleLookbackCursor);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_CleanListening_DoesNotDefaultLookBackCursor()
    {
        var earcon = CreateIdleEarconState();
        var tts = CreateIdleTtsState();
        var coordinator = CreateCoordinator(earcon.Object, tts.Object);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.CleanListening, result.Context);
        Assert.Null(result.PlaybackSettleLookbackCursor);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_ListeningCueStillPlaying_AnchorsLookBackCursorAtSpeechEnd()
    {
        // Eine Antwort kann schon während des Signaltons beginnen. Der Cursor muss deshalb
        // vor diesem Ton liegen, damit die währenddessen eingegangenen Daten verfügbar bleiben.
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        ring.WriteSamples(new byte[100]); // Daten bis zum Ende der Sprachausgabe

        var earcon = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        var earconPlaying = true;
        earcon.SetupGet(s => s.IsPlaying).Returns(() => earconPlaying);
        earcon
            .SetupSequence(s => s.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(true)   // Signalton beendet, Nachlauf noch aktiv
            .Returns(false); // Nachlauf beendet
        earcon.Setup(s => s.WaitForIdleAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                // Während des Signaltons treffen weitere 200 Bytes ein, die zum Antwortanfang gehören können.
                ring.WriteSamples(new byte[200]);
                earconPlaying = false;
                return Task.CompletedTask;
            });
        var tts = CreateIdleTtsState();
        var coordinator = CreateCoordinator(earcon.Object, tts.Object, sampleRing: ring);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        // Cursor 100 lässt die während des Signaltons ergänzten Bytes im lesbaren Bereich.
        Assert.Equal(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle, result.Context);
        Assert.Equal(new SampleCursor(100), result.PlaybackSettleLookbackCursor);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_EarconWaitWithoutSettleLoop_KeepsSpeechEndCursor()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        ring.WriteSamples(new byte[100]);

        var earconPlaying = true;
        var earcon = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earcon.SetupGet(s => s.IsPlaying).Returns(() => earconPlaying);
        earcon.Setup(s => s.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>())).Returns(false);
        earcon.Setup(s => s.WaitForIdleAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                ring.WriteSamples(new byte[200]);
                earconPlaying = false;
                return Task.CompletedTask;
            });
        var tts = CreateIdleTtsState();
        var coordinator = CreateCoordinator(earcon.Object, tts.Object, sampleRing: ring);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle, result.Context);
        Assert.Equal(new SampleCursor(100), result.PlaybackSettleLookbackCursor);
    }

    [Fact]
    public async Task ResolveCaptureStartContextAsync_CueRegistersMidSettleLoop_KeepsSpeechEndAnchor()
    {
        // Der Signalton kann erst während des Wartens als aktiv gemeldet werden.
        // Sein späteres Ende darf den bereits gesetzten Cursor nicht nach hinten verschieben.
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        ring.WriteSamples(new byte[100]); // Daten bis zum Ende der Sprachausgabe

        // Die ersten zwei Abfragen melden Ruhe, die Abfragen 3 bis 7 einen laufenden Signalton.
        // Danach kommen 200 Bytes hinzu. Ein erst dann gesetzter Cursor läge bei 300 und
        // würde diese Daten überspringen; erwartet bleibt die frühere Position 100.
        var earconProbes = 0;
        var cueWindowWritten = false;
        var earcon = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earcon.SetupGet(s => s.IsPlaying).Returns(() =>
        {
            earconProbes++;
            if (earconProbes < 3)
            {
                return false;
            }

            if (earconProbes <= 7)
            {
                return true;
            }

            if (!cueWindowWritten)
            {
                cueWindowWritten = true;
                ring.WriteSamples(new byte[200]);
            }

            return false;
        });
        earcon.Setup(s => s.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>())).Returns(false);
        var tts = CreateIdleTtsState(); // Nur der Signalton hält die Wartephase aktiv.

        var coordinator = CreateCoordinator(earcon.Object, tts.Object, sampleRing: ring);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        var result = await coordinator.ResolveCaptureStartContextAsync(behavior, CancellationToken.None);

        Assert.Equal(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle, result.Context);
        Assert.Equal(new SampleCursor(100), result.PlaybackSettleLookbackCursor);
    }

    private static Mock<IEarconPlaybackState> CreateIdleEarconState()
    {
        var mock = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        mock.SetupGet(s => s.IsPlaying).Returns(false);
        mock.Setup(s => s.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>())).Returns(false);
        return mock;
    }

    private static Mock<ITextToSpeechPlaybackState> CreateIdleTtsState()
    {
        var mock = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        mock.SetupGet(s => s.IsPlaying).Returns(false);
        mock.Setup(s => s.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>())).Returns(false);
        return mock;
    }

    private static AssistantPlaybackCoordinator CreateCoordinator(
        IEarconPlaybackState earconPlaybackState,
        ITextToSpeechPlaybackState textToSpeechPlaybackState,
        SpeechCaptureClientOptions? options = null,
        SpeechSampleRing? sampleRing = null)
    {
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        return new AssistantPlaybackCoordinator(
            earconPlaybackState,
            textToSpeechPlaybackState,
            Options.Create(options ?? new SpeechCaptureClientOptions()),
            sampleRing ?? new InMemorySpeechSampleRing(maxLookbackMs: 5_000),
            browserDebugConsole,
            NullLogger<AssistantPlaybackCoordinator>.Instance);
    }
}
