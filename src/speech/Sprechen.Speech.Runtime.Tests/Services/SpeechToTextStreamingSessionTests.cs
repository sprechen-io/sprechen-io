using Grpc.Core;
using Sprechen.Proto;
using Moq;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using GrpcSpeechConversationEvent = Sprechen.Proto.SpeechConversationEventMessage;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechToTextStreamingSessionTests
{
    [Fact]
    public async Task Events_WhenServerSendsMultipleTranscripts_YieldsThemInOrderWithoutDropping()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm);
        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var responseReader = new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(new[]
        {
            CreateTranscriptUpdate(" first "),
            CreateTranscriptFinalized("second")
        });

        using var call = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            requestWriter,
            responseReader,
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { });

        await using var session = new SpeechToTextStreamingSession(
            chunkSource,
            call,
            languageCode: "de",
            correlationId: "corr-123",
            trace: CreateTrace(),
            cancellationToken: CancellationToken.None);
        var eventsTask = ReadEventsAsync(session);
        chunkSource.PushChunk(new byte[] { 1, 2, 3 });
        chunkSource.Complete();

        await session.StopAsync(CancellationToken.None);

        var events = await eventsTask;

        Assert.Equal(2, events.Count);
        var update = Assert.IsType<SpeechConversationTranscriptUpdateEvent>(events[0]);
        Assert.Equal(" first ", update.Transcript);
        var finalTranscript = Assert.IsType<SpeechConversationTranscriptFinalizedEvent>(events[1]);
        Assert.Equal("second", finalTranscript.Transcript);

        Assert.True(requestWriter.Completed);
        Assert.Single(requestWriter.Messages);
        Assert.Equal("de", requestWriter.Messages[0].LanguageCode);
        Assert.Equal(1, chunkSource.StopCallCount);
    }

    [Fact]
    public async Task Events_WhenResponseStreamIsCancelled_CompletesWithoutThrowing()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm);
        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();

        var responseReader = new Mock<IAsyncStreamReader<GrpcSpeechConversationEvent>>(MockBehavior.Strict);
        responseReader
            .Setup(r => r.MoveNext(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RpcException(new Status(StatusCode.Cancelled, "cancelled")));

        using var call = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            requestWriter,
            responseReader.Object,
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { });

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        await using var session = new SpeechToTextStreamingSession(
            chunkSource,
            call,
            languageCode: "de",
            correlationId: "corr-123",
            trace: CreateTrace(browserDebugConsole),
            cancellationToken: CancellationToken.None);
        chunkSource.Complete();

        await session.StopAsync(CancellationToken.None);

        var events = await ReadEventsAsync(session);

        Assert.Empty(events);
        Assert.True(requestWriter.Completed);
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => entry.Contains("speech-conversation: cid=corr-123", StringComparison.Ordinal)
                && entry.Contains("speech-to-conversation-stream canceled", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StopAsync_WhenCalledTwice_ReturnsWithoutThrowing()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm);
        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var responseReader = new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(new[]
        {
            CreateTranscriptFinalized("ok")
        });

        using var call = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            requestWriter,
            responseReader,
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { });

        await using var session = new SpeechToTextStreamingSession(
            chunkSource,
            call,
            languageCode: "de",
            correlationId: "corr-123",
            trace: CreateTrace(),
            cancellationToken: CancellationToken.None);
        chunkSource.PushChunk(new byte[] { 1, 2 });
        chunkSource.Complete();

        await session.StopAsync(CancellationToken.None);
        await session.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_WhenSendLoopWriteIsBlocked_CompletesWithinGraceByCancelling()
    {
        // Die Quelle beendet sich beim Stop regulär; blockiert bleibt gezielt der Schreibaufruf.
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm);
        var requestWriter = new BlockingClientStreamWriter<AudioChunk>();
        var responseReader = new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(
            Array.Empty<GrpcSpeechConversationEvent>());

        using var call = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            requestWriter,
            responseReader,
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { });

        await using var session = new SpeechToTextStreamingSession(
            chunkSource,
            call,
            languageCode: "de",
            correlationId: "corr-123",
            trace: CreateTrace(),
            stopGrace: TimeSpan.FromMilliseconds(200),
            cancellationToken: CancellationToken.None);

        chunkSource.PushChunk(new byte[] { 1, 2, 3 });
        await requestWriter.FirstWriteStarted.WaitAsync(TimeSpan.FromSeconds(2));

        // Stop muss auch mit blockiertem Schreibaufruf innerhalb des äußeren Testtimeouts abschließen.
        await session.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task Constructor_WhenStopGraceIsNotPositive_ThrowsArgumentOutOfRange(int graceMilliseconds)
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm);
        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var responseReader = new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(
            Array.Empty<GrpcSpeechConversationEvent>());

        using var call = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            requestWriter,
            responseReader,
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { });

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new SpeechToTextStreamingSession(
            chunkSource,
            call,
            languageCode: "de",
            correlationId: "corr-123",
            trace: CreateTrace(),
            stopGrace: TimeSpan.FromMilliseconds(graceMilliseconds),
            cancellationToken: CancellationToken.None));

        Assert.Equal("stopGrace", exception.ParamName);
    }

    [Fact]
    public async Task StopAsync_WhenCalledConcurrently_WaitsForFirstStopToFinish()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm)
        {
            AutoCompleteOnStop = false
        };
        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var responseReader = new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(new[]
        {
            CreateTranscriptFinalized("ok")
        });

        using var call = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            requestWriter,
            responseReader,
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { });

        await using var session = new SpeechToTextStreamingSession(
            chunkSource,
            call,
            languageCode: "de",
            correlationId: "corr-123",
            trace: CreateTrace(),
            cancellationToken: CancellationToken.None);

        var firstStopTask = session.StopAsync(CancellationToken.None);
        await Task.Yield();

        var secondStopTask = session.StopAsync(CancellationToken.None);

        Assert.False(secondStopTask.IsCompleted);

        chunkSource.Complete();

        await Task.WhenAll(firstStopTask, secondStopTask);
    }

    [Fact]
    public async Task StopAsync_WhenDisposedDuringStop_DoesNotFailBecauseSessionWasCancelledConcurrently()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm)
        {
            AutoCompleteOnStop = false
        };
        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var responseReader = new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(new[]
        {
            CreateTranscriptFinalized("ok")
        });

        using var call = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            requestWriter,
            responseReader,
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { });

        var session = new SpeechToTextStreamingSession(
            chunkSource,
            call,
            languageCode: "de",
            correlationId: "corr-123",
            trace: CreateTrace(),
            cancellationToken: CancellationToken.None);

        var stopTask = session.StopAsync(CancellationToken.None);
        await Task.Yield();

        var disposeTask = session.DisposeAsync().AsTask();
        chunkSource.Complete();

        await stopTask;
        await disposeTask;
    }

    [Fact]
    public async Task StopAsync_WhenServerReturnsNoTranscript_CompletesWithoutNoSpeechError()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm);
        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var responseReader = new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(Array.Empty<GrpcSpeechConversationEvent>());

        using var call = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            requestWriter,
            responseReader,
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { });

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        await using var session = new SpeechToTextStreamingSession(
            chunkSource,
            call,
            languageCode: "de",
            correlationId: "corr-123",
            trace: CreateTrace(browserDebugConsole),
            cancellationToken: CancellationToken.None);
        chunkSource.PushChunk(new byte[] { 1, 2 });
        chunkSource.Complete();

        await session.StopAsync(CancellationToken.None);

        Assert.Contains(
            browserDebugConsole.Entries,
            entry => entry.Contains("speech-conversation: cid=corr-123", StringComparison.Ordinal)
                && entry.Contains("speech-to-conversation-stream completed eventCount=0 transcriptCount=0", StringComparison.Ordinal));
    }

    private static SpeechConversationEventMessage CreateTranscriptUpdate(string text)
    {
        return new SpeechConversationEventMessage
        {
            TranscriptUpdate = new SpeechConversationTranscriptUpdate
            {
                Text = text
            }
        };
    }

    private static SpeechConversationEventMessage CreateTranscriptFinalized(string text)
    {
        return new SpeechConversationEventMessage
        {
            TranscriptFinalized = new SpeechConversationTranscriptFinalized
            {
                Text = text
            }
        };
    }

    private static async Task<List<SpeechConversationEvent>> ReadEventsAsync(SpeechToTextStreamingSession session)
    {
        var events = new List<SpeechConversationEvent>();
        await foreach (var conversationEvent in session.Events)
        {
            events.Add(conversationEvent);
        }

        return events;
    }

    private static BrowserDebugConsoleSpeechClientTrace CreateTrace()
    {
        return new BrowserDebugConsoleSpeechClientTrace(
            new InMemoryBrowserDebugConsole(),
            new RecordingLogger<BrowserDebugConsoleSpeechClientTrace>());
    }

    private static BrowserDebugConsoleSpeechClientTrace CreateTrace(InMemoryBrowserDebugConsole browserDebugConsole)
    {
        return new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole,
            new RecordingLogger<BrowserDebugConsoleSpeechClientTrace>());
    }
}


