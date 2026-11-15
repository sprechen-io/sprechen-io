using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantWakeWordChangeEndToEndTests
{
    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task Workflow_WhenWakeWordOverrideConfigured_RecognizesOverrideAndRunsVoiceCommand()
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
        await voiceSettings.SetWakeWordActivationFormsOverrideAsync(["Nova"], CancellationToken.None);

        // Die Zustimmung zur Sprache wird vor dem Befehl mit dem eigenen Aktivierungswort eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 2);
        speechInputClient.EnqueueTranscript("Nova bitte Anwendungskatalog öffnen");
        speechInputClient.EnqueueTranscript("hallo welt");

        var wakeWordProvider = new FixedWakeWordProvider(["Nova"]);

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
            assistantLogger);

        await using var assistant = new SpeechAssistantService(assistantContext);

        await assistant.StartAsync(CancellationToken.None);

        try
        {
            await fixture.WaitUntilAsync(
                () => string.Equals(userLanguageState.SelectedIsoCode, "de", StringComparison.Ordinal),
                EndToEndTimeout);
        }
        catch (TimeoutException ex)
        {
            throw fixture.CreateLanguageSelectionTimeout(
                assistant,
                userLanguageState,
                speechInputClient,
                textToSpeechPlayer,
                messageBusLogger,
                assistantLogger,
                ex);
        }

        try
        {
            await fixture.WaitUntilAsync(
                () => textToSpeechPlayer.Calls.Any(call => call.Text == "hallo welt" && call.LanguageCode == "de"),
                EndToEndTimeout);
        }
        catch (TimeoutException ex)
        {
            var dump = string.Join(
                " | ",
                textToSpeechPlayer.Calls.Select(call => $"{call.LanguageCode}:{call.Text}"));

            var wakeWords = string.Join(
                ",",
                await voiceSettings.GetWakeWordActivationFormsOverrideAsync(CancellationToken.None));

            var sttDump = string.Join(
                " | ",
                speechInputClient.SpeechToTextCalls.Select(call => $"{call.LanguageCode}:{call.Text}"));

            var busDump = string.Join(
                " | ",
                messageBusLogger.Entries.Select(entry => $"{entry.Level}:{entry.Message}:{entry.Exception?.Message}"));

            throw new TimeoutException(
                $"Voice command did not complete in time. selectedIso={userLanguageState.SelectedIsoCode ?? "<null>"}, recordCalls={speechInputClient.RecordCallCount}, wakeOverride={wakeWords}, TTS calls: {dump}, STT calls: {sttDump}, busLogs: {busDump}",
                ex);
        }

        Assert.Equal("de", userLanguageState.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Formal, userLanguageState.SelectedPolitenessLevel);

        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ENTRY-COMMAND(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("HELP-USAGE(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("HELP-CATALOG(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("HELP-COMMANDS(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("HELP-LANGUAGE-CHANGE(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ASK-POLITENESS(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ASK-INTRO(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("WAKE-PROMPT(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("CHOOSE-LANGUAGE(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("CONFIRM(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("DONE(", StringComparison.Ordinal));

        var storedWakeWords = await voiceSettings.GetWakeWordActivationFormsOverrideAsync(CancellationToken.None);
        Assert.Equal(["Nova"], storedWakeWords);
    }
}
