using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Localization;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Katalogauswahl und Hilfe nach einer Aktivierung mit oder ohne angehängten Befehl.
/// Die Tests verwenden den produktiven Katalogdialog mit simulierten Ein- und Ausgabediensten.
/// Geprüft werden die erzeugten Ausgabeaufrufe und ihre Reihenfolge.
/// </summary>
[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantPostWakeEndToEndTests
{
    private const string GenericInvitation = "Wie kann ich helfen?";

    private const string HelpPrompt =
        "Sagen Sie alle Anwendungen, Suche nach, Name, Nummer, wiederholen oder abbrechen.";

    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task Workflow_WhenLeadingWakeActivationRepeats_AnnouncesWithoutPrematureReadyCue()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var recorders = fixture.CreateFeedbackRecorders();
        var textToSpeechPlayer = recorders.TextToSpeechPlayer;
        var earconPlayer = recorders.EarconPlayer;
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        // Vor dem Aktivierungsdialog die vorgeschaltete Sprachauswahl vorbereiten.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 4);
        speechInputClient.EnqueueTranscript("Hey Alex. Okay Alex."); // Wiederholte Aktivierung ohne Befehl.
        speechInputClient.EnqueueTranscript("Beispiel", confidencePercent: 95); // Name der angekündigten Anwendung.
        speechInputClient.EnqueueTranscript("hallo welt"); // Erste Testeingabe nach dem Start.

        var assistant = CreateAssistant(
            interactionSignals,
            speechInputClient,
            textToSpeechPlayer,
            earconPlayer,
            userLanguageState,
            ConfirmationPolicy.Never,
            wakeWordActivationForms: ["Okay Alex", "Hey Alex"]);

        await using var _ = assistant;

        await assistant.StartAsync(CancellationToken.None);

        try
        {
            await fixture.WaitUntilAsync(
                () => textToSpeechPlayer.Calls.Any(call => call.Text == "hallo welt" && call.LanguageCode == "de"),
                EndToEndTimeout);
        }
        catch (TimeoutException ex)
        {
            throw Describe("Repeated bare wake activation did not reach the voice command after announcing.", textToSpeechPlayer, ex);
        }

        // Die Katalogankündigung muss die Anwendung und die Möglichkeit zur Hilfe nennen.
        Assert.Contains(
            textToSpeechPlayer.Calls,
            call => call.LanguageCode == "de"
                && call.Text.Contains("Es gibt eine Anwendung: Beispiel", StringComparison.Ordinal)
                && call.Text.Contains("Hilfe", StringComparison.Ordinal));


        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text == GenericInvitation);

        // Die wiederholte Aktivierung darf keine Nichtverstanden-Meldung auslösen.
        Assert.DoesNotContain(
            textToSpeechPlayer.Calls,
            call => call.Text.Contains("Das habe ich nicht verstanden", StringComparison.OrdinalIgnoreCase));

        var feedback = recorders.ReadFeedback().ToArray();
        Assert.Contains(
            feedback,
            static entry => entry.Contains("tts:de:Es gibt eine Anwendung: Beispiel", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Workflow_WhenRepeatedWakeIncludesHelp_RoutesOnlyHelpToTheCatalog()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var earconPlayer = new RecordingEarconPlayer();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        // Vor dem Aktivierungsdialog die vorgeschaltete Sprachauswahl vorbereiten.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 3);
        speechInputClient.EnqueueTranscript("Hey Alex. Okay Alex. Hilfe"); // Wiederholte Aktivierung mit angehängtem Hilfebefehl.
        speechInputClient.EnqueueTranscript("abbrechen");

        var assistant = CreateAssistant(
            interactionSignals,
            speechInputClient,
            textToSpeechPlayer,
            earconPlayer,
            userLanguageState,
            ConfirmationPolicy.Never,
            wakeWordActivationForms: ["Okay Alex", "Hey Alex"]);

        await using var _ = assistant;

        await assistant.StartAsync(CancellationToken.None);

        try
        {
            await fixture.WaitUntilAsync(
                () => textToSpeechPlayer.Calls.Any(call => call.Text == HelpPrompt && call.LanguageCode == "de"),
                EndToEndTimeout);
        }
        catch (TimeoutException ex)
        {
            throw Describe("Repeated wake did not route only 'Hilfe' into the catalog.", textToSpeechPlayer, ex);
        }


        Assert.Contains(
            textToSpeechPlayer.Calls,
            call => call.LanguageCode == "de" && call.Text == HelpPrompt);
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text == GenericInvitation);
        Assert.DoesNotContain(
            textToSpeechPlayer.Calls,
            call => call.Text.Contains("Das habe ich nicht verstanden", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Workflow_WhenDeviceWakeOverrideEqualsApplicationName_RequiresSelectionAndKeepsRecoveryAvailable()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var earconPlayer = new RecordingEarconPlayer();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        // Die Zustimmung zur Sprache wird vor dem Befehl zum Öffnen des Katalogs eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 5);
        speechInputClient.EnqueueTranscript("Beispiel");                 // Abweichendes Aktivierungswort ohne Befehl.
        speechInputClient.EnqueueTranscript("unbekannter katalogname");
        speechInputClient.EnqueueTranscript("alle Anwendungen");
        speechInputClient.EnqueueTranscript("eins", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("hallo welt");

        var assistant = CreateAssistant(
            interactionSignals,
            speechInputClient,
            textToSpeechPlayer,
            earconPlayer,
            userLanguageState,
            ConfirmationPolicy.Never,
            wakeWordActivationForms: ["Beispiel"]);

        await using var _ = assistant;

        await assistant.StartAsync(CancellationToken.None);

        try
        {
            await fixture.WaitUntilAsync(
                () => textToSpeechPlayer.Calls.Any(call => call.Text == "hallo welt" && call.LanguageCode == "de"),
                EndToEndTimeout);
        }
        catch (TimeoutException ex)
        {
            throw Describe(
                "The colliding device wake override did not preserve list/number recovery.",
                textToSpeechPlayer,
                ex);
        }

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var announcementIndex = Array.FindIndex(
            spoken,
            text => text.Contains("Es gibt eine Anwendung: Beispiel", StringComparison.Ordinal));
        var recoveryIndex = Array.FindIndex(
            spoken,
            text => text.Contains("unbekannter katalogname wurde nicht gefunden", StringComparison.Ordinal));
        var listIndex = Array.FindIndex(
            spoken,
            text => text.Contains("Verfügbare Anwendungen: 1", StringComparison.Ordinal));
        var launchIndex = Array.FindIndex(
            spoken,
            text => text.Contains("Starte Beispiel", StringComparison.Ordinal));

        Assert.True(announcementIndex >= 0, "A bare colliding wake override must enter the catalog announcement.");
        Assert.True(recoveryIndex > announcementIndex, "An unknown selection must produce spoken recovery.");
        Assert.True(listIndex > recoveryIndex, "The list command must remain reachable after recovery.");
        Assert.True(launchIndex > listIndex, "The application may launch only after explicit number selection.");
        Assert.Contains(
            textToSpeechPlayer.Calls,
            call => call.Text.Contains("Nummer 1: Beispiel", StringComparison.Ordinal));
    }

    private static SpeechAssistantService CreateAssistant(
        ClientMessageBusSpeechInteractionSignals interactionSignals,
        QueuedSpeechInputClient speechInputClient,
        RecordingTextToSpeechPlayer textToSpeechPlayer,
        RecordingEarconPlayer earconPlayer,
        UserLanguageState userLanguageState,
        ConfirmationPolicy confirmationPolicy,
        string[]? wakeWordActivationForms = null)
    {
        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = confirmationPolicy },
            CreateApplication("sample.application", "Beispiel"));
        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var applicationCatalogDialog = FormsCatalogDialogTestFactory.Create(
            interactionSignals,
            speechInputClient,
            textToSpeechPlayer,
            mdaCatalog,
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            userLanguageState);

        var assistantContext = SpeechAssistantContextTestFactory.Create(
            interactionSignals,
            userLanguageState,
            speechInputClient,
            textToSpeechPlayer,
            earconPlayer,
            new FixedWakeWordProvider(wakeWordActivationForms ?? ["Okay"]),
            applicationCatalogDialog,
            new FixedFormSelectionDialog("main-form"),
            new EchoVoiceCommandDialog(speechInputClient, textToSpeechPlayer),
            NullLogger<SpeechAssistantService>.Instance);

        return new SpeechAssistantService(assistantContext);
    }

    private static TimeoutException Describe(string message, RecordingTextToSpeechPlayer textToSpeechPlayer, Exception inner)
    {
        ArgumentNullException.ThrowIfNull(textToSpeechPlayer);
        ArgumentNullException.ThrowIfNull(inner);
        var spoken = string.Join(" | ", textToSpeechPlayer.Calls.Select(call => $"{call.LanguageCode}:{call.Text}"));
        return new TimeoutException($"{message} spoken=[{spoken}]", inner);
    }

    private static FormsCatalog CreateCatalog(VoiceDialogSettings settings, params ApplicationDescriptor[] applications)
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            VoiceDialogSettings = settings,
            SelectionDialog = CreateSelectionDialog(),
            Applications = applications.ToList()
        };
    }

    private static ApplicationDescriptor CreateApplication(string id, string displayName)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = id,
            DisplayName = new LocalizedText { ["de"] = displayName },
            Description = new LocalizedText { ["de"] = $"{displayName} Beschreibung" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("main-form"),
                    DisplayName = new LocalizedText { ["de"] = "Hauptformular" },
                    Description = new LocalizedText { ["de"] = "Hauptformular" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        };
    }

    private static ApplicationSelectionDialog CreateSelectionDialog()
    {
        return new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "1",
            SelectionDialogId = "default.selectionDialog",
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Prompts =
            [
                CreatePrompt(CatalogDialogPromptIds.AppAnnouncingSingle, ("de", "Es gibt eine Anwendung: {{name}}. Sagen Sie den Namen oder Hilfe.")),
                CreatePrompt(CatalogDialogPromptIds.AppAnnouncingMultiple, ("de", "Es gibt {{count}} Anwendungen. Ich nenne sie jetzt.")),
                CreatePrompt(CatalogDialogPromptIds.AppListIntro, ("de", "Verfügbare Anwendungen: {{count}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppListItem, ("de", "Nummer {{number}}: {{name}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt, ("de", "Sagen Sie die Nummer oder den Namen.")),
                CreatePrompt(CatalogDialogPromptIds.AppSearchResultsIntro, ("de", "Gefunden: {{count}} Anwendungen für {{query}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppSearchResultsItem, ("de", "Nummer {{number}}: {{name}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppConfirmation, ("de", "{{name}}. Ist das richtig?")),
                CreatePrompt(CatalogDialogPromptIds.AppConfirmationRepeat, ("de", "Bitte sagen Sie ja oder nein.")),
                CreatePrompt(CatalogDialogPromptIds.AppLaunch, ("de", "Starte {{name}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppHelp, ("de", HelpPrompt)),
                CreatePrompt(CatalogDialogPromptIds.AppCancelled, ("de", "Anwendungsauswahl abgebrochen.")),
                CreatePrompt(CatalogDialogPromptIds.AppChangeLanguage, ("de", "Sagen Sie Sprache wechseln plus eine unterstützte Sprache.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoInput, ("de", "Ich habe nichts gehört. Bitte sprechen Sie noch einmal.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppReListen,
                    ("de", "Verstanden. Ich höre weiter zu."),
                    ("en", "Got it. I am still listening.")),
                CreatePrompt(CatalogDialogPromptIds.AppUnrecognized, ("de", "Das habe ich nicht verstanden. Bitte wiederholen Sie oder sagen Sie Hilfe.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoApplications, ("de", "Keine Anwendungen verfügbar.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoSearchResults, ("de", "Keine Anwendungen für {{query}} gefunden.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoMatch, ("de", "{{name}} wurde nicht gefunden.")),
                CreatePrompt(CatalogDialogPromptIds.AppInvalidNumber, ("de", "Bitte wählen Sie eine Zahl zwischen 1 und {{max}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoDescription, ("de", "Keine Beschreibung verfügbar."))
            ],
            Intents = [.. SpeechAssistantCatalogIntentHints.CreateIntents()],
            NumberWords = [.. SpeechAssistantCatalogIntentHints.CreateNumberWords()]
        };
    }

    private static CatalogVoicePrompt CreatePrompt(string promptId, params (string LanguageCode, string Text)[] entries)
    {
        var localizedText = new LocalizedText();
        foreach (var (languageCode, text) in entries)
        {
            localizedText[languageCode] = text;
        }

        return new CatalogVoicePrompt
        {
            PromptId = promptId,
            Text = localizedText
        };
    }
}
