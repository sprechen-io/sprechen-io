using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantFormSelectionConfirmEndToEndTests
{
    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task Workflow_MultiFormConfirmNoThenYes_RepeatsSelection()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var messageBusLogger = new RecordingLogger<ClientMessageBus>();
        var messageBus = new ClientMessageBus(messageBusLogger);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var assistantLogger = new RecordingLogger<SpeechAssistantService>();
        var voiceSettings = new InMemoryVoiceActivationSettings();
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var earconPlayer = new NoOpEarconPlayer();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");

        // Die Zustimmung zur Sprache wird vor den Eingaben für die Formularauswahl eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 6);
        speechInputClient.EnqueueTranscript("Okay bitte Anwendungskatalog öffnen");
        speechInputClient.EnqueueTranscript("eins", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("nein");
        speechInputClient.EnqueueTranscript("zwei", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("hallo welt");

        var wakeWordProvider = new FixedWakeWordProvider(["Okay"]);

        var voiceDialogSettings = new VoiceDialogSettings();
        var catalog = fixture.CreateMultiFormCatalog(
            "test-app",
            "de",
            voiceDialogSettings,
            ("form1", "Form Eins"),
            ("form2", "Form Zwei"));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);

        var formSelectionDialog = FormSelectionDialogTestFactory.Create(
            interactionSignals,
            speechInputClient,
            textToSpeechPlayer,
            mdaCatalog,
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new FormSelectionMatcher(),
            userLanguageState);

        var assistantContext = SpeechAssistantContextTestFactory.Create(
            interactionSignals,
            userLanguageState,
            speechInputClient,
            textToSpeechPlayer,
            earconPlayer,
            wakeWordProvider,
            new FixedFormsCatalogDialog("test-app", "Test Application", 2, "form1"),
            formSelectionDialog,
            new EchoVoiceCommandDialog(speechInputClient, textToSpeechPlayer),
            assistantLogger);

        await using var assistant = new SpeechAssistantService(assistantContext);

        await assistant.StartAsync(CancellationToken.None);

        await fixture.WaitUntilAsync(
            () => textToSpeechPlayer.Calls.Any(call => call.Text == "hallo welt" && call.LanguageCode == "de"),
            EndToEndTimeout);

        Assert.Equal("de", userLanguageState.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Formal, userLanguageState.SelectedPolitenessLevel);
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ENTRY-COMMAND(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("CONFIRM(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ASK-POLITENESS(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ASK-INTRO(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("WAKE-PROMPT(", StringComparison.Ordinal));
        Assert.Contains(textToSpeechPlayer.Calls, call => call.Text.Contains("Form Eins", StringComparison.Ordinal) && call.Text.Contains("Ist das richtig", StringComparison.Ordinal));
        Assert.Contains(textToSpeechPlayer.Calls, call => call.Text.Contains("Form Zwei", StringComparison.Ordinal) && call.Text.Contains("Ist das richtig", StringComparison.Ordinal));

        // Die Bestätigungsaufnahme wird schon während der Ansage vorbereitet.
        // Auch dieser vorbereitete Aufruf muss den Modus ShortConfirmation erhalten.
        Assert.Contains(SpeechCaptureMode.ShortConfirmation, speechInputClient.RecordModes);

        Assert.Contains(SpeechCaptureMode.Listening, speechInputClient.RecordModes);
    }
}
