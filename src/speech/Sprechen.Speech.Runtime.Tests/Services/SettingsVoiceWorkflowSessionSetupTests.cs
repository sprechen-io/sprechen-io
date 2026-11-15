using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SettingsVoiceWorkflowSessionSetupTests
{
    [Fact]
    public async Task SaveSessionSetupAsync_PersistsNormalizedSelectionAndUpdatesUserLanguageState()
    {
        var userLanguageState = new UserLanguageState();
        var capturedSettings = new UserLanguageSettings(null, null);

        var settingsStore = new Mock<IUserLanguageSettingsStore>(MockBehavior.Strict);

        // Sprache und Anrede ändern, ohne die gespeicherte Markierung des Einstiegsangebots zu verlieren.
        settingsStore
            .Setup(store => store.GetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new UserLanguageSettings(null, null, "de")));
        settingsStore
            .Setup(store => store.SetAsync(It.IsAny<UserLanguageSettings>(), It.IsAny<CancellationToken>()))
            .Callback<UserLanguageSettings, CancellationToken>((settings, _) => capturedSettings = settings)
            .Returns(ValueTask.CompletedTask);

        var workflow = CreateUserLanguageSettingsWorkflow(
            userLanguageState,
            settingsStore.Object);

        var state = await workflow.SaveSessionSetupAsync("EN", "informal", CancellationToken.None);

        Assert.Equal("en", state.SelectedIsoCode);
        Assert.Equal("informal", state.SelectedPolitenessValue);
        Assert.True(state.HasPolitenessProfile);
        Assert.Equal("en", userLanguageState.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Informal, userLanguageState.SelectedPolitenessLevel);
        Assert.Equal("en", capturedSettings.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Informal, capturedSettings.SelectedPolitenessLevel);
        Assert.Equal("de", capturedSettings.EntryOfferHeardIsoCode);
    }

    [Fact]
    public async Task LoadWakeWordConfigurationAsync_WhenNoOverrideExists_UsesModeledDefaultsForSelectedLanguage()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");

        var workflow = CreateWakeWordSettingsWorkflow(
            userLanguageState,
            new InMemoryVoiceActivationSettings(),
            new NoOpWakeWordSpeechSetupRunner());

        var state = await workflow.LoadWakeWordConfigurationAsync(
            preferredIsoCode: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal("de", state.SelectedIsoCode);
        Assert.Equal(VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage("de"), state.DefaultWakeWordActivationForms);
        Assert.Empty(state.OverrideWakeWordActivationForms);
        Assert.Equal("okay alex" + Environment.NewLine + "hey alex", state.EditorText.Trim());
    }

    [Fact]
    public async Task SaveWakeWordConfigurationAsync_WhenEditorContainsDuplicates_NormalizesAndPersistsOverride()
    {
        var userLanguageState = new UserLanguageState();
        var voiceActivationSettings = new InMemoryVoiceActivationSettings();
        var workflow = CreateWakeWordSettingsWorkflow(
            userLanguageState,
            voiceActivationSettings,
            new NoOpWakeWordSpeechSetupRunner());

        var state = await workflow.SaveWakeWordConfigurationAsync(
            editorText: "  Nova \n nova \n",
            preferredIsoCode: null,
            cancellationToken: CancellationToken.None);
        var overrideWords = await voiceActivationSettings.GetWakeWordActivationFormsOverrideAsync(CancellationToken.None);

        Assert.Equal(["Nova"], overrideWords);
        Assert.Equal(["Nova"], state.OverrideWakeWordActivationForms);
        Assert.Equal("Nova", state.EditorText.Trim());
    }

    [Fact]
    public async Task RunWakeWordSpeechSetupAsync_WhenSpeechSetupPersistsOverride_ReloadsUpdatedState()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("en");

        var voiceActivationSettings = new InMemoryVoiceActivationSettings();
        var wakeWordSpeechSetupRunner = new DelegatingWakeWordSpeechSetupRunner(
            async (isoCode, cancellationToken) =>
            {
                Assert.Equal("en", isoCode);
                await voiceActivationSettings
                    .SetWakeWordActivationFormsOverrideAsync(["alpha", "beta"], cancellationToken)
                    .ConfigureAwait(false);
            });

        var workflow = CreateWakeWordSettingsWorkflow(
            userLanguageState,
            voiceActivationSettings,
            wakeWordSpeechSetupRunner);

        var state = await workflow.RunWakeWordSpeechSetupAsync(
            preferredIsoCode: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal("en", state.SelectedIsoCode);
        Assert.Equal(["alpha", "beta"], state.OverrideWakeWordActivationForms);
        Assert.Contains("alpha", state.EditorText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("beta", state.EditorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetEarconGuideState_UsesModeledIntroAndCanonicalEarconSequence()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("en");

        var workflow = CreateEarconGuideWorkflow(userLanguageState);

        var state = workflow.GetEarconGuideState();

        Assert.Equal("en", state.SelectedIsoCode);
        Assert.Equal(ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconsIntro, "en"), state.IntroText);
        Assert.Equal(
            [
                EarconId.FlowListening,
                EarconId.FlowProcessing,
                EarconId.ValidationSuccess,
                // Die Anleitung beschränkt sich auf die hier vorgesehenen Signale des Sprachablaufs.
                EarconId.VoiceInputMicOff,
                EarconId.VoiceInputMicOn
            ],
            state.Items.Select(item => item.EarconId).ToArray());
        Assert.Equal(
            [
                ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconFlowListening, "en"),
                ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconFlowProcessing, "en"),
                ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconValidationSuccess, "en"),
                ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconVoiceInputMicOff, "en"),
                ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconVoiceInputMicOn, "en")
            ],
            state.Items.Select(item => item.Description).ToArray());
    }

    private static UserLanguageSettingsWorkflow CreateUserLanguageSettingsWorkflow(
        IUserLanguageState userLanguageState,
        IUserLanguageSettingsStore settingsStore)
    {
        return new UserLanguageSettingsWorkflow(
            userLanguageState,
            settingsStore,
            new SettingsVoiceLanguageResolver());
    }

    private static WakeWordSettingsWorkflow CreateWakeWordSettingsWorkflow(
        IUserLanguageState userLanguageState,
        IVoiceActivationSettings voiceActivationSettings,
        IWakeWordSpeechSetupRunner wakeWordSpeechSetupRunner)
    {
        return new WakeWordSettingsWorkflow(
            userLanguageState,
            voiceActivationSettings,
            wakeWordSpeechSetupRunner,
            new SettingsVoiceLanguageResolver());
    }

    private static EarconGuideWorkflow CreateEarconGuideWorkflow(IUserLanguageState userLanguageState)
    {
        var languageResolver = new SettingsVoiceLanguageResolver();
        return new EarconGuideWorkflow(
            userLanguageState,
            Mock.Of<IEarconPlayer>(),
            languageResolver,
            new EarconGuideCatalog(languageResolver));
    }

    private static string ResolveResourceText(string resourceId, string isoCode)
    {
        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text));
        return text;
    }
}
