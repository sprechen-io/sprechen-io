using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantLanguageSwitchEndToEndTests
{
    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task Workflow_SessionConfiguredEnglish_RunsVoiceCommandWithoutOnboardingPrompts()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var messageBusLogger = new RecordingLogger<ClientMessageBus>();
        var messageBus = new ClientMessageBus(messageBusLogger);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var voiceSettings = new InMemoryVoiceActivationSettings();
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var earconPlayer = new NoOpEarconPlayer();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("en");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        // Die Zustimmung zu Englisch wird vor dem Befehl zum Öffnen des Katalogs eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 2);
        speechInputClient.EnqueueTranscript("Okay please open application catalog");
        speechInputClient.EnqueueTranscript("hello world");

        var wakeWordProvider = new FixedWakeWordProvider(["Okay"]);

        var assistantContext = SpeechAssistantContextTestFactory.Create(
            interactionSignals,
            userLanguageState,
            speechInputClient,
            textToSpeechPlayer,
            earconPlayer,
            wakeWordProvider,
            new FixedFormsCatalogDialog("test-app", "Test Application"),
            new FixedFormSelectionDialog("main-form"),
            new EchoVoiceCommandDialog(speechInputClient, textToSpeechPlayer),
            NullLogger<SpeechAssistantService>.Instance);

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

        var storedWakeWords = await voiceSettings.GetWakeWordActivationFormsOverrideAsync(CancellationToken.None);
        Assert.Empty(storedWakeWords);
    }

    [Fact]
    public async Task Workflow_SessionConfiguredItalian_RunsVoiceCommandWithoutOnboardingPrompts()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var messageBusLogger = new RecordingLogger<ClientMessageBus>();
        var messageBus = new ClientMessageBus(messageBusLogger);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var voiceSettings = new InMemoryVoiceActivationSettings();
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var earconPlayer = new NoOpEarconPlayer();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("it");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        // Die Zustimmung zu Italienisch wird vor dem Befehl zum Öffnen des Katalogs eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 2);
        speechInputClient.EnqueueTranscript("Okay per favore apri il catalogo delle applicazioni");
        speechInputClient.EnqueueTranscript("ciao mondo");

        var wakeWordProvider = new FixedWakeWordProvider(["Okay"]);

        var assistantContext = SpeechAssistantContextTestFactory.Create(
            interactionSignals,
            userLanguageState,
            speechInputClient,
            textToSpeechPlayer,
            earconPlayer,
            wakeWordProvider,
            new FixedFormsCatalogDialog("test-app", "Test Application"),
            new FixedFormSelectionDialog("main-form"),
            new EchoVoiceCommandDialog(speechInputClient, textToSpeechPlayer),
            NullLogger<SpeechAssistantService>.Instance);

        await using var assistant = new SpeechAssistantService(assistantContext);

        await assistant.StartAsync(CancellationToken.None);

        await fixture.WaitUntilAsync(
            () => textToSpeechPlayer.Calls.Any(call => call.Text == "ciao mondo" && call.LanguageCode == "it"),
            EndToEndTimeout);

        Assert.Equal("it", userLanguageState.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Formal, userLanguageState.SelectedPolitenessLevel);
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ENTRY-COMMAND(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ASK-POLITENESS(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ASK-INTRO(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("WAKE-PROMPT(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("CHOOSE-LANGUAGE(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("CONFIRM(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("DONE(", StringComparison.Ordinal));
    }
}


