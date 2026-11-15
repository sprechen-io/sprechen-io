using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Moq;
using Sprechen.Voice.App.Pages;
using Sprechen.Voice.App.Tests.TestDoubles;
using VoiceAssistantComponent = Sprechen.Web.SpeechComponents.Components.VoiceAssistant;

namespace Sprechen.Voice.App.Tests.Components;

/// <summary>
/// Prüft den deutschen Hinweis auf ungültige Studienlinks über <see cref="Home"/>
/// mit der eingebetteten VoiceAssistant-Komponente und deutscher Sitzungssprache.
/// </summary>
public sealed class HomeMalformedStudyLinkCopyTests : TestContext
{
    public HomeMalformedStudyLinkCopyTests()
    {
        var languageState = new UserLanguageState();
        languageState.SetSelectedIsoCode("de");
        Services.AddSingleton<IUserLanguageState>(languageState);
        Services.AddSingleton<IWakeWordProvider>(new FixedWakeWordProvider());
    }

    [Fact]
    public void MalformedUmfragePath_RendersGermanInvalidStudyLink_FromSessionLanguage()
    {
        var speechAssistantSessionController = new Mock<ISpeechAssistantSessionController>();
        var studyTelemetryContext = new StudyTelemetryContext();
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        _ = Services.AddSpeechInteractionSignals(messageBus);

        Services.AddSingleton<NavigationManager>(
            new FixedNavigationManager("https://localhost/", "https://localhost/umfrage/sh0rt"));
        Services.AddSingleton(speechAssistantSessionController.Object);
        Services.AddSingleton(new Mock<ISpeechCaptureSurfacePressController>().Object);
        Services.AddSingleton<ILogger<VoiceAssistantComponent>>(new RecordingLogger<VoiceAssistantComponent>());
        Services.AddSingleton<IOperationalProbeSession>(new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        Services.AddSingleton(Moq.Mock.Of<IOperationalProbeBootstrapClient>());
        Services.AddSingleton<IStudyTelemetryContext>(studyTelemetryContext);
        Services.AddSingleton<IDiagnosticOperatingMode>(new RecordingDiagnosticOperatingMode());

        var cut = Render<Home>();

        Assert.Empty(cut.FindAll("button.speech-surface"));
        Assert.Equal("Ungültiger Studienlink", cut.Find("h1").TextContent.Trim());
        Assert.Contains("Studienlink ist ungültig", cut.Find("[role='alert']").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Not Found", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("does not exist", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void SegmentUmfragePath_RendersGermanInvalidStudyLink_NotNotFound()
    {
        var speechAssistantSessionController = new Mock<ISpeechAssistantSessionController>();
        var studyTelemetryContext = new StudyTelemetryContext();
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        _ = Services.AddSpeechInteractionSignals(messageBus);

        Services.AddSingleton<NavigationManager>(
            new FixedNavigationManager("https://localhost/", "https://localhost/umfrage/x9jdln/variante/X"));
        Services.AddSingleton(speechAssistantSessionController.Object);
        Services.AddSingleton(new Mock<ISpeechCaptureSurfacePressController>().Object);
        Services.AddSingleton<ILogger<VoiceAssistantComponent>>(new RecordingLogger<VoiceAssistantComponent>());
        Services.AddSingleton<IOperationalProbeSession>(new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        Services.AddSingleton(Moq.Mock.Of<IOperationalProbeBootstrapClient>());
        Services.AddSingleton<IStudyTelemetryContext>(studyTelemetryContext);
        Services.AddSingleton<IDiagnosticOperatingMode>(new RecordingDiagnosticOperatingMode());

        var cut = Render<Home>();

        Assert.Empty(cut.FindAll("button.speech-surface"));
        Assert.Equal("Ungültiger Studienlink", cut.Find("h1").TextContent.Trim());
        Assert.DoesNotContain("Not Found", cut.Markup, StringComparison.Ordinal);
    }
}
