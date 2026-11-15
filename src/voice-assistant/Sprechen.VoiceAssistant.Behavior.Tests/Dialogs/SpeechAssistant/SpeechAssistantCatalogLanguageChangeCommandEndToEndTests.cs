using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Localization;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantCatalogLanguageChangeCommandEndToEndTests
{
    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task Workflow_ChangeLanguageCommand_ChangesCatalogLanguageAndContinuesInTheNewLanguage()
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
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        // Die vorbereitete Zustimmung behält beim vorgeschalteten Spracheinstieg Deutsch bereits im ersten Angebot.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 5);
        speechInputClient.EnqueueTranscript("Okay, Hilfe.");
        speechInputClient.EnqueueTranscript("sprache wechseln englisch");
        speechInputClient.EnqueueTranscript("ja");
        speechInputClient.EnqueueTranscript("one", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("hello world");

        var wakeWordProvider = new FixedWakeWordProvider(["Okay"]);

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("test-app", supportedLanguages: ["de", "en"]));
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
            wakeWordProvider,
            applicationCatalogDialog,
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
        Assert.Contains(
            textToSpeechPlayer.Calls,
            call => call.Text.Contains("There is one application", StringComparison.Ordinal)
                && call.LanguageCode == "en");
        Assert.DoesNotContain(
            textToSpeechPlayer.Calls,
            call => call.Text.Contains("Settings", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ENTRY-COMMAND(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("CHOOSE-LANGUAGE(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("ASK-POLITENESS(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("WAKE-PROMPT(", StringComparison.Ordinal));
        Assert.DoesNotContain(textToSpeechPlayer.Calls, call => call.Text.StartsWith("CONFIRM(", StringComparison.Ordinal));
        Assert.Contains(textToSpeechPlayer.Calls, call => call.LanguageCode == "en" && call.Text.Contains("There is one application", StringComparison.Ordinal));
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

    private static ApplicationDescriptor CreateApplication(string id, string[] supportedLanguages)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = id,
            DisplayName = new LocalizedText
            {
                ["de"] = "Test Anwendung",
                ["en"] = "Test Application"
            },
            Description = new LocalizedText
            {
                ["de"] = "Test Anwendung Beschreibung",
                ["en"] = "Test application description"
            },
            SupportedLanguages = supportedLanguages.ToList(),
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("main-form"),
                    DisplayName = new LocalizedText
                    {
                        ["de"] = "Hauptformular",
                        ["en"] = "Main form"
                    },
                    Description = new LocalizedText
                    {
                        ["de"] = "Hauptformular",
                        ["en"] = "Main form"
                    },
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
            SupportedLanguages = ["de", "en"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Prompts =
            [
                CreatePrompt(
                    CatalogDialogPromptIds.AppAnnouncingSingle,
                    ("de", "Es gibt eine Anwendung: {{name}}."),
                    ("en", "There is one application: {{name}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppAnnouncingMultiple,
                    ("de", "Es gibt {{count}} Anwendungen. Ich nenne sie jetzt."),
                    ("en", "There are {{count}} applications. Here they are.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppListIntro,
                    ("de", "Verfügbare Anwendungen: {{count}}."),
                    ("en", "Available applications: {{count}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppListItem,
                    ("de", "Nummer {{number}}: {{name}}."),
                    ("en", "Number {{number}}: {{name}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppListSelectPrompt,
                    ("de", "Sagen Sie die Nummer oder den Namen."),
                    ("en", "Say the number or name.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppSearchResultsIntro,
                    ("de", "Gefunden: {{count}} Anwendungen für {{query}}."),
                    ("en", "Found {{count}} applications for {{query}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppSearchResultsItem,
                    ("de", "Nummer {{number}}: {{name}}."),
                    ("en", "Number {{number}}: {{name}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppConfirmation,
                    ("de", "{{name}}. Ist das richtig?"),
                    ("en", "{{name}}. Is that correct?")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppConfirmationRepeat,
                    ("de", "Bitte sagen Sie ja oder nein."),
                    ("en", "Please say yes or no.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppLaunch,
                    ("de", "Starte {{name}}."),
                    ("en", "Starting {{name}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppHelp,
                    ("de", "Sagen Sie alle Anwendungen, Suche nach, Nummer, wiederholen oder abbrechen."),
                    ("en", "Say all applications, search for, number, repeat or cancel.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppCancelled,
                    ("de", "Anwendungsauswahl abgebrochen."),
                    ("en", "Application selection cancelled.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppChangeLanguage,
                    ("de", "Sagen Sie Sprache wechseln plus eine unterstützte Sprache."),
                    ("en", "Say change language plus a supported language.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppNoInput,
                    ("de", "Ich habe nichts gehört. Bitte sprechen Sie noch einmal."),
                    ("en", "I didn't hear anything. Please speak again.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppReListen,
                    ("de", "Verstanden. Ich höre weiter zu."),
                    ("en", "Got it. I am still listening.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppNoApplications,
                    ("de", "Keine Anwendungen verfügbar."),
                    ("en", "No applications are available.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppNoSearchResults,
                    ("de", "Keine Anwendungen für {{query}} gefunden."),
                    ("en", "No applications found for {{query}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppNoMatch,
                    ("de", "{{name}} wurde nicht gefunden."),
                    ("en", "{{name}} was not found.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppInvalidNumber,
                    ("de", "Bitte wählen Sie eine Zahl zwischen 1 und {{max}}."),
                    ("en", "Please choose a number between 1 and {{max}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppNoDescription,
                    ("de", "Keine Beschreibung verfügbar."),
                    ("en", "No description available."))
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
