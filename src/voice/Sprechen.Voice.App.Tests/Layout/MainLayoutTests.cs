using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Voice.App.Layout;
using Sprechen.Voice.App.Tests.TestDoubles;

namespace Sprechen.Voice.App.Tests.Layout;

public sealed class MainLayoutTests : TestContext
{
    public MainLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<LayoutView> RenderLayout()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton(Mock.Of<IEarconPlayer>());
        Services.AddSingleton(Mock.Of<IThemeSettings>());
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(Mock.Of<IBootStatusBrowserBridge>());

        return Render<LayoutView>(parameters => parameters
            .Add(p => p.Layout, typeof(MainLayout))
            .Add<RenderFragment>(p => p.ChildContent, builder => builder.AddContent(0, "Body")));
    }

    [Fact]
    public void WhenFirstRendered_MarksApplicationReadyToRemoveHostPlaceholder()
    {
        var bootStatus = new Mock<IBootStatusBrowserBridge>();
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton(Mock.Of<IEarconPlayer>());
        Services.AddSingleton(Mock.Of<IThemeSettings>());
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(bootStatus.Object);

        _ = Render<LayoutView>(parameters => parameters
            .Add(p => p.Layout, typeof(MainLayout))
            .Add<RenderFragment>(p => p.ChildContent, builder => builder.AddContent(0, "Body")));

        bootStatus.Verify(b => b.MarkApplicationReadyAsync(), Times.Once);
    }

    [Fact]
    public void DoesNotRenderWasmOnlyBlazorErrorUiToast()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton(Mock.Of<IEarconPlayer>());
        Services.AddSingleton(Mock.Of<IThemeSettings>());
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(Mock.Of<IBootStatusBrowserBridge>());

        var cut = Render<LayoutView>(parameters => parameters
            .Add(p => p.Layout, typeof(MainLayout))
            .Add<RenderFragment>(p => p.ChildContent, builder => builder.AddContent(0, "Body")));

        Assert.DoesNotContain("blazor-error-ui", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("An unhandled error has occurred.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersLocalizedChrome_UnderGermanLanguageState_WithoutEnglishLiterals()
    {
        var languageState = new UserLanguageState();
        languageState.SetSelectedIsoCode("de");

        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton<IUserLanguageState>(languageState);
        Services.AddSingleton(Mock.Of<IEarconPlayer>());
        Services.AddSingleton(Mock.Of<IThemeSettings>());
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(Mock.Of<IBootStatusBrowserBridge>());

        var cut = Render<LayoutView>(parameters => parameters
            .Add(p => p.Layout, typeof(MainLayout))
            .Add<RenderFragment>(p => p.ChildContent, builder => builder.AddContent(0, "Body")));

        Assert.Equal("Navigation öffnen", cut.Find("button.nav-open-button").GetAttribute("aria-label"));
        Assert.Equal("Navigation schließen", cut.Find("button.nav-close-button").GetAttribute("aria-label"));

        var navMarkup = cut.Markup;
        Assert.Contains("Startseite", navMarkup, StringComparison.Ordinal);
        Assert.Contains("Sprachtester", navMarkup, StringComparison.Ordinal);
        Assert.Contains("Einstellungen", navMarkup, StringComparison.Ordinal);

        Assert.DoesNotContain("Open navigation", navMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("Close navigation", navMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("Language Tester", navMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenNavigationOpensAndUrlChanges_ClosesDrawer()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton(Mock.Of<IEarconPlayer>());
        Services.AddSingleton(Mock.Of<IThemeSettings>());
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(Mock.Of<IBootStatusBrowserBridge>());

        var cut = Render<LayoutView>(parameters => parameters
            .Add(p => p.Layout, typeof(MainLayout))
            .Add<RenderFragment>(p => p.ChildContent, builder => builder.AddContent(0, "Body")));

        cut.Find("button.nav-open-button").Click();

        cut.WaitForAssertion(() =>
        {
            var drawer = cut.Find("#nav-drawer");
            Assert.Contains("show", drawer.ClassList, StringComparer.OrdinalIgnoreCase);
        });

        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/language-tester");

        cut.WaitForAssertion(() =>
        {
            var drawer = cut.Find("#nav-drawer");
            Assert.DoesNotContain("show", drawer.ClassList, StringComparer.OrdinalIgnoreCase);
            Assert.NotNull(cut.Find("button.nav-open-button"));
        });
    }

    [Fact]
    public void WhenUrlChangesWithDrawerClosed_KeepsDrawerClosed()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton(Mock.Of<IEarconPlayer>());
        Services.AddSingleton(Mock.Of<IThemeSettings>());
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(Mock.Of<IBootStatusBrowserBridge>());

        var cut = Render<LayoutView>(parameters => parameters
            .Add(p => p.Layout, typeof(MainLayout))
            .Add<RenderFragment>(p => p.ChildContent, builder => builder.AddContent(0, "Body")));

        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/language-tester");

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("show", cut.Find("#nav-drawer").ClassList, StringComparer.OrdinalIgnoreCase);
            Assert.NotNull(cut.Find("button.nav-open-button"));
        });
    }

    [Fact]
    public void WhenDrawerOpenAndCloseButtonClicked_ClosesDrawer()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton(Mock.Of<IEarconPlayer>());
        Services.AddSingleton(Mock.Of<IThemeSettings>());
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(Mock.Of<IBootStatusBrowserBridge>());

        var cut = Render<LayoutView>(parameters => parameters
            .Add(p => p.Layout, typeof(MainLayout))
            .Add<RenderFragment>(p => p.ChildContent, builder => builder.AddContent(0, "Body")));

        cut.Find("button.nav-open-button").Click();

        cut.WaitForAssertion(() => Assert.Contains("show", cut.Find("#nav-drawer").ClassList, StringComparer.OrdinalIgnoreCase));

        cut.Find("button.nav-close-button").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("show", cut.Find("#nav-drawer").ClassList, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void WhenDrawerOpen_KeepsOpenButtonInDomHiddenWithAriaExpandedTrue()
    {
        var cut = RenderLayout();

        var openButton = cut.Find("button.nav-open-button");
        Assert.Equal("false", openButton.GetAttribute("aria-expanded"));
        Assert.DoesNotContain("is-hidden", openButton.ClassList);

        cut.Find("button.nav-open-button").Click();

        cut.WaitForAssertion(() =>
        {
            var open = cut.Find("button.nav-open-button");
            Assert.Equal("true", open.GetAttribute("aria-expanded"));
            Assert.Contains("is-hidden", open.ClassList);
            Assert.Contains("show", cut.Find("#nav-drawer").ClassList, StringComparer.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void WhenDrawerOpenAndEscapePressed_ClosesDrawerAndRestoresOpenButton()
    {
        var cut = RenderLayout();

        cut.Find("button.nav-open-button").Click();

        cut.WaitForAssertion(() => Assert.Contains("show", cut.Find("#nav-drawer").ClassList, StringComparer.OrdinalIgnoreCase));

        cut.Find("#nav-drawer").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("show", cut.Find("#nav-drawer").ClassList, StringComparer.OrdinalIgnoreCase);
            var open = cut.Find("button.nav-open-button");
            Assert.Equal("false", open.GetAttribute("aria-expanded"));
            Assert.DoesNotContain("is-hidden", open.ClassList);
        });
    }

    [Fact]
    public void WhenNonEscapeKeyPressedOnDrawer_KeepsDrawerOpen()
    {
        var cut = RenderLayout();

        cut.Find("button.nav-open-button").Click();

        cut.WaitForAssertion(() => Assert.Contains("show", cut.Find("#nav-drawer").ClassList, StringComparer.OrdinalIgnoreCase));

        cut.Find("#nav-drawer").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        cut.WaitForAssertion(() => Assert.Contains("show", cut.Find("#nav-drawer").ClassList, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void WhenDrawerOpenAndBackdropClicked_ClosesDrawer()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton(Mock.Of<IEarconPlayer>());
        Services.AddSingleton(Mock.Of<IThemeSettings>());
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(Mock.Of<IBootStatusBrowserBridge>());

        var cut = Render<LayoutView>(parameters => parameters
            .Add(p => p.Layout, typeof(MainLayout))
            .Add<RenderFragment>(p => p.ChildContent, builder => builder.AddContent(0, "Body")));

        cut.Find("button.nav-open-button").Click();

        cut.WaitForAssertion(() => Assert.Contains("show", cut.Find("#nav-drawer").ClassList, StringComparer.OrdinalIgnoreCase));

        cut.Find("div.nav-backdrop").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("show", cut.Find("#nav-drawer").ClassList, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void RendersAppearanceSwitchInDrawer_LocalizedUnderGerman()
    {
        var languageState = new UserLanguageState();
        languageState.SetSelectedIsoCode("de");

        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton<IUserLanguageState>(languageState);
        Services.AddSingleton(Mock.Of<IEarconPlayer>());
        Services.AddSingleton(Mock.Of<IThemeSettings>());
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(Mock.Of<IBootStatusBrowserBridge>());

        var cut = Render<LayoutView>(parameters => parameters
            .Add(p => p.Layout, typeof(MainLayout))
            .Add<RenderFragment>(p => p.ChildContent, builder => builder.AddContent(0, "Body")));

        var switchMarkup = cut.Find("fieldset.theme-switch").OuterHtml;
        Assert.Contains("Erscheinungsbild", switchMarkup, StringComparison.Ordinal);
        Assert.Contains("Hell", switchMarkup, StringComparison.Ordinal);
        Assert.Contains("Dunkel", switchMarkup, StringComparison.Ordinal);
        Assert.NotNull(cut.Find("#nav-theme-option-system"));
        Assert.NotNull(cut.Find("#nav-theme-option-light"));
        Assert.NotNull(cut.Find("#nav-theme-option-dark"));
    }

    [Fact]
    public void WhenDarkChosenInDrawer_PersistsDarkThemeMode()
    {
        var themeSettings = new Mock<IThemeSettings>();
        themeSettings
            .Setup(settings => settings.GetAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<ThemeMode>(ThemeMode.System));
        ThemeMode? persisted = null;
        themeSettings
            .Setup(settings => settings.SetAsync(It.IsAny<ThemeMode>(), It.IsAny<CancellationToken>()))
            .Callback<ThemeMode, CancellationToken>((mode, _) => persisted = mode)
            .Returns(ValueTask.CompletedTask);

        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton(Mock.Of<IEarconPlayer>());
        Services.AddSingleton(themeSettings.Object);
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(Mock.Of<IBootStatusBrowserBridge>());

        var cut = Render<LayoutView>(parameters => parameters
            .Add(p => p.Layout, typeof(MainLayout))
            .Add<RenderFragment>(p => p.ChildContent, builder => builder.AddContent(0, "Body")));

        cut.Find("#nav-theme-option-dark").Change(true);

        cut.WaitForAssertion(() => Assert.Equal(ThemeMode.Dark, persisted));
    }
}
