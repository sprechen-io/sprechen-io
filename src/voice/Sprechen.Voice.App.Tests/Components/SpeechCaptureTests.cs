using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Web.SpeechComponents;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.LanguageContracts;
using Sprechen.Voice.App.Tests.TestDoubles;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class SpeechCaptureTests : TestContext
{
    public SpeechCaptureTests()
    {
        // Das Fokussieren beim ersten Rendern ruft JavaScript auf.
        // Loose erlaubt diesen Aufruf ohne eigene Testimplementierung.
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Die folgenden Komponentenprüfungen erwarten die englischen Katalogtexte.


        Services.AddSingleton(Mock.Of<IUserLanguageState>(state => state.SelectedIsoCode == "en"));

        // Ohne Weckwörter bleibt die Bereitschaftsmeldung kurz.
        // Tests der Einführung registrieren einen Anbieter mit Weckwörtern.
        Services.AddSingleton<IWakeWordProvider>(new FixedWakeWordProvider());

        Services.AddLogging();
    }

    [Fact]
    public async Task WhenRecordingStateChanges_UpdatesCssClass()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var feedback = Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton(Mock.Of<ISpeechCaptureSurfacePressController>());

        var cut = Render<SpeechCapture>();
        var icon = cut.Find("button.speech-surface");

        Assert.DoesNotContain("recording", icon.ClassList, StringComparer.OrdinalIgnoreCase);

        await messageBus.PublishAsync(new SpeechAssistantRecordingStateChanged(true));

        cut.WaitForAssertion(() =>
        {
            icon = cut.Find("button.speech-surface");
            Assert.Contains("recording", icon.ClassList, StringComparer.OrdinalIgnoreCase);
        });

        await messageBus.PublishAsync(new SpeechAssistantRecordingStateChanged(false));

        cut.WaitForAssertion(() =>
        {
            icon = cut.Find("button.speech-surface");
            Assert.DoesNotContain("recording", icon.ClassList, StringComparer.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task WhenEarconFamilyChanges_UpdatesCssClass()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton(Mock.Of<ISpeechCaptureSurfacePressController>());

        var cut = Render<SpeechCapture>();
        var surface = cut.Find("button.speech-surface");

        Assert.Contains("earcon-idle", surface.ClassList, StringComparer.OrdinalIgnoreCase);

        await messageBus.PublishAsync(new EarconFamilyChanged(EarconFamily.ValidationNotOk));

        cut.WaitForAssertion(() =>
        {
            surface = cut.Find("button.speech-surface");
            Assert.Contains("earcon-validation-not-ok", surface.ClassList, StringComparer.OrdinalIgnoreCase);
        });

        await messageBus.PublishAsync(new EarconFamilyChanged(null));

        cut.WaitForAssertion(() =>
        {
            surface = cut.Find("button.speech-surface");
            Assert.Contains("earcon-idle", surface.ClassList, StringComparer.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task WhenActivationStateChanges_UpdatesAccessibilityAndCssClass()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton(Mock.Of<ISpeechCaptureSurfacePressController>());

        var cut = Render<SpeechCapture>();
        var surface = cut.Find("button.speech-surface");
        var announcement = cut.Find("span[aria-live='polite']");

        Assert.Contains("activation-not-started", surface.ClassList, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            SpeechCaptureSurfaceCopy.GetAriaLabel(new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.NotStarted, false, null), "en"),
            surface.GetAttribute("aria-label"));
        Assert.Equal(
            SpeechCaptureSurfaceCopy.GetStatusAnnouncement(new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.NotStarted, false, null), "en"),
            announcement.TextContent.Trim());
        Assert.Equal("false", surface.GetAttribute("aria-busy"));
        Assert.Null(surface.GetAttribute("disabled"));
        Assert.Equal("false", surface.GetAttribute("aria-disabled"));

        await messageBus.PublishAsync(new SpeechAssistantActivationStateChanged(SpeechAssistantActivationState.Starting));

        cut.WaitForAssertion(() =>
        {
            surface = cut.Find("button.speech-surface");
            announcement = cut.Find("span[aria-live='polite']");
            Assert.Contains("activation-starting", surface.ClassList, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(
                SpeechCaptureSurfaceCopy.GetAriaLabel(new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.Starting, false, null), "en"),
                surface.GetAttribute("aria-label"));
            Assert.Equal(
                SpeechCaptureSurfaceCopy.GetStatusAnnouncement(new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.Starting, false, null), "en"),
                announcement.TextContent.Trim());
            Assert.Null(surface.GetAttribute("disabled"));
            Assert.Equal("true", surface.GetAttribute("aria-disabled"));
            Assert.Equal("true", surface.GetAttribute("aria-busy"));
        });

        await messageBus.PublishAsync(new SpeechAssistantActivationStateChanged(SpeechAssistantActivationState.Paused));

        cut.WaitForAssertion(() =>
        {
            surface = cut.Find("button.speech-surface");
            announcement = cut.Find("span[aria-live='polite']");
            Assert.Contains("activation-paused", surface.ClassList, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(
                SpeechCaptureSurfaceCopy.GetAriaLabel(new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.Paused, false, null), "en"),
                surface.GetAttribute("aria-label"));
            Assert.Equal(
                SpeechCaptureSurfaceCopy.GetStatusAnnouncement(new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.Paused, false, null), "en"),
                announcement.TextContent.Trim());
            Assert.Equal("false", surface.GetAttribute("aria-busy"));
            Assert.Null(surface.GetAttribute("disabled"));
            Assert.Equal("false", surface.GetAttribute("aria-disabled"));
        });

        await messageBus.PublishAsync(new SpeechAssistantActivationStateChanged(SpeechAssistantActivationState.Failed));

        cut.WaitForAssertion(() =>
        {
            surface = cut.Find("button.speech-surface");
            announcement = cut.Find("span[aria-live='polite']");
            Assert.Contains("activation-failed", surface.ClassList, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(
                SpeechCaptureSurfaceCopy.GetAriaLabel(new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.Failed, false, null), "en"),
                surface.GetAttribute("aria-label"));
            Assert.Equal(
                SpeechCaptureSurfaceCopy.GetStatusAnnouncement(new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.Failed, false, null), "en"),
                announcement.TextContent.Trim());
            Assert.Equal("false", surface.GetAttribute("aria-busy"));
            Assert.Null(surface.GetAttribute("disabled"));
            Assert.Equal("false", surface.GetAttribute("aria-disabled"));
        });
    }

    [Fact]
    public async Task WhenActivationStateAlreadyAvailable_UsesFeedbackStateOnInitialRender()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var feedback = Services.AddSpeechInteractionSignals(messageBus);

        await feedback.SetActivationStateAsync(SpeechAssistantActivationState.Ready);

        Services.AddSingleton(Mock.Of<ISpeechCaptureSurfacePressController>());

        var cut = Render<SpeechCapture>();
        var surface = cut.Find("button.speech-surface");

        Assert.Contains("activation-ready", surface.ClassList, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            SpeechCaptureSurfaceCopy.GetAriaLabel(new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.Ready, false, null), "en"),
            surface.GetAttribute("aria-label"));
    }

    [Fact]
    public async Task WhenColdStart_UsesDescriptiveAccessibleNameAndPointsAtFirstInstruction()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton(Mock.Of<ISpeechCaptureSurfacePressController>());

        var cut = Render<SpeechCapture>(parameters => parameters
            .Add(p => p.StartControlAccessibleName, "Start voice assistant - activate to begin")
            .Add(p => p.StartControlDescribedById, "start-instruction"));
        var surface = cut.Find("button.speech-surface");



        Assert.Equal("Start voice assistant - activate to begin", surface.GetAttribute("aria-label"));
        Assert.Equal("start-instruction", surface.GetAttribute("aria-describedby"));

        // Nach dem Start ersetzt der zustandsabhängige Katalogtext die einmalige Startanweisung.

        await messageBus.PublishAsync(new SpeechAssistantActivationStateChanged(SpeechAssistantActivationState.Ready));

        cut.WaitForAssertion(() =>
        {
            surface = cut.Find("button.speech-surface");
            Assert.Equal(
                SpeechCaptureSurfaceCopy.GetAriaLabel(new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.Ready, false, null), "en"),
                surface.GetAttribute("aria-label"));
            Assert.Null(surface.GetAttribute("aria-describedby"));
        });
    }

    [Fact]
    public async Task WhenWakeListeningFallbackStateChanges_UpdatesStatusAnnouncementAndTitle()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        Services.AddSingleton(Mock.Of<ISpeechCaptureSurfacePressController>());

        var cut = Render<SpeechCapture>();

        await messageBus.PublishAsync(new SpeechAssistantActivationStateChanged(SpeechAssistantActivationState.Ready));
        await messageBus.PublishAsync(new SpeechWakeListeningFallbackStateChanged(
            true,
            "WAKE_DETECTOR_UNAVAILABLE",
            LexicalWakeFallbackReason.DetectorUnavailable));

        cut.WaitForAssertion(() =>
        {
            var surface = cut.Find("button.speech-surface");
            var announcement = cut.Find("span[aria-live='polite']");

            Assert.Equal(
                "Voice assistant is ready. Wake-word detection may respond a little slower right now.",
                announcement.TextContent.Trim());
            Assert.Equal(
                "Voice assistant is ready. Wake-word detection may respond a little slower right now.",
                surface.GetAttribute("title"));
        });
    }

    [Fact]
    public async Task WhenReadyWithWakeWordInventory_AnnouncesWakeWordsAndHelpInAriaLiveRegion()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var feedback = Services.AddSpeechInteractionSignals(messageBus);
        await feedback.SetActivationStateAsync(SpeechAssistantActivationState.Ready);
        Services.AddSingleton(Mock.Of<ISpeechCaptureSurfacePressController>());
        Services.AddSingleton<IWakeWordProvider>(new FixedWakeWordProvider("Hey Sprechen"));

        var cut = Render<SpeechCapture>();

        cut.WaitForAssertion(() =>
        {
            var announcement = cut.Find("span[aria-live='polite']");
            Assert.Equal(
                "Voice assistant is ready. Say Hey Sprechen to begin. After the wake word you can say help at any time.",
                announcement.TextContent.Trim());
        });
    }

    [Fact]
    public async Task WhenReadyWithEmptyWakeWordInventory_AnnouncesPlainReadyText()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var feedback = Services.AddSpeechInteractionSignals(messageBus);
        await feedback.SetActivationStateAsync(SpeechAssistantActivationState.Ready);
        Services.AddSingleton(Mock.Of<ISpeechCaptureSurfacePressController>());
        Services.AddSingleton<IWakeWordProvider>(new FixedWakeWordProvider());

        var cut = Render<SpeechCapture>();

        cut.WaitForAssertion(() =>
        {
            var announcement = cut.Find("span[aria-live='polite']");
            Assert.Equal("Voice assistant is ready.", announcement.TextContent.Trim());
        });
    }

    [Fact]
    public void WhenPressed_DelegatesToSpeechCaptureSurfaceController()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        var controller = new Mock<ISpeechCaptureSurfacePressController>();
        controller.Setup(c => c.HandlePressAsync(CancellationToken.None)).Returns(ValueTask.CompletedTask);

        Services.AddSingleton(controller.Object);

        var cut = Render<SpeechCapture>();

        cut.Find("button.speech-surface").Click();

        controller.Verify(c => c.HandlePressAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task WhenPressedWhileStarting_SuppressesPressWithoutLosingFocusableButton()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        var controller = new Mock<ISpeechCaptureSurfacePressController>();
        controller.Setup(c => c.HandlePressAsync(CancellationToken.None)).Returns(ValueTask.CompletedTask);

        Services.AddSingleton(controller.Object);

        var cut = Render<SpeechCapture>();

        await messageBus.PublishAsync(new SpeechAssistantActivationStateChanged(SpeechAssistantActivationState.Starting));

        cut.WaitForAssertion(() =>
        {
            var surface = cut.Find("button.speech-surface");
            Assert.Equal("true", surface.GetAttribute("aria-disabled"));
            Assert.Null(surface.GetAttribute("disabled"));
        });

        cut.Find("button.speech-surface").Click();

        controller.Verify(c => c.HandlePressAsync(CancellationToken.None), Times.Never);
    }
}

