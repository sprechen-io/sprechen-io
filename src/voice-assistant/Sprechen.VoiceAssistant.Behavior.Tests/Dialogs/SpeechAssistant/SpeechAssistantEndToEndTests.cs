using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantEndToEndTests
{
    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task Workflow_DefaultGermanSession_RunsVoiceCommandWithoutOnboardingPrompts()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var messageBusLogger = new RecordingLogger<ClientMessageBus>();
        var messageBus = new ClientMessageBus(messageBusLogger);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var assistantLogger = new RecordingLogger<SpeechAssistantService>();
        var voiceSettings = new InMemoryVoiceActivationSettings();
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var feedbackRecorders = fixture.CreateFeedbackRecorders();
        var textToSpeechPlayer = feedbackRecorders.TextToSpeechPlayer;
        var earconPlayer = feedbackRecorders.EarconPlayer;
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");

        // Die vorbereitete Zustimmung behält beim vorgeschalteten Spracheinstieg Deutsch bereits im ersten Angebot.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 2);
        speechInputClient.EnqueueTranscript("Okay bitte Anwendungskatalog öffnen");
        speechInputClient.EnqueueTranscript("hallo welt");

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
            assistantLogger);

        await using var assistant = new SpeechAssistantService(assistantContext);

        var feedbackCursor = fixture.GetFeedbackCursor(feedbackRecorders.ReadFeedback);

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

            throw new TimeoutException($"Voice command did not complete in time. TTS calls: {dump}", ex);
        }

        Assert.Equal("de", userLanguageState.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Formal, userLanguageState.SelectedPolitenessLevel);
        await fixture.AssertFeedbackProducedAfterAsync(
            feedbackRecorders.ReadFeedback,
            feedbackCursor,
            EndToEndTimeout,
            "default German session");

        var calls = textToSpeechPlayer.Calls.ToArray();
        Assert.DoesNotContain(calls, call => call.Text.StartsWith("ENTRY-COMMAND(", StringComparison.Ordinal));
        Assert.DoesNotContain(calls, call => call.Text.StartsWith("ASK-POLITENESS(", StringComparison.Ordinal));
        Assert.DoesNotContain(calls, call => call.Text.StartsWith("ASK-INTRO(", StringComparison.Ordinal));
        Assert.DoesNotContain(calls, call => call.Text.StartsWith("WAKE-PROMPT(", StringComparison.Ordinal));
        Assert.DoesNotContain(calls, call => call.Text.StartsWith("CHOOSE-LANGUAGE(", StringComparison.Ordinal));
        Assert.DoesNotContain(calls, call => call.Text.StartsWith("CONFIRM(", StringComparison.Ordinal));
        Assert.DoesNotContain(calls, call => call.Text.StartsWith("DONE(", StringComparison.Ordinal));

        var storedWakeWords = await voiceSettings.GetWakeWordActivationFormsOverrideAsync(CancellationToken.None);
        Assert.Empty(storedWakeWords);
    }

    [Fact]
    public async Task Workflow_EnteringApplications_AnnouncesAllApplicationsBeforeUserLoadsOne()
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

        // Die vorbereitete Zustimmung behält beim vorgeschalteten Spracheinstieg Deutsch bereits im ersten Angebot.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 3);
        speechInputClient.EnqueueTranscript("Okay bitte Anwendungskatalog öffnen");
        speechInputClient.EnqueueTranscript("zwei", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("hallo beta");

        var wakeWordProvider = new FixedWakeWordProvider(["Okay"]);

        var catalog = SpeechAssistantApplicationCatalogBuilder.CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            SpeechAssistantApplicationCatalogBuilder.CreateApplication("alpha", "Alpha"),
            SpeechAssistantApplicationCatalogBuilder.CreateApplication("beta", "Beta"));
        var applicationCatalogDialog = FormsCatalogDialogTestFactory.Create(
            interactionSignals,
            speechInputClient,
            textToSpeechPlayer,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            userLanguageState);

        var assistantContext = SpeechAssistantContextTestFactory.Create(
            interactionSignals,
            userLanguageState,
            speechInputClient,
            textToSpeechPlayer,
            earconPlayer,
            wakeWordProvider,
            applicationCatalogDialog,
            new FixedFormSelectionDialog("main-form"),
            new EchoVoiceCommandDialog(speechInputClient, textToSpeechPlayer),
            assistantLogger);

        await using var assistant = new SpeechAssistantService(assistantContext);

        await assistant.StartAsync(CancellationToken.None);

        try
        {
            await fixture.WaitUntilAsync(
                () => textToSpeechPlayer.Calls.Any(call => call.Text == "hallo beta" && call.LanguageCode == "de"),
                EndToEndTimeout);
        }
        catch (TimeoutException ex)
        {
            var dump = string.Join(
                " | ",
                textToSpeechPlayer.Calls.Select(call => $"{call.LanguageCode}:{call.Text}"));

            throw new TimeoutException($"Application transition did not complete in time. TTS calls: {dump}", ex);
        }

        Assert.Equal("de", userLanguageState.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Formal, userLanguageState.SelectedPolitenessLevel);

        var calls = textToSpeechPlayer.Calls.ToArray();
        Assert.DoesNotContain(calls, call => call.Text.StartsWith("ASK-POLITENESS(", StringComparison.Ordinal));
        Assert.DoesNotContain(calls, call => call.Text.StartsWith("ASK-INTRO(", StringComparison.Ordinal));
        Assert.DoesNotContain(calls, call => call.Text.StartsWith("WAKE-PROMPT(", StringComparison.Ordinal));
        var firstInventoryItemIndex = Array.FindIndex(
            calls,
            call => call.LanguageCode == "de"
                && call.Text.Contains("Nummer 1", StringComparison.Ordinal)
                && call.Text.Contains("Alpha", StringComparison.Ordinal));
        var secondInventoryItemIndex = Array.FindIndex(
            calls,
            call => call.LanguageCode == "de"
                && call.Text.Contains("Nummer 2", StringComparison.Ordinal)
                && call.Text.Contains("Beta", StringComparison.Ordinal));
        var inventorySelectPromptIndex = Array.FindIndex(
            calls,
            call => call.LanguageCode == "de"
                && call.Text.Contains("Sagen Sie die Nummer oder den Namen", StringComparison.Ordinal));
        var launchIndex = Array.FindIndex(
            calls,
            call => call.LanguageCode == "de"
                && call.Text.Contains("Starte Beta", StringComparison.Ordinal));

        Assert.True(firstInventoryItemIndex >= 0, "Expected the segmented inventory prompt to announce the first application.");
        Assert.True(secondInventoryItemIndex > firstInventoryItemIndex, "Expected the segmented inventory prompt to announce the second application after the first.");
        Assert.True(inventorySelectPromptIndex > secondInventoryItemIndex, "Expected the segmented inventory prompt to end with the selection instruction.");
        Assert.True(launchIndex > inventorySelectPromptIndex, "Expected application launch after the segmented inventory prompt.");
        Assert.Contains(calls, call => call.Text == "hallo beta" && call.LanguageCode == "de");
    }
}
