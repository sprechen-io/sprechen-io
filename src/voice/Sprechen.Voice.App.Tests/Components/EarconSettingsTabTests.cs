using Bunit;
using Moq;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Web.SpeechComponents;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class EarconSettingsTabTests : TestContext
{
    [Fact]
    public void PreviewButton_WhenPressed_InvokesEarconPreview()
    {
        var userLanguageState = new UserLanguageState();
        var workflow = new Mock<IEarconGuideWorkflow>(MockBehavior.Strict);
        workflow
            .Setup(owner => owner.GetEarconGuideState(It.IsAny<string?>()))
            .Returns(new EarconGuideState(
                "de",
                "Earcons intro.",
                [new EarconGuideItem(EarconId.FlowListening, "Listening cue.")]));
        workflow
            .Setup(owner => owner.PreviewEarconAsync(EarconId.FlowListening, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflow
            .Setup(owner => owner.StopEarconPreviewsAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<ILanguageCatalog>(VoiceInterfaceLanguageCatalog.CreateDefault());
        Services.AddSingleton(workflow.Object);

        var cut = Render<EarconSettingsTab>();

        Assert.Contains("Earcons intro.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Listening cue.", cut.Markup, StringComparison.Ordinal);

        cut.Find("#earcon-preview-flow-listening").Click();
        cut.Find("#earcon-stop-all").Click();

        workflow.Verify(owner => owner.PreviewEarconAsync(EarconId.FlowListening, It.IsAny<CancellationToken>()), Times.Once);
        workflow.Verify(owner => owner.StopEarconPreviewsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(EarconId.VoiceInputMicOff, "earcon-preview-voice-input-mic-off")]
    [InlineData(EarconId.VoiceInputMicOn, "earcon-preview-voice-input-mic-on")]
    public void PreviewButton_WhenVoiceInputToggleCueIsPressed_InvokesEarconPreview(
        EarconId earconId,
        string previewButtonId)
    {
        var userLanguageState = new UserLanguageState();
        var workflow = new Mock<IEarconGuideWorkflow>(MockBehavior.Strict);
        workflow
            .Setup(owner => owner.GetEarconGuideState(It.IsAny<string?>()))
            .Returns(new EarconGuideState(
                "de",
                "Earcons intro.",
                [new EarconGuideItem(earconId, "Toggle cue.")]));
        workflow
            .Setup(owner => owner.PreviewEarconAsync(earconId, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<ILanguageCatalog>(VoiceInterfaceLanguageCatalog.CreateDefault());
        Services.AddSingleton(workflow.Object);

        var cut = Render<EarconSettingsTab>();

        cut.Find($"#{previewButtonId}").Click();

        workflow.Verify(owner => owner.PreviewEarconAsync(earconId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void PreviewButton_WhenPreviewFails_ShowsWarning()
    {
        var userLanguageState = new UserLanguageState();
        var workflow = new Mock<IEarconGuideWorkflow>(MockBehavior.Strict);
        workflow
            .Setup(owner => owner.GetEarconGuideState(It.IsAny<string?>()))
            .Returns(new EarconGuideState(
                "de",
                "Earcons intro.",
                [new EarconGuideItem(EarconId.FlowListening, "Listening cue.")]));
        workflow
            .Setup(owner => owner.PreviewEarconAsync(EarconId.FlowListening, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("preview failed"));
        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<ILanguageCatalog>(VoiceInterfaceLanguageCatalog.CreateDefault());
        Services.AddSingleton(workflow.Object);

        var cut = Render<EarconSettingsTab>();

        cut.Find("#earcon-preview-flow-listening").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("preview failed", cut.Find(".alert").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void StopAllButton_WhenStopFails_ShowsWarning()
    {
        var userLanguageState = new UserLanguageState();
        var workflow = new Mock<IEarconGuideWorkflow>(MockBehavior.Strict);
        workflow
            .Setup(owner => owner.GetEarconGuideState(It.IsAny<string?>()))
            .Returns(new EarconGuideState(
                "de",
                "Earcons intro.",
                [new EarconGuideItem(EarconId.FlowListening, "Listening cue.")]));
        workflow
            .Setup(owner => owner.StopEarconPreviewsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("stop failed"));
        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<ILanguageCatalog>(VoiceInterfaceLanguageCatalog.CreateDefault());
        Services.AddSingleton(workflow.Object);

        var cut = Render<EarconSettingsTab>();

        cut.Find("#earcon-stop-all").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("stop failed", cut.Find(".alert").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void WhenSelectedLanguageChanges_ReloadsEarconGuideState()
    {
        var userLanguageState = new UserLanguageState();
        var workflow = new Mock<IEarconGuideWorkflow>(MockBehavior.Strict);
        workflow
            .SetupSequence(owner => owner.GetEarconGuideState(It.IsAny<string?>()))
            .Returns(new EarconGuideState(
                "de",
                "Intro de.",
                [new EarconGuideItem(EarconId.FlowListening, "Beschreibung de.")]))
            .Returns(new EarconGuideState(
                "en",
                "Intro en.",
                [new EarconGuideItem(EarconId.FlowListening, "Description en.")]));
        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<ILanguageCatalog>(VoiceInterfaceLanguageCatalog.CreateDefault());
        Services.AddSingleton(workflow.Object);

        var cut = Render<EarconSettingsTab>();

        userLanguageState.SetSelectedIsoCode("en");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Intro en.", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Description en.", cut.Markup, StringComparison.Ordinal);
            Assert.Contains(
                ResolveTemplate(
                    VoiceInterfaceResourceIds.SettingsSessionLanguageSummary,
                    displayIsoCode: "en",
                    placeholder: "{languageName}",
                    replacement: VoiceInterfaceLanguageCatalog.CreateDefault().ResolveDisplayName("en", "en")),
                cut.Find(".small.text-body-secondary.mt-2").TextContent.Trim(),
                StringComparison.Ordinal);
        });
    }

    private static string ResolveTemplate(string resourceId, string displayIsoCode, string placeholder, string replacement)
    {
        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, displayIsoCode, out var text));
        return text.Replace(placeholder, replacement, StringComparison.Ordinal);
    }
}
