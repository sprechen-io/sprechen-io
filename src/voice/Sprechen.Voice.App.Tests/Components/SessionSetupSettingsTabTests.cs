using Bunit;
using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Web.SpeechComponents;
using Sprechen.Voice.App.Tests.TestDoubles;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class SessionSetupSettingsTabTests : TestContext
{
    private static readonly VoiceInterfaceLanguageCatalog LanguageCatalog = VoiceInterfaceLanguageCatalog.CreateDefault();

    [Fact]
    public void SaveAsync_PersistsSessionSetupAndUpdatesUserLanguageState()
    {
        var userLanguageState = new UserLanguageState();
        var capturedSettings = new UserLanguageSettings(null, null);

        var settingsStore = new Mock<IUserLanguageSettingsStore>(MockBehavior.Strict);
        settingsStore
            .Setup(store => store.SetAsync(It.IsAny<UserLanguageSettings>(), It.IsAny<CancellationToken>()))
            .Callback<UserLanguageSettings, CancellationToken>((settings, _) => capturedSettings = settings)
            .Returns(ValueTask.CompletedTask);

        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<ISessionSetupSettingsWorkflow>(CreateWorkflow(userLanguageState, settingsStore.Object));

        var cut = Render<SessionSetupSettingsTab>();

        cut.Find("#session-setup-language").Change("en");
        cut.Find("#session-setup-politeness").Change("informal");
        cut.Find("button.btn.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("en", userLanguageState.SelectedIsoCode);
            Assert.Equal(PolitenessLevel.Informal, userLanguageState.SelectedPolitenessLevel);
            Assert.Equal("en", capturedSettings.SelectedIsoCode);
            Assert.Equal(PolitenessLevel.Informal, capturedSettings.SelectedPolitenessLevel);
            Assert.Contains(
                ResolveResourceText(VoiceInterfaceResourceIds.SettingsSessionSetupSaveSuccess, "en"),
                cut.Markup,
                StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Render_WhenSelectedLanguageChanges_KeepsLanguageEndonymsVisible()
    {
        var userLanguageState = new UserLanguageState();

        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<ISessionSetupSettingsWorkflow>(
            CreateWorkflow(userLanguageState, Mock.Of<IUserLanguageSettingsStore>()));

        var cut = Render<SessionSetupSettingsTab>();

        cut.Find("#session-setup-language").Change("en");

        cut.WaitForAssertion(() =>
        {
            var optionTexts = cut.FindAll("#session-setup-language option")
                .Select(option => option.TextContent.Trim())
                .ToArray();
            var expectedEndonyms = VoiceInterfaceModelCatalog.SupportedLanguages
                .OrderBy(GetEndonym, StringComparer.InvariantCultureIgnoreCase)
                .Select(GetEndonym)
                .ToArray();
            var savedLanguageSummary = cut.Find(".small.text-body-secondary").Children[0].TextContent.Trim();

            Assert.Equal(expectedEndonyms, optionTexts);
            Assert.DoesNotContain(optionTexts, option => option.Contains("(en)", StringComparison.Ordinal));
            Assert.DoesNotContain(optionTexts, option => option.Contains("(de)", StringComparison.Ordinal));
            Assert.Equal(
                ResolveTemplate(
                    VoiceInterfaceResourceIds.SettingsSessionSetupSavedLanguageSummary,
                    displayIsoCode: "en",
                    placeholder: "{languageName}",
                    replacement: LanguageCatalog.ResolveDisplayName("en", "en")),
                savedLanguageSummary);
        });
    }

    private static string GetEndonym(string isoCode)
    {
        return LanguageCatalog.GetEndonym(LanguageCode.ParseIso6391(isoCode));
    }

    private static string ResolveResourceText(string resourceId, string isoCode)
    {
        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text));
        return text;
    }

    private static string ResolveTemplate(string resourceId, string displayIsoCode, string placeholder, string replacement)
    {
        return ResolveResourceText(resourceId, displayIsoCode).Replace(placeholder, replacement, StringComparison.Ordinal);
    }

    private static ISessionSetupSettingsWorkflow CreateWorkflow(
        IUserLanguageState userLanguageState,
        IUserLanguageSettingsStore settingsStore)
    {
        return new InMemorySessionSetupSettingsWorkflow(
            userLanguageState,
            settingsStore);
    }
}
