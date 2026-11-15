using Grpc.Core;
using Moq;
using Sprechen.Proto.Transport;
using Sprechen.Service.Grpc;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc.Cross;

public sealed class LanguageResolverTests
{
    [Fact]
    public void ResolveRequiredIsoLanguageCode_WhenSupportedNormalizesCode()
    {
        var resolver = new LanguageResolver(CreateSupportedLanguages().Object);

        var resolved = resolver.ResolveRequiredIsoLanguageCode(" DE ", "missing");

        Assert.Equal("de", resolved);
    }

    [Fact]
    public void ResolveOptionalIsoLanguageCode_WhenMissingReturnsNull()
    {
        var resolver = new LanguageResolver(CreateSupportedLanguages().Object);

        var resolved = resolver.ResolveOptionalIsoLanguageCode(" ");

        Assert.Null(resolved);
    }

    [Fact]
    public void ResolveRequiredIsoLanguageCode_WhenUnsupportedThrowsStableCode()
    {
        var resolver = new LanguageResolver(CreateSupportedLanguages().Object);

        var ex = Assert.Throws<RpcException>(() =>
            resolver.ResolveRequiredIsoLanguageCode("fr", "missing"));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.LanguageCodeUnsupported, errorCode);
    }

    [Fact]
    public void ResolveLanguageDisplayName_WhenCatalogContainsLanguageReturnsDisplayName()
    {
        var resolver = new LanguageResolver(CreateSupportedLanguages().Object);

        var displayName = resolver.ResolveLanguageDisplayName("EN");

        Assert.Equal("English", displayName);
    }

    private static Mock<ISupportedLanguagesService> CreateSupportedLanguages()
    {
        var supportedLanguages = new Mock<ISupportedLanguagesService>(MockBehavior.Strict);
        supportedLanguages
            .SetupGet(service => service.Languages)
            .Returns(new[]
            {
                new SupportedLanguage("de", "Deutsch"),
                new SupportedLanguage("en", "English")
            });
        supportedLanguages
            .Setup(service => service.IsSupportedIsoCode(It.IsAny<string>()))
            .Returns((string code) => string.Equals(code, "de", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(code, "en", StringComparison.OrdinalIgnoreCase));

        return supportedLanguages;
    }
}
