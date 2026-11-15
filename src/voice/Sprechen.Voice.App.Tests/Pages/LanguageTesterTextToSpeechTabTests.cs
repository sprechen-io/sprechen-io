using Bunit;
using Moq;
using Sprechen.Voice.App.Pages;

namespace Sprechen.Voice.App.Tests.Pages;

public sealed class LanguageTesterTextToSpeechTabTests : TestContext
{
    [Fact]
    public void OnInitializedAsync_WhenLanguagesLoaded_RendersSelectOptions()
    {
        Services.AddSingleton(CreateSupportedLanguagesClient(defaultIsoCode: "en"));
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());

        var cut = Render<LanguageTesterTextToSpeechTab>();

        cut.WaitForAssertion(() =>
        {
            var options = cut.FindAll("#lang option");
            Assert.Contains(options, o => o.GetAttribute("value") == "de");
            Assert.Contains(options, o => o.GetAttribute("value") == "en");
        });
    }

    [Fact]
    public async Task TextToSpeechAsync_WhenSubmitted_SpeaksWithSelectedLanguage()
    {
        Services.AddSingleton(CreateSupportedLanguagesClient(defaultIsoCode: "en"));

        var ttsPlayer = new Mock<ITextToSpeechPlayer>();
        Services.AddSingleton(ttsPlayer.Object);

        var cut = Render<LanguageTesterTextToSpeechTab>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#lang")));

        cut.Find("#text").Change("Hello");
        cut.Find("#lang").Change("de");
        await cut.Find("form").SubmitAsync();

        ttsPlayer.Verify(
            player => player.SpeakAsync(
                "Hello",
                "de",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void OnInitializedAsync_WhenLanguageLoadFails_ShowsError()
    {
        var supportedLanguagesClient = new Mock<ISupportedLanguagesClient>(MockBehavior.Strict);
        supportedLanguagesClient
            .Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("down"));

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());

        var cut = Render<LanguageTesterTextToSpeechTab>();

        cut.WaitForAssertion(() => Assert.Contains("Loading supported languages failed", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task TextToSpeechAsync_WhenTextIsWhitespace_ShowsError()
    {
        Services.AddSingleton(CreateSupportedLanguagesClient(defaultIsoCode: "de"));
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());

        var cut = Render<LanguageTesterTextToSpeechTab>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#lang")));

        cut.Find("#text").Change("  ");
        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Contains("TextToSpeech failed: Text is required.", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task TextToSpeechAsync_WhenSupportedLanguagesMissing_ShowsError()
    {
        var supportedLanguagesClient = new Mock<ISupportedLanguagesClient>(MockBehavior.Strict);
        supportedLanguagesClient
            .Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportedLanguages("de", Array.Empty<SupportedLanguage>()));

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());

        var cut = Render<LanguageTesterTextToSpeechTab>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#text")));

        cut.Find("#text").Change("Hello");
        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Contains("Supported languages are not available.", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task TextToSpeechAsync_WhenPlayerIsCancelled_DoesNotShowError()
    {
        Services.AddSingleton(CreateSupportedLanguagesClient(defaultIsoCode: "de"));

        var player = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        player
            .Setup(p => p.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("cancelled"));
        Services.AddSingleton(player.Object);

        var cut = Render<LanguageTesterTextToSpeechTab>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#lang")));

        cut.Find("#text").Change("Hello");
        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.DoesNotContain("alert-danger", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TextToSpeechAsync_WhenPlayerThrows_ShowsError()
    {
        Services.AddSingleton(CreateSupportedLanguagesClient(defaultIsoCode: "de"));

        var player = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        player
            .Setup(p => p.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SpeechErrorException(SpeechErrorCode.ProviderFailure, "boom"));
        Services.AddSingleton(player.Object);

        var cut = Render<LanguageTesterTextToSpeechTab>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#lang")));

        cut.Find("#text").Change("Hello");
        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Contains("TextToSpeech failed: boom", cut.Markup, StringComparison.Ordinal));
    }

    private static ISupportedLanguagesClient CreateSupportedLanguagesClient(string defaultIsoCode)
    {
        var client = new Mock<ISupportedLanguagesClient>(MockBehavior.Strict);
        client
            .Setup(service => service.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportedLanguages(
                defaultIsoCode,
                [
                    new SupportedLanguage("de", "Deutsch"),
                    new SupportedLanguage("en", "English")
                ]));

        return client.Object;
    }
}
