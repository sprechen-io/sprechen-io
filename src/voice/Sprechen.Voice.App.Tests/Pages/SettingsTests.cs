using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Web.SpeechComponents;
using Sprechen.Voice.App.Pages;
using Sprechen.Voice.App.Tests.TestDoubles;
using Xunit;

namespace Sprechen.Voice.App.Tests.Pages;

public sealed class SettingsTests
{
    [Fact]
    public void RendersTabsAndSwitchesActiveTab()
    {
        using var ctx = new TestContext();
        ctx.ComponentFactories.AddStub<AudioSettingsTab>();
        ctx.ComponentFactories.AddStub<SessionSetupSettingsTab>("<div id=\"stub-session-setup\"></div>");
        ctx.ComponentFactories.AddStub<EarconSettingsTab>("<div id=\"stub-earcon-settings\"></div>");
        ctx.ComponentFactories.AddStub<VoiceDialogSettingsTab>();
        ctx.ComponentFactories.AddStub<VoiceActivationSettingsTab>();
        ctx.ComponentFactories.AddStub<SpeechCaptureSettingsTab>();
        ctx.Services.AddSingleton<IUserLanguageState>(new UserLanguageState());

        var cut = ctx.Render<Settings>();

        Assert.Equal(2, cut.FindAll("[role='tab']").Count);
        Assert.Contains("active", cut.Find("#settings-tab-audio").ClassList);
        Assert.DoesNotContain("active", cut.Find("#settings-tab-voice").ClassList);

        cut.Find("#settings-tab-voice").Click();

        Assert.DoesNotContain("active", cut.Find("#settings-tab-audio").ClassList);
        Assert.Contains("active", cut.Find("#settings-tab-voice").ClassList);

        var voicePanel = cut.Find("#settings-panel-voice");
        Assert.Contains("show", voicePanel.ClassList);
        Assert.Contains("active", voicePanel.ClassList);
        Assert.NotNull(cut.Find("#stub-session-setup"));
        Assert.NotNull(cut.Find("#stub-earcon-settings"));
    }

    [Fact]
    public void Tablist_ArrowHomeEndKeys_MoveActiveTabAndUpdateAriaAndRovingTabindex()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.ComponentFactories.AddStub<AudioSettingsTab>();
        ctx.ComponentFactories.AddStub<SessionSetupSettingsTab>("<div id=\"stub-session-setup\"></div>");
        ctx.ComponentFactories.AddStub<EarconSettingsTab>("<div id=\"stub-earcon-settings\"></div>");
        ctx.ComponentFactories.AddStub<VoiceDialogSettingsTab>();
        ctx.ComponentFactories.AddStub<VoiceActivationSettingsTab>();
        ctx.ComponentFactories.AddStub<SpeechCaptureSettingsTab>();
        ctx.Services.AddSingleton<IUserLanguageState>(new UserLanguageState());

        var cut = ctx.Render<Settings>();
        var tablist = cut.Find("ul.nav-tabs");

        Assert.Equal("true", cut.Find("#settings-tab-audio").GetAttribute("aria-selected"));
        Assert.Equal("0", cut.Find("#settings-tab-audio").GetAttribute("tabindex"));
        Assert.Equal("-1", cut.Find("#settings-tab-voice").GetAttribute("tabindex"));

        tablist.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("active", cut.Find("#settings-tab-voice").ClassList);
            Assert.Equal("true", cut.Find("#settings-tab-voice").GetAttribute("aria-selected"));
            Assert.Equal("false", cut.Find("#settings-tab-audio").GetAttribute("aria-selected"));
            Assert.Equal("0", cut.Find("#settings-tab-voice").GetAttribute("tabindex"));
            Assert.Equal("-1", cut.Find("#settings-tab-audio").GetAttribute("tabindex"));
        });

        tablist.KeyDown(new KeyboardEventArgs { Key = "End" });
        cut.WaitForAssertion(() => Assert.Contains("active", cut.Find("#settings-tab-voice").ClassList));

        tablist.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        cut.WaitForAssertion(() => Assert.Contains("active", cut.Find("#settings-tab-audio").ClassList));

        tablist.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        cut.WaitForAssertion(() => Assert.Contains("active", cut.Find("#settings-tab-voice").ClassList));

        tablist.KeyDown(new KeyboardEventArgs { Key = "Home" });
        cut.WaitForAssertion(() => Assert.Contains("active", cut.Find("#settings-tab-audio").ClassList));
    }

    [Fact]
    public void VoiceTab_WhenSessionSetupLanguageChanges_EarconGuideFollowsDraftSelectionBeforeSave()
    {
        using var ctx = new TestContext();
        ctx.ComponentFactories.AddStub<AudioSettingsTab>();
        ctx.ComponentFactories.AddStub<VoiceDialogSettingsTab>();
        ctx.ComponentFactories.AddStub<VoiceActivationSettingsTab>();
        ctx.ComponentFactories.AddStub<SpeechCaptureSettingsTab>();

        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        UserLanguageSettings? capturedSettings = null;
        var settingsStore = new Mock<IUserLanguageSettingsStore>(MockBehavior.Strict);
        settingsStore
            .Setup(store => store.SetAsync(It.IsAny<UserLanguageSettings>(), It.IsAny<CancellationToken>()))
            .Callback<UserLanguageSettings, CancellationToken>((settings, _) => capturedSettings = settings)
            .Returns(ValueTask.CompletedTask);

        var sessionSetupWorkflow = new InMemorySessionSetupSettingsWorkflow(
            userLanguageState,
            settingsStore.Object);
        var earconGuideWorkflow = new InMemoryEarconGuideWorkflow(userLanguageState);

        ctx.Services.AddSingleton<IUserLanguageState>(userLanguageState);
        ctx.Services.AddSingleton<ISessionSetupSettingsWorkflow>(sessionSetupWorkflow);
        ctx.Services.AddSingleton<IEarconGuideWorkflow>(earconGuideWorkflow);

        var germanIntro = ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconsIntro, "de");
        var englishIntro = ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconsIntro, "en");
        var englishFlowListening = ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconFlowListening, "en");

        var cut = ctx.Render<Settings>();
        cut.Find("#settings-tab-voice").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(germanIntro, cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("#session-setup-language").Change("en");

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain(germanIntro, cut.Markup, StringComparison.Ordinal);
            Assert.Contains(englishIntro, cut.Markup, StringComparison.Ordinal);
            Assert.Contains(englishFlowListening, cut.Markup, StringComparison.Ordinal);
            Assert.Equal("de", userLanguageState.SelectedIsoCode);
            Assert.Null(capturedSettings);
        });

        cut.Find("button.btn.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("en", userLanguageState.SelectedIsoCode);
            Assert.Equal("en", capturedSettings?.SelectedIsoCode);
            Assert.Contains(
                ResolveResourceText(VoiceInterfaceResourceIds.SettingsSessionSetupSaveSuccess, "en"),
                cut.Markup,
                StringComparison.Ordinal);
            Assert.Contains(englishIntro, cut.Markup, StringComparison.Ordinal);
        });
    }

    private static string ResolveResourceText(string resourceId, string isoCode)
    {
        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text));
        return text;
    }
}
