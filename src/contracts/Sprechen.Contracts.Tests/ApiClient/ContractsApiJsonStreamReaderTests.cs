using System.Text;
using System.Text.Json;
using Sprechen.Contracts.ApiClient;
using Sprechen.Mda.Contracts.V1.Catalog;
using Xunit;

namespace Sprechen.Contracts.Tests.ApiClient;

public sealed class ContractsApiJsonStreamReaderTests
{
    [Fact]
    public async Task ReadAsync_WhenJsonIsMalformed_PropagatesJsonException()
    {
        var reader = new ContractsApiJsonStreamReader();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{not-json"));

        await Assert.ThrowsAsync<JsonException>(
            () => reader.ReadAsync<FormsCatalogDocument>(
                stream,
                "Failed to deserialize application catalog.",
                CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_WhenJsonIsNull_ThrowsConfiguredInvalidOperationException()
    {
        var reader = new ContractsApiJsonStreamReader();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("null"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reader.ReadAsync<FormsCatalogDocument>(
                stream,
                "Failed to deserialize application catalog.",
                CancellationToken.None));

        Assert.Equal("Failed to deserialize application catalog.", ex.Message);
    }
}
