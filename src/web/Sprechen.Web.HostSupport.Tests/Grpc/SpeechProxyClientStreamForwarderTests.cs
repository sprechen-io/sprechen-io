using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Web.HostSupport.Grpc.Adapter;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

public sealed class SpeechProxyClientStreamForwarderTests
{
    [Fact]
    public async Task ForwardAsync_ForwardsAllRequestsCompletesStreamAndReturnsBackendResponse()
    {
        var backendWriter = new CollectingClientStreamWriter<AudioChunk>();
        var backendResponse = new SpeechTranscript { Text = "hello" };
        using var call = new AsyncClientStreamingCall<AudioChunk, SpeechTranscript>(
            backendWriter,
            Task.FromResult(backendResponse),
            Task.FromResult(new Metadata()),
            static () => new Status(StatusCode.OK, string.Empty),
            static () => new Metadata(),
            static () => { });
        var requestStream = new SequenceAsyncStreamReader<AudioChunk>(
        [
            new AudioChunk { LanguageCode = "de-DE" },
            new AudioChunk { LanguageCode = "en-US" }
        ]);

        var response = await new SpeechProxyClientStreamForwarder().ForwardAsync(
            call,
            requestStream,
            CancellationToken.None);

        Assert.Same(backendResponse, response);
        Assert.True(backendWriter.Completed);
        Assert.Equal(["de-DE", "en-US"], backendWriter.Messages.Select(message => message.LanguageCode));
    }
}
