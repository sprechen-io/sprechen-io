using Grpc.Core;
using Sprechen.Proto.Transport;
using Sprechen.Web.HostSupport.Grpc.Adapter;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

public sealed class SpeechProxyCallContextFactoryTests
{
    [Fact]
    public void Create_PreservesIncomingCorrelationIdInBackendHeaders()
    {
        var requestHeaders = new Metadata
        {
            { SprechenGrpcCorrelationMetadata.CorrelationIdHeaderName, "client-123" }
        };

        var callContext = new SpeechProxyCallContextFactory().Create(requestHeaders);

        Assert.Equal("client-123", callContext.ClientCorrelationId);
        Assert.Equal("<none>", callContext.TraceId);
        Assert.True(SprechenGrpcCorrelationMetadata.TryGetCorrelationId(callContext.BackendHeaders, out var backendCorrelationId));
        Assert.Equal("client-123", backendCorrelationId);
    }
}
