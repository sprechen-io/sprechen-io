using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SettingsVoiceWorkflowTests
{
    [Fact]
    public async Task SaveSelectedLanguageAsync_WhenLanguageChanges_PersistsNormalizedLanguageAndExistingPoliteness()
    {
        var state = new UserLanguageState();
        state.SetSelectedIsoCode("de");
        state.SetSelectedPolitenessLevel(PolitenessLevel.Informal);

        var store = new Mock<IUserLanguageSettingsStore>(MockBehavior.Strict);

        // Beim Sprachwechsel die übrigen Einstellungen einschließlich der Markierung des Einstiegsangebots erhalten.
        store.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new UserLanguageSettings("de", PolitenessLevel.Informal, "de")));
        store.Setup(s => s.SetAsync(
                It.Is<UserLanguageSettings>(settings =>
                    settings.SelectedIsoCode == "en"
                    && settings.SelectedPolitenessLevel == PolitenessLevel.Informal
                    && settings.EntryOfferHeardIsoCode == "de"),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var workflow = new UserLanguageSettingsWorkflow(
            state,
            store.Object,
            new SettingsVoiceLanguageResolver());

        var result = await workflow.SaveSelectedLanguageAsync("EN", CancellationToken.None);

        Assert.Equal("en", result.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Informal, result.SelectedPolitenessLevel);
        Assert.Equal("en", state.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Informal, state.SelectedPolitenessLevel);
        store.VerifyAll();
    }

    [Fact]
    public async Task SaveSelectedPolitenessAsync_WhenLanguageIsMissing_UsesModeledDefaultLanguage()
    {
        var state = new UserLanguageState();
        var defaultLanguage = VoiceInterfaceModelCatalog.DefaultLanguage;

        var store = new Mock<IUserLanguageSettingsStore>(MockBehavior.Strict);
        store.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new UserLanguageSettings(null, null, "de")));
        store.Setup(s => s.SetAsync(
                It.Is<UserLanguageSettings>(settings =>
                    settings.SelectedIsoCode == defaultLanguage
                    && settings.SelectedPolitenessLevel == PolitenessLevel.Formal
                    && settings.EntryOfferHeardIsoCode == "de"),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var workflow = new UserLanguageSettingsWorkflow(
            state,
            store.Object,
            new SettingsVoiceLanguageResolver());

        var result = await workflow.SaveSelectedPolitenessAsync(PolitenessLevel.Formal, CancellationToken.None);

        Assert.Equal(defaultLanguage, result.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Formal, result.SelectedPolitenessLevel);
        Assert.Equal(defaultLanguage, state.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Formal, state.SelectedPolitenessLevel);
        store.VerifyAll();
    }

    [Fact]
    public void GetEarconGuideState_WhenPreferredIsoCodeIsProvided_UsesPreviewLanguageWithoutMutatingUserLanguageState()
    {
        var state = new UserLanguageState();
        state.SetSelectedIsoCode("de");

        var languageResolver = new SettingsVoiceLanguageResolver();
        var workflow = new EarconGuideWorkflow(
            state,
            Mock.Of<IEarconPlayer>(),
            languageResolver,
            new EarconGuideCatalog(languageResolver));

        var guide = workflow.GetEarconGuideState("en");

        Assert.Equal("en", guide.SelectedIsoCode);
        Assert.Equal("de", state.SelectedIsoCode);
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconsIntro, "en"),
            guide.IntroText);
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconFlowListening, "en"),
            guide.Items[0].Description);
        Assert.Equal(EarconId.FlowProcessing, guide.Items[1].EarconId);
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconFlowProcessing, "en"),
            guide.Items[1].Description);
    }

    private static string ResolveResourceText(string resourceId, string isoCode)
    {
        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text));
        return text;
    }
}
