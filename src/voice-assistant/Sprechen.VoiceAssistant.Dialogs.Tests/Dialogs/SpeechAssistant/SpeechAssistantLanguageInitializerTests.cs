using Moq;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantLanguageInitializerTests
{
    [Fact]
    public async Task InitializeAsync_WhenUserLanguageAlreadySelected_PersistsSelectedIsoCode()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        var workflow = CreateSettingsWorkflow(userLanguageState, "de");
        var initializer = new SpeechAssistantLanguageInitializer(CreateSetupServices(userLanguageState, workflow.Object));

        var selectedIsoCode = await initializer.InitializeAsync(CancellationToken.None);

        Assert.Equal("de", selectedIsoCode);
        workflow.Verify(w => w.SaveSelectedLanguageAsync("de", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WhenUserLanguageMissing_PersistsModeledDefaultLanguage()
    {
        var userLanguageState = new UserLanguageState();
        var expectedIsoCode = VoiceInterfaceModelCatalog.DefaultLanguage;
        var workflow = CreateSettingsWorkflow(userLanguageState, expectedIsoCode);
        var initializer = new SpeechAssistantLanguageInitializer(CreateSetupServices(userLanguageState, workflow.Object));

        var selectedIsoCode = await initializer.InitializeAsync(CancellationToken.None);

        Assert.Equal(expectedIsoCode, selectedIsoCode);
        Assert.Equal(expectedIsoCode, userLanguageState.SelectedIsoCode);
        workflow.Verify(w => w.SaveSelectedLanguageAsync(expectedIsoCode, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SpeechAssistantSetupDependencies CreateSetupServices(
        IUserLanguageState userLanguageState,
        IUserLanguageSettingsWorkflow userLanguageSettingsWorkflow)
    {
        return new SpeechAssistantSetupDependencies(
            userLanguageState,
            userLanguageSettingsWorkflow,
            Mock.Of<IWakeWordProvider>(),
            Mock.Of<IAcousticWakeWordDetectorConfigurationResolver>(),
            Mock.Of<IFormsCatalogDataClient>(),
            Mock.Of<ISessionEntryCancelVocabulary>(),
            new StudyTelemetryContext(), new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
    }

    private static Mock<IUserLanguageSettingsWorkflow> CreateSettingsWorkflow(
        UserLanguageState state,
        string expectedIsoCode)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedIsoCode);

        var workflow = new Mock<IUserLanguageSettingsWorkflow>(MockBehavior.Strict);
        workflow.Setup(w => w.SaveSelectedLanguageAsync(expectedIsoCode, It.IsAny<CancellationToken>()))
            .Returns((string? isoCode, CancellationToken _) => ValueTask.FromResult(SetLanguage(state, isoCode!)));

        return workflow;
    }

    private static UserLanguageSettings SetLanguage(UserLanguageState state, string isoCode)
    {
        state.SetSelectedIsoCode(isoCode);
        return new UserLanguageSettings(isoCode, state.SelectedPolitenessLevel);
    }
}
