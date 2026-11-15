using Bunit;
using Sprechen.Voice.App.Pages;

namespace Sprechen.Voice.App.Tests.Components;


public sealed class NotFoundCopyTests : TestContext
{
    [Fact]
    public void RendersGermanInvalidStudyLink_WhenSessionLanguageIsGerman()
    {
        var languageState = new UserLanguageState();
        languageState.SetSelectedIsoCode("de");
        Services.AddSingleton<IUserLanguageState>(languageState);

        var cut = Render<NotFound>();

        Assert.Equal("Nicht gefunden", cut.Find("h1").TextContent.Trim());
        Assert.Contains("existiert nicht", cut.Find("p").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Not Found", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("does not exist", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersEnglishInvalidStudyLink_WhenSessionLanguageIsEnglish()
    {
        var languageState = new UserLanguageState();
        languageState.SetSelectedIsoCode("en");
        Services.AddSingleton<IUserLanguageState>(languageState);

        var cut = Render<NotFound>();

        Assert.Equal("Not Found", cut.Find("h1").TextContent.Trim());
        Assert.Contains("does not exist", cut.Find("p").TextContent, StringComparison.Ordinal);
    }
}
