using Bunit;
using Moq;
using Sprechen.Web.SpeechComponents;
using Sprechen.Voice.App.Tests.TestDoubles;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class VoiceActivationSettingsTabTests : TestContext
{
    [Fact]
    public void WhenLoaded_RendersDefaultWakeWords()
    {
        var userLanguageState = new UserLanguageState();
        var settings = new InMemoryVoiceActivationSettings();

        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<ILanguageCatalog>(VoiceInterfaceLanguageCatalog.CreateDefault());
        Services.AddSingleton<IWakeWordSettingsWorkflow>(CreateWorkflow(userLanguageState, settings));

        var cut = Render<VoiceActivationSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            var items = cut.FindAll("li").Select(item => item.TextContent.Trim()).ToArray();

            Assert.Equal(["okay alex", "hey alex"], items);

            var editor = cut.Find("#wakewords-editor");
            var editorValue = editor.GetAttribute("value") ?? editor.TextContent;
            Assert.Contains("okay alex", editorValue, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void SaveAsync_WhenEditorContainsActivationForms_PersistsOverride()
    {
        var userLanguageState = new UserLanguageState();
        var settings = new InMemoryVoiceActivationSettings();

        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<ILanguageCatalog>(VoiceInterfaceLanguageCatalog.CreateDefault());
        Services.AddSingleton<IWakeWordSettingsWorkflow>(CreateWorkflow(userLanguageState, settings));

        var cut = Render<VoiceActivationSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Loading...", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("#wakewords-editor").Input("  Nova \n nova \n");
        cut.Find("#wakewords-save").Click();

        cut.WaitForAssertion(() =>
        {
            var overrideWords = settings.GetWakeWordActivationFormsOverrideAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();
            Assert.Equal(["Nova"], overrideWords);

            var editor = cut.Find("#wakewords-editor");
            var editorValue = editor.GetAttribute("value") ?? editor.TextContent;
            Assert.Equal("Nova", editorValue.Trim());
            Assert.Contains("Wake word override saved.", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ResetAsync_WhenInvoked_ClearsOverrideAndRestoresDefaults()
    {
        var userLanguageState = new UserLanguageState();
        var settings = new InMemoryVoiceActivationSettings();

        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<ILanguageCatalog>(VoiceInterfaceLanguageCatalog.CreateDefault());
        Services.AddSingleton<IWakeWordSettingsWorkflow>(CreateWorkflow(userLanguageState, settings));

        var cut = Render<VoiceActivationSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Loading...", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("#wakewords-editor").Input("Nova");
        cut.Find("#wakewords-save").Click();

        cut.Find("#wakewords-reset").Click();

        cut.WaitForAssertion(() =>
        {
            var overrideWords = settings.GetWakeWordActivationFormsOverrideAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();
            Assert.Empty(overrideWords);

            var editor = cut.Find("#wakewords-editor");
            var editorValue = editor.GetAttribute("value") ?? editor.TextContent;
            Assert.Contains("okay alex", editorValue, StringComparison.Ordinal);
            Assert.Contains("Wake word override reset to modeled defaults.", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void SaveAsync_WhenServiceFails_ShowsError()
    {
        var userLanguageState = new UserLanguageState();
        var voiceSettings = new Mock<IVoiceActivationSettings>();
        voiceSettings.Setup(settings => settings.GetWakeWordActivationFormsOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<string[]>([]));
        voiceSettings.Setup(settings => settings.SetWakeWordActivationFormsOverrideAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromException(new InvalidOperationException("boom")));

        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<ILanguageCatalog>(VoiceInterfaceLanguageCatalog.CreateDefault());
        Services.AddSingleton<IWakeWordSettingsWorkflow>(CreateWorkflow(userLanguageState, voiceSettings.Object));

        var cut = Render<VoiceActivationSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Loading...", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("#wakewords-editor").Input("Nova");
        cut.Find("#wakewords-save").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("boom", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void RunSpeechSetupAsync_WhenWorkflowPersistsOverride_ReloadsOverrideAndShowsStatus()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("en");

        var voiceActivationSettings = new InMemoryVoiceActivationSettings();
        Func<string, CancellationToken, ValueTask> runSpeechSetupAsync =
            async (isoCode, cancellationToken) =>
            {
                Assert.Equal("en", isoCode);
                await voiceActivationSettings
                    .SetWakeWordActivationFormsOverrideAsync(["alpha", "beta"], cancellationToken)
                    .ConfigureAwait(false);
            };

        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<ILanguageCatalog>(VoiceInterfaceLanguageCatalog.CreateDefault());
        Services.AddSingleton<IWakeWordSettingsWorkflow>(
            CreateWorkflow(userLanguageState, voiceActivationSettings, runSpeechSetupAsync));

        var cut = Render<VoiceActivationSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Loading...", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("#wakewords-speech-setup").Click();

        cut.WaitForAssertion(() =>
        {
            var overrideWords = voiceActivationSettings.GetWakeWordActivationFormsOverrideAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();
            Assert.Equal(["alpha", "beta"], overrideWords);

            var editor = cut.Find("#wakewords-editor");
            var editorValue = editor.GetAttribute("value") ?? editor.TextContent;
            Assert.Contains("alpha", editorValue, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("beta", editorValue, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Speech-guided wake word review finished.", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void WhenLoaded_RendersLocalizedSessionLanguageName()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("en");
        var settings = new InMemoryVoiceActivationSettings();

        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<ILanguageCatalog>(VoiceInterfaceLanguageCatalog.CreateDefault());
        Services.AddSingleton<IWakeWordSettingsWorkflow>(CreateWorkflow(userLanguageState, settings));

        var cut = Render<VoiceActivationSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            var sessionLanguage = cut.Find(".small.text-body-secondary.mb-2").TextContent.Trim();
            Assert.Contains("Session language:", sessionLanguage, StringComparison.Ordinal);
            Assert.Contains("English", sessionLanguage, StringComparison.Ordinal);
            Assert.DoesNotContain("Session language: en", sessionLanguage, StringComparison.Ordinal);
        });
    }

    private static IWakeWordSettingsWorkflow CreateWorkflow(
        IUserLanguageState userLanguageState,
        IVoiceActivationSettings voiceActivationSettings,
        Func<string, CancellationToken, ValueTask>? runSpeechSetupAsync = null)
    {
        return new InMemoryWakeWordSettingsWorkflow(
            userLanguageState,
            voiceActivationSettings,
            runSpeechSetupAsync);
    }
}
