using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using GrpcSpeechConversationEvent = Sprechen.Proto.SpeechConversationEventMessage;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechToTextStreamingReceiveLoopTests
{
    [Fact]
    public async Task RunAsync_WhenFailedEventArrives_CompletesChannelWithTerminalFailure()
    {
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var events = new SpeechToTextStreamingEventChannel();
        var loop = new SpeechToTextStreamingReceiveLoop(
            events,
            CreateTrace(browserDebugConsole),
            CancellationToken.None);
        var responseReader = new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(new[]
        {
            CreateFailedEvent("provider.failure", "boom")
        });

        var runException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => loop.RunAsync(responseReader, CancellationToken.None));
        var readResult = await ReadEventsAndCaptureExceptionAsync(events.Events);

        Assert.Equal("boom", runException.Message);
        Assert.IsType<SpeechConversationFailedEvent>(Assert.Single(readResult.Events));
        var readException = Assert.IsType<InvalidOperationException>(readResult.Exception);
        Assert.Equal("boom", readException.Message);
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => entry.Contains("speech-conversation: cid=corr-123", StringComparison.Ordinal)
                && entry.Contains("speech-to-conversation-stream failed-event eventCount=1 transcriptCount=0", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_WhenRpcFailureOccurs_CompletesChannelWithMappedSpeechError()
    {
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var events = new SpeechToTextStreamingEventChannel();
        var loop = new SpeechToTextStreamingReceiveLoop(
            events,
            CreateTrace(browserDebugConsole),
            CancellationToken.None);
        var responseReader = new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(
            Array.Empty<GrpcSpeechConversationEvent>(),
            new RpcException(new Status(StatusCode.Unavailable, "offline")));

        var runException = await Assert.ThrowsAsync<SpeechErrorException>(
            () => loop.RunAsync(responseReader, CancellationToken.None));
        var readResult = await ReadEventsAndCaptureExceptionAsync(events.Events);

        Assert.Equal(SpeechErrorCode.Unspecified, runException.ErrorCode);
        Assert.Empty(readResult.Events);
        var readException = Assert.IsType<SpeechErrorException>(readResult.Exception);
        Assert.Equal(SpeechErrorCode.Unspecified, readException.ErrorCode);
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => entry.Contains("speech-conversation: cid=corr-123", StringComparison.Ordinal)
                && entry.Contains("speech-to-conversation-stream failed statusCode=Unavailable eventCount=0 transcriptCount=0", StringComparison.Ordinal));
    }

    private static SpeechConversationEventMessage CreateFailedEvent(string errorCode, string detail)
    {
        return new SpeechConversationEventMessage
        {
            ConversationFailed = new SpeechConversationFailed
            {
                ErrorCode = errorCode,
                Detail = detail
            }
        };
    }

    private static async Task<SpeechToTextStreamingReadEventsResult> ReadEventsAndCaptureExceptionAsync(IAsyncEnumerable<SpeechConversationEvent> events)
    {
        var emittedEvents = new List<SpeechConversationEvent>();
        try
        {
            await foreach (var conversationEvent in events)
            {
                emittedEvents.Add(conversationEvent);
            }

            return new SpeechToTextStreamingReadEventsResult(emittedEvents, Exception: null);
        }
        catch (Exception ex)
        {
            return new SpeechToTextStreamingReadEventsResult(emittedEvents, ex);
        }
    }

    private static SpeechToTextStreamingSessionTrace CreateTrace(InMemoryBrowserDebugConsole browserDebugConsole)
    {
        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole,
            new RecordingLogger<BrowserDebugConsoleSpeechClientTrace>());

        return new SpeechToTextStreamingSessionTrace(trace, "corr-123", "de");
    }
}
