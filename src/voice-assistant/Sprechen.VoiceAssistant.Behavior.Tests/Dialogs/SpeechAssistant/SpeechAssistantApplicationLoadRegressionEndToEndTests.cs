using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft den Assistentenablauf vom Aktivierungsbefehl über die Anwendungsauswahl nach Namen bis zur Startmeldung.
/// Eingaben und Sprachausgabe werden durch Testdienste ersetzt; eine echte Audio- oder Transportkette läuft hier nicht.
/// </summary>
[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantApplicationLoadRegressionEndToEndTests
{
    private const string NoInputPrompt = "Ich habe nichts gehört";
    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task NamingAnApplication_OpensIt()
    {
        var session = await StartSessionAsync(
            utteranceCount: 3,
            enqueue: client =>
            {
                client.EnqueueTranscript("Okay bitte Anwendungskatalog öffnen");
                client.EnqueueTranscript("Beta", confidencePercent: 95);
                client.EnqueueTranscript("hallo beta");
            });

        await using var assistant = session.Assistant;

        // Die Anwendung über ihren Namen auswählen und die entsprechende Startmeldung abwarten.
        await session.WaitForSpokenAsync("Starte Beta", "application launch after naming it");

        Assert.DoesNotContain(session.SpokenText(), text => text.Contains(NoInputPrompt, StringComparison.Ordinal));
    }

    private static async Task<SpeechAssistantApplicationLoadSession> StartSessionAsync(int utteranceCount, Action<QueuedSpeechInputClient> enqueue)
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");

        // Der Dienst führt zuerst die Sprachauswahl aus; die vorbereitete Zustimmung behält Deutsch bereits beim ersten Angebot.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, utteranceCount);
        enqueue(speechInputClient);

        var catalog = SpeechAssistantApplicationCatalogBuilder.CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            SpeechAssistantApplicationCatalogBuilder.CreateApplication("alpha", "Alpha"),
            SpeechAssistantApplicationCatalogBuilder.CreateApplication("beta", "Beta"));

        var applicationCatalogDialog = FormsCatalogDialogTestFactory.Create(
            interactionSignals,
            speechInputClient,
            textToSpeechPlayer,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(
                new FormsCatalogIntentMatcher(),
                CatalogIntentParserFixtures.LanguageTargets,
                CatalogIntentParserFixtures.LanguageCatalog),
            userLanguageState);

        var assistantContext = SpeechAssistantContextTestFactory.Create(
            interactionSignals,
            userLanguageState,
            speechInputClient,
            textToSpeechPlayer,
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            applicationCatalogDialog,
            new FixedFormSelectionDialog("main-form"),
            new EchoVoiceCommandDialog(speechInputClient, textToSpeechPlayer),
            new RecordingLogger<SpeechAssistantService>());

        var assistant = new SpeechAssistantService(assistantContext);
        await assistant.StartAsync(CancellationToken.None);

        return new SpeechAssistantApplicationLoadSession(fixture, assistant, textToSpeechPlayer);
    }
}
