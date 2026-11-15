using Grpc.Core;
using Microsoft.Extensions.Logging;
using Sprechen.Web.HostSupport.Grpc.Adapter;
using Sprechen.Web.HostSupport.Tests.TestDoubles;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

/// <summary>
/// Prüft, dass RPC- und Token-Abbrüche mindestens auf LogLevel.Information protokolliert werden.
/// So bleibt der Abbruch auch bei deaktivierten Debug-Meldungen erkennbar.
/// </summary>
public sealed class SpeechProxyAbortReadabilityTests
{
    [Theory]
    [InlineData("RpcCancelled")]
    [InlineData("TokenCancelled")]
    public async Task AnAbortedCall_IsReadableWithoutDebug(string terminal)
    {
        var logger = new RecordingLogger<SpeechProxyCancellationExecutor>();
        var executor = new SpeechProxyCancellationExecutor(logger);
        using var caller = new CancellationTokenSource();

        if (terminal == "TokenCancelled")
        {
            await caller.CancelAsync();
        }

        await executor.ExecuteAsync(
            _ => terminal == "TokenCancelled"
                ? throw new OperationCanceledException()
                : throw new RpcException(new Status(StatusCode.Cancelled, "cancelled")),
            fallbackResult: "fallback",
            caller.Token,
            operationName: "SpeechToText",
            stage: "proxy-response-stream",
            clientCorrelationId: "corr-1");

        var entry = Assert.Single(logger.Entries);
        Assert.Contains(terminal, entry.Message, StringComparison.Ordinal);
        Assert.True(
            entry.Level >= LogLevel.Information,
            $"an abort at {terminal} was written at {entry.Level}; at a study level the call's opening "
            + "would be on the record with nothing after it, which reads as a stream that never ended");
    }
}
