using Bunit;
using Sprechen.Voice.App.Layout;

namespace Sprechen.Voice.App.Tests.Layout;

public sealed class NavMenuTests : TestContext
{
    [Fact]
    public void RendersLocalizedLabels_UnderGermanLanguageState()
    {
        var languageState = new UserLanguageState();
        languageState.SetSelectedIsoCode("de");
        Services.AddSingleton<IUserLanguageState>(languageState);

        var cut = Render<NavMenu>();

        var markup = cut.Markup;
        Assert.Contains("Startseite", markup, StringComparison.Ordinal);
        Assert.Contains("Sprachtester", markup, StringComparison.Ordinal);
        Assert.Contains("Einstellungen", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenSelectedLanguageChanges_ReRendersLabelsInNewLanguage()
    {
        var languageState = new UserLanguageState();
        languageState.SetSelectedIsoCode("de");
        Services.AddSingleton<IUserLanguageState>(languageState);

        var cut = Render<NavMenu>();
        Assert.Contains("Sprachtester", cut.Markup, StringComparison.Ordinal);

        // Das Navigationsmenü bleibt beim Sprachwechsel bestehen und muss seine Texte aktualisieren.

        cut.InvokeAsync(() => languageState.SetSelectedIsoCode("en"));

        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.Contains("Home", markup, StringComparison.Ordinal);
            Assert.Contains("Language Tester", markup, StringComparison.Ordinal);
            Assert.Contains("Settings", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Sprachtester", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Einstellungen", markup, StringComparison.Ordinal);
        });
    }
}
