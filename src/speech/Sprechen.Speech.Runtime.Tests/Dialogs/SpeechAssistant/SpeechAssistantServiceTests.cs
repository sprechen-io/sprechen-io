using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.TestDoubles;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Exceptions;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantServiceTests
{
    [Fact]
    public async Task StartAsync_WhenUserLanguageAlreadySelected_DispatchesLanguageSelectedInterrupt()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        var settingsWorkflow = CreateSettingsWorkflow(userLanguageState, "de");

        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);
        host.Setup(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        host.Setup(h => h.DispatchToActiveAsync(
                It.Is<SpeechAssistantLanguageSelectedEvent>(evt => evt.NormalizedIsoCode == "de"),
                DispatchMode.Interrupt,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            Mock.Of<ITextToSpeechPlayer>(),
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context, () => host.Object);

        await service.StartAsync(CancellationToken.None);

        host.VerifyAll();
        settingsWorkflow.Verify(
            w => w.SaveSelectedLanguageAsync("de", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenUserLanguageMissing_UsesModeledDefaultLanguage()
    {
        var userLanguageState = new UserLanguageState();
        var defaultLanguage = VoiceInterfaceModelCatalog.DefaultLanguage;
        var settingsWorkflow = CreateSettingsWorkflow(userLanguageState, defaultLanguage);

        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);
        host.Setup(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        host.Setup(h => h.DispatchToActiveAsync(
                It.Is<SpeechAssistantLanguageSelectedEvent>(evt => evt.NormalizedIsoCode == defaultLanguage),
                DispatchMode.Interrupt,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            Mock.Of<ITextToSpeechPlayer>(),
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context, () => host.Object);

        await service.StartAsync(CancellationToken.None);

        Assert.Equal(defaultLanguage, userLanguageState.SelectedIsoCode);
        host.VerifyAll();
        settingsWorkflow.Verify(
            w => w.SaveSelectedLanguageAsync(defaultLanguage, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenCalledTwice_DoesNotRecreateHost()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        var settingsWorkflow = CreateSettingsWorkflow(userLanguageState, "de");

        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);
        host.Setup(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        host.Setup(h => h.DispatchToActiveAsync(
                It.Is<SpeechAssistantLanguageSelectedEvent>(evt => evt.NormalizedIsoCode == "de"),
                DispatchMode.Interrupt,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            Mock.Of<ITextToSpeechPlayer>(),
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context, () => host.Object);

        await service.StartAsync(CancellationToken.None);
        await service.StartAsync(CancellationToken.None);

        host.Verify(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
        host.Verify(
            h => h.DispatchToActiveAsync(
                It.Is<SpeechAssistantLanguageSelectedEvent>(evt => evt.NormalizedIsoCode == "de"),
                DispatchMode.Interrupt,
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        settingsWorkflow.Verify(
            w => w.SaveSelectedLanguageAsync("de", It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task HandleSpeechCapturePressAsync_WhenStarted_DispatchesPressedEvent()
    {
        var userLanguageState = new UserLanguageState();
        var settingsWorkflow = CreateSettingsWorkflow(userLanguageState, VoiceInterfaceModelCatalog.DefaultLanguage);
        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);
        host.Setup(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        host.Setup(h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantLanguageSelectedEvent>(),
                DispatchMode.Interrupt,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        host.Setup(h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantPressedEvent>(),
                DispatchMode.Enqueue,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            Mock.Of<ITextToSpeechPlayer>(),
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context, () => host.Object);

        await service.StartAsync(CancellationToken.None);
        await service.HandleSpeechCapturePressAsync(CancellationToken.None);

        host.VerifyAll();
        host.Verify(
            h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantPressedEvent>(),
                It.IsAny<DispatchMode>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        settingsWorkflow.Verify(
            w => w.SaveSelectedLanguageAsync(VoiceInterfaceModelCatalog.DefaultLanguage, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleSpeechCapturePressAsync_WhenCalledAgain_DispatchesPressedEventEveryTime()
    {
        var userLanguageState = new UserLanguageState();
        var settingsWorkflow = CreateSettingsWorkflow(userLanguageState, VoiceInterfaceModelCatalog.DefaultLanguage);
        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);
        host.Setup(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        host.Setup(h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantLanguageSelectedEvent>(),
                DispatchMode.Interrupt,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        host.Setup(h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantPressedEvent>(),
                DispatchMode.Enqueue,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            Mock.Of<ITextToSpeechPlayer>(),
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context, () => host.Object);

        await service.StartAsync(CancellationToken.None);
        await service.HandleSpeechCapturePressAsync(CancellationToken.None);
        await service.HandleSpeechCapturePressAsync(CancellationToken.None);

        host.VerifyAll();
        host.Verify(
            h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantPressedEvent>(),
                DispatchMode.Enqueue,
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        settingsWorkflow.Verify(
            w => w.SaveSelectedLanguageAsync(VoiceInterfaceModelCatalog.DefaultLanguage, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleSpeechCapturePressAsync_WhenNotStarted_ThrowsSpeechAssistantNotStartedException()
    {
        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            Mock.Of<ITextToSpeechPlayer>(),
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context, () => Mock.Of<IDialogHost<SpeechAssistantEvent>>());

        await Assert.ThrowsAsync<SpeechAssistantNotStartedException>(() => service.HandleSpeechCapturePressAsync(CancellationToken.None).AsTask());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(PolitenessLevel.Formal)]
    [InlineData(PolitenessLevel.Informal)]
    public async Task StartAsync_OnFirstActivation_SpeaksWakeWordIntroOnceTeachingTheWakeWordInTheSelectedRegister(
        PolitenessLevel? selectedPolitenessLevel)
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(selectedPolitenessLevel);
        var settingsWorkflow = CreateSettingsWorkflow(userLanguageState, "de");

        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);
        host.Setup(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        host.Setup(h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantLanguageSelectedEvent>(),
                DispatchMode.Interrupt,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var textToSpeech = new Mock<ITextToSpeechPlayer>();
        textToSpeech
            .Setup(p => p.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            textToSpeech.Object,
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context, () => host.Object);

        // Ohne Auswahl gilt die formale Anrede; eine ausdrückliche Wahl muss die Einführung des Aktivierungsworts erreichen.
        var effectivePolitenessLevel = selectedPolitenessLevel ?? PolitenessLevel.Formal;
        var expectedIntro = FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt("de", effectivePolitenessLevel);

        await service.StartAsync(CancellationToken.None);

        Assert.Contains("Okay Alex", expectedIntro, StringComparison.Ordinal);
        if (effectivePolitenessLevel == PolitenessLevel.Informal)
        {
            Assert.NotEqual(
                FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt("de", PolitenessLevel.Formal),
                expectedIntro);
        }

        textToSpeech.Verify(
            p => p.SpeakAsync(expectedIntro, "de", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(PolitenessLevel.Formal)]
    [InlineData(PolitenessLevel.Informal)]
    public async Task StartAsync_WhenCalledTwice_SpeaksWakeWordIntroOnlyOnce(PolitenessLevel selectedPolitenessLevel)
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(selectedPolitenessLevel);
        var settingsWorkflow = CreateSettingsWorkflow(userLanguageState, "de");

        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);
        host.Setup(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        host.Setup(h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantLanguageSelectedEvent>(),
                DispatchMode.Interrupt,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var textToSpeech = new Mock<ITextToSpeechPlayer>();
        textToSpeech
            .Setup(p => p.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            textToSpeech.Object,
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context, () => host.Object);

        var expectedIntro = FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt("de", selectedPolitenessLevel);
        var expectedRepeatHint = FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt("de", selectedPolitenessLevel);

        await service.StartAsync(CancellationToken.None);
        await service.StartAsync(CancellationToken.None);

        // Beide Einführungssätze dürfen über zwei Aktivierungen hinweg nur einmal je Sitzung angefordert werden.
        Assert.Contains("Okay Alex", expectedRepeatHint, StringComparison.Ordinal);
        textToSpeech.Verify(
            p => p.SpeakAsync(expectedIntro, "de", It.IsAny<CancellationToken>()),
            Times.Once);
        textToSpeech.Verify(
            p => p.SpeakAsync(expectedRepeatHint, "de", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenWakeWordIntroPlaybackFails_PropagatesAndRetriesOnNextStart()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);
        var settingsWorkflow = CreateSettingsWorkflow(userLanguageState, "de");

        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);
        host.Setup(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        host.Setup(h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantLanguageSelectedEvent>(),
                DispatchMode.Interrupt,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var playbackFailure = new SpeechErrorException(SpeechErrorCode.ProviderFailure, "output graph unavailable");
        var introPrompt = FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt("de", PolitenessLevel.Formal);
        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);

        // Der Fehler ist an den Einführungstext gebunden, nicht an den ersten Aufruf.
        // Sonst würde die vorangestellte Sprachfrage mit anderer Fehlerbehandlung das Testziel verschieben.
        textToSpeech
            .Setup(p => p.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        textToSpeech
            .SetupSequence(p => p.SpeakAsync(introPrompt, "de", It.IsAny<CancellationToken>()))
            .ThrowsAsync(playbackFailure)
            .Returns(Task.CompletedTask);

        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            textToSpeech.Object,
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context, () => host.Object);
        var intro = introPrompt;
        var repeatHint = FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt("de", PolitenessLevel.Formal);

        var exception = await Assert.ThrowsAsync<SpeechErrorException>(
            () => service.StartAsync(CancellationToken.None).AsTask());

        // Eine fehlgeschlagene notwendige Einführung darf den Assistenten noch nicht starten.
        // Die nächste Aktivierung muss die Einführung zuerst erneut versuchen.
        host.Verify(
            h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        host.Verify(
            h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantLanguageSelectedEvent>(),
                It.IsAny<DispatchMode>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        await service.StartAsync(CancellationToken.None);

        Assert.Same(playbackFailure, exception);
        textToSpeech.Verify(
            p => p.SpeakAsync(intro, "de", It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        textToSpeech.Verify(
            p => p.SpeakAsync(repeatHint, "de", It.IsAny<CancellationToken>()),
            Times.Once);
        host.Verify(
            h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        host.Verify(
            h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantLanguageSelectedEvent>(),
                DispatchMode.Interrupt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartAsync_WhenStudyEntryIsBound_SpeaksAnnouncementBeforeIntroAndDispatchesStudySession(bool operational)
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        var settingsWorkflow = CreateSettingsWorkflow(userLanguageState, "de");
        var studyContext = new StudyTelemetryContext();
        studyContext.SetStudyContext(operational ? null : "x9jdln");
        var probeSession = new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession();
        if (operational)
        {
            probeSession.Request();
            probeSession.AcceptContext(new OperationalProbeContext("probe-0123456789abcdef0123456789abcdef", "B"));
        }

        var spoken = new List<string>();
        var dispatched = new List<SpeechAssistantEvent>();
        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);
        host.Setup(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        host.Setup(h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantEvent>(),
                DispatchMode.Interrupt,
                It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((evt, _, _) => dispatched.Add(evt))
            .Returns(ValueTask.CompletedTask);

        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeech
            .Setup(p => p.SpeakAsync(It.IsAny<string>(), "de", It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((text, _, _) => spoken.Add(text))
            .Returns(Task.CompletedTask);

        var catalog = new FormsCatalog
        {
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "study.umfrage",
                    DisplayName = new LocalizedText { ["de"] = "Umfrage" },
                    Description = new LocalizedText { ["de"] = "Studie" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    IsStudyApplication = true,
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("hintergrund-und-setup"),
                            DisplayName = new LocalizedText { ["de"] = "Quizabend" },
                            Description = new LocalizedText { ["de"] = "Form" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"],
                            Sequence = 1
                        }
                    ]
                }
            ]
        };

        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            textToSpeech.Object,
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance,
            formsCatalogDataClient: new FixedFormsCatalogDataClient(catalog),
            studyTelemetryContext: studyContext,
            operationalProbeSession: probeSession);

        await using var service = new SpeechAssistantService(context, () => host.Object);
        await service.StartAsync(CancellationToken.None);

        var announcement = FormDialogFlowPromptTextResolver.GetStudyEntryStudyAnnouncementPrompt("de", "Umfrage");
        var intro = FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt("de", PolitenessLevel.Formal);
        Assert.Contains("echte oder pers\u00f6nliche Daten", announcement, StringComparison.Ordinal);
        Assert.DoesNotContain("x9jdln", announcement, StringComparison.Ordinal);
        Assert.Contains(announcement, spoken);
        Assert.True(
            spoken.IndexOf(announcement) < spoken.IndexOf(intro),
            "the study announcement must be spoken before the wake-word intro");
        Assert.Equal(2, dispatched.Count);
        var bound = Assert.IsType<SpeechAssistantStudySessionBoundEvent>(dispatched[0]);
        Assert.Equal("study.umfrage", bound.Session.ApplicationId);
        Assert.Equal("hintergrund-und-setup", bound.Session.FormId);
        Assert.IsType<SpeechAssistantLanguageSelectedEvent>(dispatched[1]);
        if (operational)
        {
            Assert.Null(studyContext.ParticipantId);
            Assert.True(probeSession.Matches(probeSession.Context!.ProbeId, "B", bound.Session.ApplicationId, bound.Session.FormId));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartAsync_WhenStudyEntryIsBoundAndCatalogHasNoStudyApplication_Throws(bool operational)
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        var settingsWorkflow = CreateSettingsWorkflow(userLanguageState, "de");
        var studyContext = new StudyTelemetryContext();
        studyContext.SetStudyContext(operational ? null : "x9jdln");
        var probeSession = new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession();
        if (operational)
        {
            probeSession.Request();
            probeSession.AcceptContext(new OperationalProbeContext("probe-0123456789abcdef0123456789abcdef", "B"));
        }

        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);
        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            Mock.Of<ITextToSpeechPlayer>(),
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance,
            formsCatalogDataClient: new FixedFormsCatalogDataClient(new FormsCatalog()),
            studyTelemetryContext: studyContext,
            operationalProbeSession: probeSession);

        await using var service = new SpeechAssistantService(context, () => host.Object);

        await Assert.ThrowsAsync<StudyBindFailedException>(
            () => service.StartAsync(CancellationToken.None).AsTask());
        host.Verify(
            h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
