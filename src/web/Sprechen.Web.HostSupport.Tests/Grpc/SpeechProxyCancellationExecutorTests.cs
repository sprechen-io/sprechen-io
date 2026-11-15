using Grpc.Core;
using Sprechen.Web.HostSupport.Tests.TestDoubles;
using Sprechen.Web.HostSupport.Grpc.Adapter;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

public sealed class SpeechProxyCancellationExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenBackendRpcIsCancelled_ReturnsFallbackAndLogsStage()
    {
        var logger = new RecordingLogger<SpeechProxyCancellationExecutor>();
        var executor = new SpeechProxyCancellationExecutor(logger);

        var result = await executor.ExecuteAsync(
            _ => throw new RpcException(new Status(StatusCode.Cancelled, "cancelled")),
            fallbackResult: "fallback",
            CancellationToken.None,
            operationName: "SpeechToConversationStream",
            stage: "proxy-response-stream",
            clientCorrelationId: "corr-123");

        Assert.Equal("fallback", result);
        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("SpeechToConversationStream", StringComparison.Ordinal)
                && entry.Message.Contains("proxy-response-stream", StringComparison.Ordinal)
                && entry.Message.Contains("corr-123", StringComparison.Ordinal));
    }
}
