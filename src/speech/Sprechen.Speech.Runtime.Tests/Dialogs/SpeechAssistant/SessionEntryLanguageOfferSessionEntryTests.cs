using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;
using Sprechen.Speech.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Aktivierung der Einstiegsentscheidung im Modell und die Reihenfolge
/// der Sprachfrage vor der Einführung des Aktivierungsworts beim Sitzungsstart.
/// </summary>
public sealed class SessionEntryLanguageOfferSessionEntryTests
{
    [Fact]
    public void TheModelKeepsTheEntryDecisionSwitchedOn()
    {
        // Eine Deaktivierung verändert den vorgesehenen Einstieg und darf nicht unbemerkt als Nebenwirkung einer anderen Änderung erfolgen.
        Assert.True(VoiceInterfaceModelCatalog.SessionLanguageOfferEnabled);
    }

    [Fact]
    public async Task SessionStart_OffersTheLanguage_AndOpensAnAnswerWindow()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        var settingsWorkflow = new Mock<IUserLanguageSettingsWorkflow>(MockBehavior.Strict);
        settingsWorkflow
            .Setup(w => w.SaveSelectedLanguageAsync("de", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new UserLanguageSettings("de", PolitenessLevel.Formal)));

        // Ohne Marker wird der Erstkontakt geprüft. Deutsch beantwortet die erste Frage,
        // damit dieser Fall den Einstieg und nicht zusätzlich einen Sprachwechsel untersucht.
        settingsWorkflow
            .Setup(w => w.ReadEntryOfferHeardIsoCodeAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<string?>(null));
        settingsWorkflow
            .Setup(w => w.SaveEntryOfferHeardAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);
        host.Setup(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        host.Setup(h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantLanguageSelectedEvent>(),
                DispatchMode.Interrupt,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var spoken = new List<string>();
        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeech
            .Setup(p => p.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((text, _, _) =>
            {
                spoken.Add(text);
                return Task.CompletedTask;
            });

        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]));
        speechClient.EnqueueTranscript("Deutsch");

        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            speechClient,
            speechClient,
            speechClient,
            textToSpeech.Object,
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context, () => host.Object);

        await service.StartAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(30));

        // Die Sprachentscheidung muss vor der Einführung erfolgen, damit diese bereits
        // in der gewählten Sprache angeboten wird. Bloße Existenz beider Ansagen genügt nicht.
        var keepQuestion = SessionLanguageOfferPromptTextResolver
            .GetKeepQuestionSegment("de", PolitenessLevel.Formal)
            .Text;

        var firstDecisionIndex = spoken.IndexOf(keepQuestion);
        var introIndex = spoken.IndexOf(
            FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt("de", PolitenessLevel.Formal));

        Assert.True(firstDecisionIndex >= 0, "the entry never offered the language");
        Assert.True(introIndex >= 0, "the entry never taught the activation word");
        Assert.True(
            firstDecisionIndex < introIndex,
            $"the language question must come before the activation word is taught, but the offer was "
            + $"utterance {firstDecisionIndex} and the intro was utterance {introIndex}");

        // Die letzte angeforderte Ansage ist der Wiederholungshinweis zur Einführung.
        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt("de", PolitenessLevel.Formal),
            spoken[^1]);

        // Zur Frage gehört ein Aufnahmeaufruf; eine reine Ansage würde keine Antwort entgegennehmen.
        Assert.True(
            speechClient.RecordCallCount >= 1,
            "the entry announced a language decision and then opened no answer window");
    }
}
