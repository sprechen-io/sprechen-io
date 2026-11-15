using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moq;
using Sprechen.Voice.App.Pages;

namespace Sprechen.Voice.App.Tests.Pages;

public sealed class LanguageTesterTests : TestContext
{
    private void RegisterTablistServices()
    {
        var supportedLanguagesClient = new Mock<ISupportedLanguagesClient>(MockBehavior.Strict);
        supportedLanguagesClient
            .Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportedLanguages(
                "en",
                [
                    new SupportedLanguage("de", "Deutsch"),
                    new SupportedLanguage("en", "English")
                ]));

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(Mock.Of<ISpeechToTextStreamingClient>());
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
    }

    [Fact]
    public void Heading_RendersLocalizedTitleUnderGermanLanguageState()
    {
        var languageState = new UserLanguageState();
        languageState.SetSelectedIsoCode("de");

        var supportedLanguagesClient = new Mock<ISupportedLanguagesClient>(MockBehavior.Strict);
        supportedLanguagesClient
            .Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportedLanguages(
                "en",
                [
                    new SupportedLanguage("de", "Deutsch"),
                    new SupportedLanguage("en", "English")
                ]));

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(Mock.Of<ISpeechToTextStreamingClient>());
        Services.AddSingleton<IUserLanguageState>(languageState);

        var cut = Render<LanguageTester>();

        var heading = cut.Find("h1");
        Assert.Equal("Sprachtester", heading.TextContent);
        Assert.DoesNotContain("Language Tester", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Heading_ReRendersLocalizedTitle_WhenSelectedLanguageChanges()
    {
        var languageState = new UserLanguageState();
        languageState.SetSelectedIsoCode("de");

        var supportedLanguagesClient = new Mock<ISupportedLanguagesClient>(MockBehavior.Strict);
        supportedLanguagesClient
            .Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportedLanguages(
                "en",
                [
                    new SupportedLanguage("de", "Deutsch"),
                    new SupportedLanguage("en", "English")
                ]));

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(Mock.Of<ISpeechToTextStreamingClient>());
        Services.AddSingleton<IUserLanguageState>(languageState);

        var cut = Render<LanguageTester>();
        Assert.Equal("Sprachtester", cut.Find("h1").TextContent);


        cut.InvokeAsync(() => languageState.SetSelectedIsoCode("en"));

        cut.WaitForAssertion(() => Assert.Equal("Language Tester", cut.Find("h1").TextContent));
    }

    [Fact]
    public void Tablist_HasAccessibleTabAttributesAtParityWithSettings()
    {
        RegisterTablistServices();

        var cut = Render<LanguageTester>();

        var tts = cut.Find("#language-tester-tab-tts");
        var stt = cut.Find("#language-tester-tab-stt");

        Assert.Equal("tab", tts.GetAttribute("role"));
        Assert.Equal("tab", stt.GetAttribute("role"));
        Assert.Equal("true", tts.GetAttribute("aria-selected"));
        Assert.Equal("false", stt.GetAttribute("aria-selected"));
        Assert.Equal("language-tester-panel-tts", tts.GetAttribute("aria-controls"));
        Assert.Equal("language-tester-panel-stt", stt.GetAttribute("aria-controls"));
        Assert.Equal("0", tts.GetAttribute("tabindex"));
        Assert.Equal("-1", stt.GetAttribute("tabindex"));

        var panel = cut.Find("#language-tester-panel-tts");
        Assert.Equal("tabpanel", panel.GetAttribute("role"));
        Assert.Equal("language-tester-tab-tts", panel.GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Tablist_ArrowAndHomeKeys_SwitchTabsAndUpdateRovingTabindex()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        RegisterTablistServices();

        var cut = Render<LanguageTester>();

        cut.WaitForAssertion(() => Assert.Contains("Text To Speech", cut.Markup, StringComparison.Ordinal));

        var tablist = cut.Find("ul.nav-tabs");
        tablist.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Speech To Text", cut.Markup, StringComparison.Ordinal);
            Assert.Equal("true", cut.Find("#language-tester-tab-stt").GetAttribute("aria-selected"));
            Assert.Equal("0", cut.Find("#language-tester-tab-stt").GetAttribute("tabindex"));
            Assert.Equal("-1", cut.Find("#language-tester-tab-tts").GetAttribute("tabindex"));
        });

        tablist.KeyDown(new KeyboardEventArgs { Key = "Home" });

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("true", cut.Find("#language-tester-tab-tts").GetAttribute("aria-selected"));
            Assert.Equal("0", cut.Find("#language-tester-tab-tts").GetAttribute("tabindex"));
            Assert.Equal("-1", cut.Find("#language-tester-tab-stt").GetAttribute("tabindex"));
        });
    }

    [Fact]
    public void WhenSwitchingTabs_RendersEachTab()
    {
        var supportedLanguagesClient = new Mock<ISupportedLanguagesClient>(MockBehavior.Strict);
        supportedLanguagesClient
            .Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportedLanguages(
                "en",
                [
                    new SupportedLanguage("de", "Deutsch"),
                    new SupportedLanguage("en", "English")
                ]));

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(Mock.Of<ISpeechToTextStreamingClient>());
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());

        var cut = Render<LanguageTester>();

        cut.WaitForAssertion(() => Assert.Contains("TTS", cut.Markup, StringComparison.Ordinal));

        cut.FindAll("button.nav-link").Single(b => b.TextContent.Contains("STT", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Contains("Speech To Text", cut.Markup, StringComparison.Ordinal));

        supportedLanguagesClient.Verify(client => client.GetAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
