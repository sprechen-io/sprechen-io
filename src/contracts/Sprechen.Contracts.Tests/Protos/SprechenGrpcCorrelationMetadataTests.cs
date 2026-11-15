using Grpc.Core;
using Sprechen.Proto.Transport;
using Xunit;

namespace Sprechen.Contracts.Tests.Protos;

public sealed class SprechenGrpcCorrelationMetadataTests
{
    [Fact]
    public void CreateHeaders_WhenCorrelationIdIsSet_WritesHeader()
    {
        var headers = SprechenGrpcCorrelationMetadata.CreateHeaders("corr-123");

        Assert.True(SprechenGrpcCorrelationMetadata.TryGetCorrelationId(headers, out var correlationId));
        Assert.Equal("corr-123", correlationId);
    }

    [Fact]
    public void AddOrReplaceCorrelationId_WhenHeaderAlreadyExists_ReplacesExistingValue()
    {
        var headers = new Metadata
        {
            { SprechenGrpcCorrelationMetadata.CorrelationIdHeaderName, "old" }
        };

        SprechenGrpcCorrelationMetadata.AddOrReplaceCorrelationId(headers, "new");

        Assert.True(SprechenGrpcCorrelationMetadata.TryGetCorrelationId(headers, out var correlationId));
        Assert.Equal("new", correlationId);
        Assert.Single(
            headers,
            entry => string.Equals(
                entry.Key,
                SprechenGrpcCorrelationMetadata.CorrelationIdHeaderName,
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetOrCreateCorrelationId_WhenHeaderIsMissing_ReturnsGeneratedCorrelationId()
    {
        var correlationId = SprechenGrpcCorrelationMetadata.GetOrCreateCorrelationId(headers: null);

        Assert.False(string.IsNullOrWhiteSpace(correlationId));
    }
}
