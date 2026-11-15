using Moq;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using AppSpeechTranscript = Sprechen.Speech.App.Contracts.Services.SpeechTranscript;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class WakeWordSetupWorkflowTests
{
    [Fact]
    public async Task WhenModeledWakeWordsAreAvailable_UsesModelFallbackAndDoesNotPersistOnDecline()
    {
        var voiceSettings = new Mock<IVoiceActivationSettings>(MockBehavior.Strict);
        voiceSettings
            .Setup(settings => settings.GetWakeWordActivationFormsOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string[]>(Array.Empty<string>()));

        var utteranceClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        utteranceClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUtterance());
        utteranceClient
            .Setup(client => client.SpeechToTextAsync(It.IsAny<CapturedSpeechAudio>(), It.IsAny<string?>(), It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new AppSpeechTranscript("no", null));

        var tts = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        tts.Setup(player => player.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer.Setup(player => player.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        earconPlayer.Setup(player => player.StopAllAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus.Setup(bus => bus.PublishAsync(It.IsAny<SpeechAssistantRecordingStateChanged>()))
            .Returns(ValueTask.CompletedTask);

        var workflow = WakeWordSetupWorkflowTestFactory.Create(
            voiceSettings.Object,
            utteranceClient.Object,
            tts.Object,
            earconPlayer.Object,
            SpeechInteractionSignalsTestFactory.Create(messageBus.Object));

        await workflow.EnsureWakeWordsConfiguredAsync("en", CancellationToken.None);

        voiceSettings.Verify(
            settings => settings.SetWakeWordActivationFormsOverrideAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        utteranceClient.Verify(client => client.RecordAsync(It.IsAny<CancellationToken>()), Times.Once);
        tts.Verify(player => player.SpeakAsync(It.IsAny<string>(), "en", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenWakeWordCaptureFails_ReAsksInsteadOfSayingItDidNotUnderstand()
    {
        // Scheitert die Erkennung, soll die Frage erneut kommen, ohne der Person Unverständlichkeit zuzuschreiben.
        var voiceSettings = new Mock<IVoiceActivationSettings>(MockBehavior.Strict);
        voiceSettings
            .Setup(settings => settings.GetWakeWordActivationFormsOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string[]>(Array.Empty<string>()));

        var yesNoAnswered = false;
        var utteranceClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        utteranceClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUtterance());
        utteranceClient
            .Setup(client => client.SpeechToTextAsync(It.IsAny<CapturedSpeechAudio>(), It.IsAny<string?>(), It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .Returns(() =>
            {
                // Nach der Zustimmung scheitern alle Erkennungsversuche an der vollen Warteschlange.
                if (!yesNoAnswered)
                {
                    yesNoAnswered = true;
                    return Task.FromResult(new AppSpeechTranscript("yes", null));
                }

                throw new SpeechRecognitionQueueFullException(queueDepth: 8);
            });

        var spokenTexts = new List<string>();
        var tts = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        tts.Setup(player => player.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((text, _, _) => spokenTexts.Add(text))
            .Returns(Task.CompletedTask);

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer.Setup(player => player.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        earconPlayer.Setup(player => player.StopAllAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus.Setup(bus => bus.PublishAsync(It.IsAny<SpeechAssistantRecordingStateChanged>()))
            .Returns(ValueTask.CompletedTask);

        var workflow = WakeWordSetupWorkflowTestFactory.Create(
            voiceSettings.Object,
            utteranceClient.Object,
            tts.Object,
            earconPlayer.Object,
            SpeechInteractionSignalsTestFactory.Create(messageBus.Object));

        await workflow.EnsureWakeWordsConfiguredAsync("en", CancellationToken.None);

        // Nach der ersten Ausgabe folgen genau drei gleiche Ausgaben, ohne zusätzliche Fehlermeldung.
        Assert.Equal(4, spokenTexts.Count);
        var afterTheChangeQuestion = spokenTexts.Skip(1).ToArray();
        Assert.Equal(3, afterTheChangeQuestion.Length);
        Assert.Single(afterTheChangeQuestion.Distinct(StringComparer.Ordinal));
        voiceSettings.Verify(
            settings => settings.SetWakeWordActivationFormsOverrideAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenUserDoesNotRespondToYesNoPrompt_TreatsAsDecline()
    {
        var voiceSettings = new Mock<IVoiceActivationSettings>(MockBehavior.Strict);
        voiceSettings
            .Setup(settings => settings.GetWakeWordActivationFormsOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string[]>(Array.Empty<string>()));

        var utteranceClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        utteranceClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NoSpeechDetectedException());

        var tts = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        tts.Setup(player => player.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer.Setup(player => player.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        earconPlayer.Setup(player => player.StopAllAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus.Setup(bus => bus.PublishAsync(It.IsAny<SpeechAssistantRecordingStateChanged>()))
            .Returns(ValueTask.CompletedTask);

        var workflow = WakeWordSetupWorkflowTestFactory.Create(
            voiceSettings.Object,
            utteranceClient.Object,
            tts.Object,
            earconPlayer.Object,
            SpeechInteractionSignalsTestFactory.Create(messageBus.Object));

        await workflow.EnsureWakeWordsConfiguredAsync("en", CancellationToken.None);

        utteranceClient.Verify(client => client.RecordAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        voiceSettings.Verify(
            settings => settings.SetWakeWordActivationFormsOverrideAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenUserProvidesNoWakeWords_DoesNotPersistOverride()
    {
        var voiceSettings = new Mock<IVoiceActivationSettings>(MockBehavior.Strict);
        voiceSettings
            .Setup(settings => settings.GetWakeWordActivationFormsOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string[]>(Array.Empty<string>()));

        var utteranceClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        utteranceClient
            .SetupSequence(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUtterance())
            .ReturnsAsync(CreateUtterance())
            .ReturnsAsync(CreateUtterance())
            .ReturnsAsync(CreateUtterance());

        utteranceClient
            .SetupSequence(client => client.SpeechToTextAsync(It.IsAny<CapturedSpeechAudio>(), It.IsAny<string?>(), It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new AppSpeechTranscript("yes", null))
            .ReturnsAsync(new AppSpeechTranscript(" ", null))
            .ReturnsAsync(new AppSpeechTranscript(" ", null))
            .ReturnsAsync(new AppSpeechTranscript(" ", null));

        var tts = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        tts.Setup(player => player.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer.Setup(player => player.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        earconPlayer.Setup(player => player.StopAllAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus.Setup(bus => bus.PublishAsync(It.IsAny<SpeechAssistantRecordingStateChanged>()))
            .Returns(ValueTask.CompletedTask);

        var workflow = WakeWordSetupWorkflowTestFactory.Create(
            voiceSettings.Object,
            utteranceClient.Object,
            tts.Object,
            earconPlayer.Object,
            SpeechInteractionSignalsTestFactory.Create(messageBus.Object));

        await workflow.EnsureWakeWordsConfiguredAsync("en", CancellationToken.None);

        voiceSettings.Verify(
            settings => settings.SetWakeWordActivationFormsOverrideAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenUserDeclinesChange_DoesNotPersistOverride()
    {
        var voiceSettings = new Mock<IVoiceActivationSettings>();
        voiceSettings
            .Setup(settings => settings.GetWakeWordActivationFormsOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string[]>(Array.Empty<string>()));

        var utteranceClient = new Mock<ISpeechInputClient>();
        utteranceClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUtterance());
        utteranceClient
            .Setup(client => client.SpeechToTextAsync(It.IsAny<CapturedSpeechAudio>(), It.IsAny<string?>(), It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new AppSpeechTranscript("no", null));

        var tts = new Mock<ITextToSpeechPlayer>();
        tts.Setup(player => player.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var workflow = WakeWordSetupWorkflowTestFactory.Create(
            voiceSettings.Object,
            utteranceClient.Object,
            tts.Object,
            Mock.Of<IEarconPlayer>(),
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()));

        await workflow.EnsureWakeWordsConfiguredAsync("en", CancellationToken.None);

        voiceSettings.Verify(
            settings => settings.SetWakeWordActivationFormsOverrideAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenUserChangesWakeWordsAndConfirms_PersistsOverride()
    {
        var voiceSettings = new Mock<IVoiceActivationSettings>();
        voiceSettings
            .Setup(settings => settings.GetWakeWordActivationFormsOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string[]>(Array.Empty<string>()));

        var utteranceClient = new Mock<ISpeechInputClient>();
        utteranceClient
            .SetupSequence(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUtterance())
            .ReturnsAsync(CreateUtterance())
            .ReturnsAsync(CreateUtterance());

        utteranceClient
            .SetupSequence(client => client.SpeechToTextAsync(It.IsAny<CapturedSpeechAudio>(), It.IsAny<string?>(), It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new AppSpeechTranscript("yes", null))
            .ReturnsAsync(new AppSpeechTranscript("alpha, beta", null))
            .ReturnsAsync(new AppSpeechTranscript("yes", null));

        var tts = new Mock<ITextToSpeechPlayer>();
        tts.Setup(player => player.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var workflow = WakeWordSetupWorkflowTestFactory.Create(
            voiceSettings.Object,
            utteranceClient.Object,
            tts.Object,
            Mock.Of<IEarconPlayer>(),
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()));

        await workflow.EnsureWakeWordsConfiguredAsync("en", CancellationToken.None);

        voiceSettings.Verify(
            settings => settings.SetWakeWordActivationFormsOverrideAsync(
                It.Is<string[]>(wakeWords => wakeWords.SequenceEqual(new[] { "alpha", "beta" }, StringComparer.OrdinalIgnoreCase)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WhenUserIncludesOkayPrefixInWakeWords_PersistsFullActivationForms()
    {
        var voiceSettings = new Mock<IVoiceActivationSettings>();
        voiceSettings
            .Setup(settings => settings.GetWakeWordActivationFormsOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string[]>(Array.Empty<string>()));

        var utteranceClient = new Mock<ISpeechInputClient>();
        utteranceClient
            .SetupSequence(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUtterance())
            .ReturnsAsync(CreateUtterance())
            .ReturnsAsync(CreateUtterance());

        utteranceClient
            .SetupSequence(client => client.SpeechToTextAsync(It.IsAny<CapturedSpeechAudio>(), It.IsAny<string?>(), It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new AppSpeechTranscript("yes", null))
            .ReturnsAsync(new AppSpeechTranscript("Okay alpha, beta", null))
            .ReturnsAsync(new AppSpeechTranscript("yes", null));

        var tts = new Mock<ITextToSpeechPlayer>();
        tts.Setup(player => player.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var workflow = WakeWordSetupWorkflowTestFactory.Create(
            voiceSettings.Object,
            utteranceClient.Object,
            tts.Object,
            Mock.Of<IEarconPlayer>(),
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()));

        await workflow.EnsureWakeWordsConfiguredAsync("en", CancellationToken.None);

        voiceSettings.Verify(
            settings => settings.SetWakeWordActivationFormsOverrideAsync(
                It.Is<string[]>(wakeWords => wakeWords.SequenceEqual(new[] { "Okay alpha", "beta" }, StringComparer.OrdinalIgnoreCase)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static CapturedSpeechAudio CreateUtterance()
    {
        return new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 0x00, 0x00 } });
    }
}
