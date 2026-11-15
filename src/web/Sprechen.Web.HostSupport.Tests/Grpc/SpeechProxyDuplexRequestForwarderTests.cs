using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Web.HostSupport.Grpc.Adapter;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

public sealed class SpeechProxyDuplexRequestForwarderTests
{
    [Fact]
    public async Task ForwardAsync_ForwardsAllRequestsAndCompletesBackendStream()
    {
        var backendWriter = new CollectingClientStreamWriter<AudioChunk>();
        using var call = new AsyncDuplexStreamingCall<AudioChunk, SpeechTranscript>(
            backendWriter,
            new SequenceAsyncStreamReader<SpeechTranscript>([]),
            Task.FromResult(new Metadata()),
            static () => new Status(StatusCode.OK, string.Empty),
            static () => new Metadata(),
            static () => { });
        var requestStream = new SequenceAsyncStreamReader<AudioChunk>(
        [
            new AudioChunk { LanguageCode = "de-DE" },
            new AudioChunk { LanguageCode = "en-US" }
        ]);

        await new SpeechProxyDuplexRequestForwarder().ForwardAsync(
            requestStream,
            call,
            CancellationToken.None);

        Assert.True(backendWriter.Completed);
        Assert.Equal(["de-DE", "en-US"], backendWriter.Messages.Select(message => message.LanguageCode));
    }
}
