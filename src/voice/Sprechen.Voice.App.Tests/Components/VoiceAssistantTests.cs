using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Sprechen.Web.SpeechComponents;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.LanguageContracts;
using Sprechen.Voice.App.Pages;
using Sprechen.Voice.App.Services;
using Sprechen.Voice.App.Tests.TestDoubles;
using VoiceAssistantComponent = Sprechen.Web.SpeechComponents.Components.VoiceAssistant;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class VoiceAssistantTests : TestContext
{
    private readonly RecordingDiagnosticOperatingMode _diagnosticOperatingMode = new();

    public VoiceAssistantTests()
    {
        // Die eingebettete SpeechCapture-Komponente bezieht ihre Texte aus der Sitzungssprache.
        // Hier werden die englischen Texte erwartet.
        Services.AddSingleton(Mock.Of<IUserLanguageState>(state => state.SelectedIsoCode == "en"));

        // SpeechCapture benötigt die Weckwörter für die Bereitschaftsmeldung.
        Services.AddSingleton<IWakeWordProvider>(new FixedWakeWordProvider());
        Services.AddSingleton<IDiagnosticOperatingMode>(_diagnosticOperatingMode);
    }

    [Fact]
    public void OnInitialized_WithUmfrageTokenPath_AppliesParticipantAndDoesNotStartAssistant()
    {
        var speechAssistantSessionController = new Mock<ISpeechAssistantSessionController>();

        var studyTelemetryContext = RenderWithUrl(
            "https://localhost/umfrage/x9jdln",
            speechAssistantSessionController);

        speechAssistantSessionController.Verify(service => service.StopAsync(CancellationToken.None), Times.Never);
        Assert.Equal("x9jdln", studyTelemetryContext.ParticipantId);
    }

    [Theory]
    [InlineData("https://localhost/umfrage/x9jdln/variante/X")]
    [InlineData("https://localhost/umfrage/x9jdln/aufgabe/t05")]
    [InlineData("https://localhost/umfrage/x9jdln/aufgabe/T05/variante/Y")]
    public void OnInitialized_WithUmfrageSegmentPath_AppliesNoStudyContext(string url)
    {
        var studyTelemetryContext = RenderWithUrl(url);

        Assert.Null(studyTelemetryContext.ParticipantId);
    }

    [Theory]
    [InlineData("https://localhost/umfrage/x9jdln/aufgabe")]
    [InlineData("https://localhost/umfrage/x9jdln/aufgabe/toolongtaskid12345")]
    [InlineData("https://localhost/umfrage/x9jdln/aufgabe/t05/extra")]
    [InlineData("https://localhost/umfrage/x9jdln/variante/B/aufgabe/t05")]
    [InlineData("https://localhost/umfrage/x9jdln/variante/B")]
    [InlineData("https://localhost/umfrage/x9jdln/aufgabe/T01/variante/V0")]
    public void OnInitialized_WithMalformedTaskUrl_AppliesNoStudyContext(string url)
    {
        var studyTelemetryContext = RenderWithUrl(url);

        Assert.Null(studyTelemetryContext.ParticipantId);
    }

    [Theory]
    [InlineData("https://localhost/")]
    [InlineData("https://localhost/umfrage/")]
    [InlineData("https://localhost/umfrage/sh0rt")]                      // Token mit nur fünf Zeichen
    [InlineData("https://localhost/umfrage/Uppercase99")]
    [InlineData("https://localhost/umfrage/x9jdln/foo/A")]
    [InlineData("https://localhost/umfrage/x9jdln/variante/a_b")]
    [InlineData("https://localhost/umfrage/x9jdln/extra/segment/here")]
    [InlineData("https://localhost/#pid=P123&tid=T05&var=b")]            // URL-Fragmente begründen keine Studienidentität
    public void OnInitialized_WithNonStudyOrMalformedUrl_AppliesNoStudyContext(string url)
    {
        var studyTelemetryContext = RenderWithUrl(url);

        Assert.Null(studyTelemetryContext.ParticipantId);
    }

    [Theory]
    [InlineData("https://localhost/umfrage/x9jdln/variante/B")]
    [InlineData("https://localhost/umfrage/x9jdln/variante/X")]
    [InlineData("https://localhost/umfrage/x9jdln/aufgabe/T01/variante/V0")]
    [InlineData("https://localhost/umfrage/x9jdln/aufgabe")]
    [InlineData("https://localhost/umfrage/x9jdln/aufgabe/t05")]
    [InlineData("https://localhost/umfrage/x9jdln/aufgabe/toolongtaskid12345")]
    [InlineData("https://localhost/umfrage/x9jdln/variante/B/aufgabe/t05")]
    [InlineData("https://localhost/umfrage/")]
    [InlineData("https://localhost/umfrage/sh0rt")]
    [InlineData("https://localhost/umfrage/x9jdln/foo/A")]
    [InlineData("https://localhost/umfrage/x9jdln/variante/a_b")]
    [InlineData("https://localhost/umfrage/x9jdln/aufgabe/t05/extra")]
    [InlineData("https://localhost/umfrage/Uppercase99")]
    [InlineData("https://localhost/umfrage/x9jdln/extra/segment/here")]
    public void OnInitialized_WithClaimedMalformedUmfragePath_OmitsStartControlAndAnnouncesNonSession(string url)
    {
        var cut = RenderCutWithUrl(url);

        Assert.Empty(cut.FindAll("button.speech-surface"));
        Assert.Equal("Invalid study link", cut.Find("h1").TextContent.Trim());
        var alert = cut.Find("[role='alert']");
        Assert.Equal("assertive", alert.GetAttribute("aria-live"));
        Assert.Equal(
            "This study link is invalid. Please check the link or contact the study coordinator.",
            alert.TextContent.Trim());
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void Home_WithClaimedMalformedUmfragePath_PassesLocalizedActionableCopyToVoiceAssistant(
        string isoCode)
    {
        var copy = new VoiceSettingsCopy();
        var cut = RenderHomeCutWithUrl("https://localhost/umfrage/sh0rt", isoCode);

        Assert.Empty(cut.FindAll("button.speech-surface"));
        Assert.Equal(copy.Chrome.GetInvalidStudyLinkHeading(isoCode), cut.Find("h1").TextContent.Trim());
        var alert = cut.Find("[role='alert']");
        Assert.Equal("assertive", alert.GetAttribute("aria-live"));
        Assert.Equal(copy.Chrome.GetInvalidStudyLinkMessage(isoCode), alert.TextContent.Trim());
    }

    [Fact]
    public void OnInitialized_WithMalformedUmfragePath_RendersPassedLocalizedInvalidStudyLinkCopy()
    {
        var copy = new VoiceSettingsCopy();
        var cut = RenderCutWithUrl(
            "https://localhost/umfrage/sh0rt",
            copy.Chrome.GetInvalidStudyLinkHeading("de"),
            copy.Chrome.GetInvalidStudyLinkMessage("de"));

        Assert.Empty(cut.FindAll("button.speech-surface"));
        Assert.Equal("Ungültiger Studienlink", cut.Find("h1").TextContent.Trim());
        Assert.Contains("Studienlink ist ungültig", cut.Find("[role='alert']").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Not Found", cut.Find("h1").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void OnInitialized_WithRootPath_StillPresentsStartControl()
    {
        var cut = RenderCutWithUrl("https://localhost/");

        Assert.NotEmpty(cut.FindAll("button.speech-surface"));
        Assert.Empty(cut.FindAll("[role='alert']"));
    }

    [Fact]
    public void OnInitialized_WithValidUmfrageTokenPath_StillPresentsStartControl()
    {
        var cut = RenderCutWithUrl("https://localhost/umfrage/x9jdln");

        Assert.NotEmpty(cut.FindAll("button.speech-surface"));
        Assert.Empty(cut.FindAll("[role='alert']"));
    }

    [Fact]
    public void OnInitialized_WithValidUmfrageTokenPath_DoesNotShowStudySessionStatus()
    {
        var cut = RenderCutWithUrl("https://localhost/umfrage/x9jdln");

        Assert.Empty(cut.FindAll("[role='status']"));
        Assert.DoesNotContain("Studienlink", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("x9jdln", cut.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(cut.FindAll("button.speech-surface"));
    }

    [Fact]
    public void OnInitialized_WithUmfrageSegmentPath_OmitsStartControlAndAnnouncesNonSession()
    {
        var cut = RenderCutWithUrl("https://localhost/umfrage/x9jdln/variante/X");

        Assert.Empty(cut.FindAll("button.speech-surface"));
        Assert.Equal("Invalid study link", cut.Find("h1").TextContent.Trim());
        Assert.Equal("assertive", cut.Find("[role='alert']").GetAttribute("aria-live"));
    }

    private StudyTelemetryContext RenderWithUrl(
        string url,
        Mock<ISpeechAssistantSessionController>? speechAssistantSessionController = null)
    {
        speechAssistantSessionController ??= new Mock<ISpeechAssistantSessionController>();
        var studyTelemetryContext = new StudyTelemetryContext();
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        _ = Services.AddSpeechInteractionSignals(messageBus);

        Services.AddSingleton<NavigationManager>(new FixedNavigationManager("https://localhost/", url));
        Services.AddSingleton(speechAssistantSessionController.Object);
        Services.AddSingleton(new Mock<ISpeechCaptureSurfacePressController>().Object);
        Services.AddSingleton<ILogger<VoiceAssistantComponent>>(new RecordingLogger<VoiceAssistantComponent>());
        Services.AddSingleton<IOperationalProbeSession>(new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        Services.AddSingleton(Moq.Mock.Of<IOperationalProbeBootstrapClient>());
        Services.AddSingleton<IStudyTelemetryContext>(studyTelemetryContext);

        _ = Render<VoiceAssistantComponent>();
        return studyTelemetryContext;
    }

    private IRenderedComponent<VoiceAssistantComponent> RenderCutWithUrl(
        string url,
        string? invalidStudyLinkHeading = null,
        string? invalidStudyLinkMessage = null)
    {
        var speechAssistantSessionController = new Mock<ISpeechAssistantSessionController>();
        var studyTelemetryContext = new StudyTelemetryContext();
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        _ = Services.AddSpeechInteractionSignals(messageBus);

        Services.AddSingleton<NavigationManager>(new FixedNavigationManager("https://localhost/", url));
        Services.AddSingleton(speechAssistantSessionController.Object);
        Services.AddSingleton(new Mock<ISpeechCaptureSurfacePressController>().Object);
        Services.AddSingleton<ILogger<VoiceAssistantComponent>>(new RecordingLogger<VoiceAssistantComponent>());
        Services.AddSingleton<IOperationalProbeSession>(new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        Services.AddSingleton(Moq.Mock.Of<IOperationalProbeBootstrapClient>());
        Services.AddSingleton<IStudyTelemetryContext>(studyTelemetryContext);

        return Render<VoiceAssistantComponent>(parameters =>
        {
            if (invalidStudyLinkHeading is not null)
            {
                parameters.Add(component => component.InvalidStudyLinkHeading, invalidStudyLinkHeading);
            }

            if (invalidStudyLinkMessage is not null)
            {
                parameters.Add(component => component.InvalidStudyLinkMessage, invalidStudyLinkMessage);
            }
        });
    }

    private IRenderedComponent<Home> RenderHomeCutWithUrl(string url, string isoCode)
    {
        var speechAssistantSessionController = new Mock<ISpeechAssistantSessionController>();
        var studyTelemetryContext = new StudyTelemetryContext();
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        _ = Services.AddSpeechInteractionSignals(messageBus);

        Services.AddSingleton(Mock.Of<IUserLanguageState>(state => state.SelectedIsoCode == isoCode));
        Services.AddSingleton<NavigationManager>(new FixedNavigationManager("https://localhost/", url));
        Services.AddSingleton(speechAssistantSessionController.Object);
        Services.AddSingleton(new Mock<ISpeechCaptureSurfacePressController>().Object);
        Services.AddSingleton<ILogger<VoiceAssistantComponent>>(new RecordingLogger<VoiceAssistantComponent>());
        Services.AddSingleton<IOperationalProbeSession>(new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        Services.AddSingleton(Moq.Mock.Of<IOperationalProbeBootstrapClient>());
        Services.AddSingleton<IStudyTelemetryContext>(studyTelemetryContext);

        return Render<Home>();
    }

    [Fact]
    public void OnInitialized_WithDebugPath_TurnsDiagnosticModeOnAndShowsTheNotice()
    {
        var cut = RenderCutWithUrl("https://localhost/debug");

        Assert.True(_diagnosticOperatingMode.IsDiagnostic);
        Assert.Equal(DiagnosticOperatingModeNames.Diagnostic, _diagnosticOperatingMode.OperatingModeMarker);
        Assert.NotEmpty(cut.FindAll("button.speech-surface"));
        var notice = cut.Find("[role='status']");
        Assert.Equal("polite", notice.GetAttribute("aria-live"));
        Assert.Equal("Diagnostic mode. This session is being recorded.", notice.TextContent.Trim());
    }

    [Fact]
    public void OnInitialized_WithHomePath_LeavesDiagnosticModeOff()
    {
        var cut = RenderCutWithUrl("https://localhost/");

        Assert.False(_diagnosticOperatingMode.IsDiagnostic);
        Assert.Empty(cut.FindAll("[role='status']"));
        Assert.NotEmpty(cut.FindAll("button.speech-surface"));
    }

    [Fact]
    public void OnInitialized_WithStudyPath_LeavesDiagnosticModeOffAndKeepsStudyIdentity()
    {
        var studyTelemetryContext = RenderWithUrl("https://localhost/umfrage/x9jdln");

        Assert.Equal("x9jdln", studyTelemetryContext.ParticipantId);
        Assert.False(_diagnosticOperatingMode.IsDiagnostic);
    }

    [Fact]
    public void OnInitialized_WithDebugStudyPath_TurnsDiagnosticModeOnAndKeepsStudyIdentity()
    {
        var studyTelemetryContext = RenderWithUrl("https://localhost/debug/umfrage/x9jdln");

        Assert.Equal("x9jdln", studyTelemetryContext.ParticipantId);
        Assert.True(_diagnosticOperatingMode.IsDiagnostic);
        Assert.Equal(DiagnosticOperatingModeNames.Diagnostic, _diagnosticOperatingMode.OperatingModeMarker);
    }

    [Fact]
    public void OnInitialized_WithDebugStudySegmentPath_TurnsDiagnosticModeOnAndShowsRefusal()
    {
        var cut = RenderCutWithUrl("https://localhost/debug/umfrage/x9jdln/variante/X");

        Assert.True(_diagnosticOperatingMode.IsDiagnostic);
        Assert.Empty(cut.FindAll("button.speech-surface"));
        Assert.Equal("Invalid study link", cut.Find("h1").TextContent.Trim());
    }

    [Fact]
    public async Task DisposeAsync_WhenInvoked_StopsSpeechAssistant()
    {
        var speechAssistantSessionController = new Mock<ISpeechAssistantSessionController>();
        speechAssistantSessionController.Setup(service => service.StopAsync(CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        var speechCaptureSurfaceController = new Mock<ISpeechCaptureSurfacePressController>();
        var logger = new RecordingLogger<VoiceAssistantComponent>();
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        _ = Services.AddSpeechInteractionSignals(messageBus);

        Services.AddSingleton(speechAssistantSessionController.Object);
        Services.AddSingleton(speechCaptureSurfaceController.Object);
        Services.AddSingleton<ILogger<VoiceAssistantComponent>>(logger);
        Services.AddSingleton<IOperationalProbeSession>(new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        Services.AddSingleton(Moq.Mock.Of<IOperationalProbeBootstrapClient>());
        Services.AddSingleton<IStudyTelemetryContext>(new StudyTelemetryContext());

        var cut = Render<VoiceAssistantComponent>();

        await cut.Instance.DisposeAsync();

        speechAssistantSessionController.Verify(service => service.StopAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WhenSpeechAssistantThrows_LogsDebug()
    {
        var speechAssistantSessionController = new Mock<ISpeechAssistantSessionController>();
        speechAssistantSessionController.Setup(service => service.StopAsync(CancellationToken.None))
            .Returns(() => new ValueTask(Task.FromException(new InvalidOperationException("boom"))));

        var speechCaptureSurfaceController = new Mock<ISpeechCaptureSurfacePressController>();
        var logger = new RecordingLogger<VoiceAssistantComponent>();
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        _ = Services.AddSpeechInteractionSignals(messageBus);

        Services.AddSingleton(speechAssistantSessionController.Object);
        Services.AddSingleton(speechCaptureSurfaceController.Object);
        Services.AddSingleton<ILogger<VoiceAssistantComponent>>(logger);
        Services.AddSingleton<IOperationalProbeSession>(new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        Services.AddSingleton(Moq.Mock.Of<IOperationalProbeBootstrapClient>());
        Services.AddSingleton<IStudyTelemetryContext>(new StudyTelemetryContext());

        var cut = Render<VoiceAssistantComponent>();

        await cut.Instance.DisposeAsync();

        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Debug &&
                     entry.Message.Contains("Stopping the speech assistant failed.", StringComparison.Ordinal));
    }
}
