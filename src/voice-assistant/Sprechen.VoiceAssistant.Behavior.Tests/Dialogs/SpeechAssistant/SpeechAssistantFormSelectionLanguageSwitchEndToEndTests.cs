using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantFormSelectionLanguageSwitchEndToEndTests
{
    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task Workflow_SessionConfiguredEnglish_UsesEnglishFormSelectionPrompts()
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
        userLanguageState.SetSelectedIsoCode("en");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        // Die Zustimmung zur Sprache wird vor den Eingaben für die Formularauswahl eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 4);
        speechInputClient.EnqueueTranscript("Okay please open application catalog");
        speechInputClient.EnqueueTranscript("one", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("yes", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("hello world");

        var wakeWordProvider = new FixedWakeWordProvider(["Okay"]);

        var voiceDialogSettings = new VoiceDialogSettings();
        var catalog = fixture.CreateMultiFormCatalog(
            "test-app",
            "en",
            voiceDialogSettings,
            (FormId: "form1", DisplayName: "Form One"),
            (FormId: "form2", DisplayName: "Form Two"));

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
            () => textToSpeechPlayer.Calls.Any(call => call.Text == "hello world" && call.LanguageCode == "en"),
            EndToEndTimeout);

        Assert.Equal("en", userLanguageState.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Formal, userLanguageState.SelectedPolitenessLevel);
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ENTRY-COMMAND(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("CHOOSE-LANGUAGE(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("LANGUAGE-CHANGED(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ASK-POLITENESS(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ASK-INTRO(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("WAKE-PROMPT(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("CONFIRM(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("DONE(", StringComparison.Ordinal));
        Assert.Contains(textToSpeechPlayer.Calls, call => call.LanguageCode == "en" && call.Text.Contains("This application has", StringComparison.Ordinal));
        Assert.Contains(textToSpeechPlayer.Calls, call => call.LanguageCode == "en" && call.Text.Contains("Is that correct", StringComparison.Ordinal));
    }
}

