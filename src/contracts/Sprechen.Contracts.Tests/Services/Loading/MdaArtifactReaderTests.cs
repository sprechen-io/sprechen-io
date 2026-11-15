using System.Security.Cryptography;
using System.Text;
using Sprechen.Contracts.Services.Loading;
using Xunit;

namespace Sprechen.Contracts.Tests.Services.Loading;

public sealed class MdaArtifactReaderTests
{
    [Fact]
    public void ReadJsonArtifact_WhenContentIsValid_ComputesContentDerivedETag()
    {
        var content = Encoding.UTF8.GetBytes("{\"ok\":true}");
        var reader = new MdaArtifactReader(_ => content, _ => []);

        var artifact = reader.ReadJsonArtifact("test/reference.json", "application/json");

        var expectedHash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        Assert.Equal("application/json", artifact.ContentType);
        Assert.Equal(content, artifact.Content);
        Assert.Equal($"\"{expectedHash}\"", artifact.ETag);
    }

    [Fact]
    public void ReadJsonArtifact_WhenArtifactIsMissing_Throws()
    {
        var reader = new MdaArtifactReader(
            path => throw new InvalidOperationException($"Embedded runtime artifact '{path}' was not found."),
            _ => []);

        var ex = Assert.Throws<InvalidOperationException>(
            () => reader.ReadJsonArtifact("missing/reference.json", "application/json"));

        Assert.Contains("was not found", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadJsonArtifact_WhenArtifactIsEmpty_Throws()
    {
        var reader = new MdaArtifactReader(_ => [], _ => []);

        var ex = Assert.Throws<InvalidOperationException>(
            () => reader.ReadJsonArtifact("empty/reference.json", "application/json"));

        Assert.Contains("is empty", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadJsonArtifact_WhenJsonIsMalformed_ThrowsInvalidJsonArtifact()
    {
        var reader = new MdaArtifactReader(_ => Encoding.UTF8.GetBytes("{not-json"), _ => []);

        var ex = Assert.Throws<InvalidOperationException>(
            () => reader.ReadJsonArtifact("broken/reference.json", "application/json"));

        Assert.Contains("Invalid JSON artifact 'broken/reference.json'", ex.Message, StringComparison.Ordinal);
    }
}
