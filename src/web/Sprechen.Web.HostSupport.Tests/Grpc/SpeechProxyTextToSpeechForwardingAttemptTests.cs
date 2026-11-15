using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Web.HostSupport.Grpc.Adapter;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

public sealed class SpeechProxyTextToSpeechForwardingAttemptTests
{
    [Fact]
    public async Task TryForwardAsync_CancelledRpc_ReportsAnAbortRatherThanNothing()
    {
        // Der Abbruchmarker unterscheidet den abgebrochenen RPC vom erfolgreichen leeren Ergebnis und ermöglicht eine eigene Abschlussmeldung.
        var attempt = new SpeechProxyTextToSpeechForwardingAttempt();
        var writer = new RecordingServerStreamWriter<AudioResponse>();

        var summary = await attempt.TryForwardAsync(
            _ => new AsyncServerStreamingCall<AudioResponse>(
                new SequenceAsyncStreamReader<AudioResponse>(
                    [],
                    new RpcException(new Status(StatusCode.Cancelled, "cancelled"))),
                Task.FromResult(new Metadata()),
                static () => new Status(StatusCode.Cancelled, "cancelled"),
                static () => new Metadata(),
                static () => { }),
            writer,
            CancellationToken.None);

        Assert.True(summary.Aborted);
        Assert.Equal(0, summary.ChunkCount);
        Assert.Empty(writer.Items);
    }
}
