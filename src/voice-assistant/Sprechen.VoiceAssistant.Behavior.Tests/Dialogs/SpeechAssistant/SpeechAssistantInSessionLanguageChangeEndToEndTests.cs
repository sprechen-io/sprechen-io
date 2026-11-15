using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Immutable;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantInSessionLanguageChangeEndToEndTests
{
    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task Workflow_WhenVoiceCommandChangesLanguage_RestartsActiveDialogInNewLanguage()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var voiceSettings = new InMemoryVoiceActivationSettings();
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var earconPlayer = new NoOpEarconPlayer();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");

        // Die Zustimmung zur anfänglichen Sprache wird vor dem späteren Sprachwechsel eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 2);
        speechInputClient.EnqueueTranscript("Okay bitte Anwendungskatalog oeffnen");
        speechInputClient.EnqueueTranscript("hello world");

        var wakeWordProvider = new FixedWakeWordProvider(["Okay"]);
        var voiceCommandDialog = new SwitchLanguageThenEchoVoiceCommandDialog(
            speechInputClient,
            textToSpeechPlayer,
            targetIsoCode: "en",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)
                .Add("city", "Berlin"));

        var assistantContext = SpeechAssistantContextTestFactory.Create(
            interactionSignals,
            userLanguageState,
            speechInputClient,
            textToSpeechPlayer,
            earconPlayer,
            wakeWordProvider,
            new FixedFormsCatalogDialog("test-app", "Test Application", 1, "main-form"),
            new FixedFormSelectionDialog("main-form"),
            voiceCommandDialog,
            NullLogger<SpeechAssistantService>.Instance);

        await using var assistant = new SpeechAssistantService(assistantContext);

        await assistant.StartAsync(CancellationToken.None);

        await fixture.WaitUntilAsync(
            () => textToSpeechPlayer.Calls.Any(call => call.Text == "hello world" && call.LanguageCode == "en"),
            EndToEndTimeout);

        Assert.Equal("en", userLanguageState.SelectedIsoCode);
        Assert.Equal(2, voiceCommandDialog.Executions.Count);
        Assert.Equal("de", voiceCommandDialog.Executions[0].LanguageCode);
        Assert.Equal("en", voiceCommandDialog.Executions[1].LanguageCode);
        Assert.Equal("Berlin", voiceCommandDialog.Executions[1].InitialValuesByVariableId["city"]);
    }
}
