using Moq;
using Xunit;
using Sprechen.Service.Application.Cqrs.LanguageCatalog;

namespace Sprechen.Service.Application.Tests;

public sealed class ListSupportedLanguagesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsDefaultIsoCodeAndLanguagesFromService()
    {
        var supportedLanguages = new Mock<ISupportedLanguagesService>(MockBehavior.Strict);
        supportedLanguages.SetupGet(service => service.DefaultIsoCode).Returns("de");
        supportedLanguages
            .SetupGet(service => service.Languages)
            .Returns(new[]
            {
                new SupportedLanguage("de", "Deutsch"),
                new SupportedLanguage("en", "English")
            });
        var handler = new ListSupportedLanguagesQueryHandler(supportedLanguages.Object);

        var result = await handler.HandleAsync(new ListSupportedLanguagesQuery(), CancellationToken.None);

        Assert.Equal("de", result.DefaultIsoCode);
        Assert.Collection(
            result.Languages,
            first =>
            {
                Assert.Equal("de", first.IsoCode);
                Assert.Equal("Deutsch", first.DisplayName);
            },
            second =>
            {
                Assert.Equal("en", second.IsoCode);
                Assert.Equal("English", second.DisplayName);
            });
        supportedLanguages.VerifyAll();
    }

    [Fact]
    public void Constructor_WhenSupportedLanguagesIsNullThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ListSupportedLanguagesQueryHandler(null!));
    }
}
