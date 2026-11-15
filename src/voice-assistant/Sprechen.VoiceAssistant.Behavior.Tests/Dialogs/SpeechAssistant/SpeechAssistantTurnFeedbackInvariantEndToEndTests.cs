using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Ausgabeaufrufe und Fortsetzung ausgewählter Katalog- und Formularauswahldialoge nach Rückfragen, Abbrüchen und Fehlern.
/// Der produktive Auswahldialog läuft mit Testdiensten; der anschließende Formularbefehlsdialog ist durch EchoVoiceCommandDialog ersetzt.
/// </summary>
[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantTurnFeedbackInvariantEndToEndTests
{
    private const string AppName = "Beispiel";
    private const string UnrecognizedPromptText = "Das habe ich nicht verstanden. Bitte noch einmal.";
    private const string AppCancelledText = "Anwendungsauswahl abgebrochen.";
    private const string FormCancelledText = "Formularauswahl abgebrochen.";
    private const string LanguageKeptFragment = "Die Sprache bleibt";
    // Die Sprachwechselbestätigung verwendet die Zielsprache, damit sie auch ohne Verständnis der bisherigen Sitzungssprache beantwortet werden kann.
    private const string LanguageConfirmationFragment = "Switch the language to";
    // Ein Erkennungsfehler soll als technischer Fehler beschrieben werden und nicht als unverständliche Äußerung der Person.
    private const string RecoverableFailureFragment = "Die Spracherkennung ist derzeit nicht verfügbar";
    private const string EnglishAnnounceFragment = "There is one application";

    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(50);

    // EchoVoiceCommandDialog ersetzt den Formularbefehlsdialog; dessen Bestätigungs-, Eskalations- und Zifferneingaben laufen hier nicht.

    [Fact]
    public async Task SelectionDialog_UnhandledIntent_SpeaksReListenPromptBeforeNextRound()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var harness = CreateCatalogHarness(fixture, ConfirmationPolicy.Never);

        // Die Zustimmung zur Sprache wird vor den Eingaben für den Dialog eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(harness.SpeechInputClient, harness.UserLanguageState);
        await fixture.EnqueueUtterancesAsync(harness.SpeechInputClient, 6);
        harness.SpeechInputClient.EnqueueTranscript("Okay");   // Aktivierung ohne angehängten Befehl.
        harness.SpeechInputClient.EnqueueTranscript("ja");     // Ja ohne ausstehende Bestätigung.
        harness.SpeechInputClient.EnqueueTranscript(AppName, confidencePercent: 95);
        harness.SpeechInputClient.EnqueueTranscript("fertig"); // Der gestartete Testdialog gibt diese Eingabe zurück.

        await using var assistant = new SpeechAssistantService(harness.Context);

        await assistant.StartAsync(CancellationToken.None);

        await WaitForSpokenAsync(fixture, harness.Tts, $"Es gibt eine Anwendung: {AppName}");
        var cursor = fixture.GetFeedbackCursor(harness.ReadFeedback);
        await WaitForSpokenAsync(fixture, harness.Tts, UnrecognizedPromptText);
        await fixture.AssertFeedbackProducedAfterAsync(harness.ReadFeedback, cursor, EndToEndTimeout, "unhandled catalog intent");

        await WaitForSpokenAsync(fixture, harness.Tts, "fertig");
        var calls = harness.Tts.Calls.Select(call => call.Text).ToArray();
        var unrecognizedIndex = Array.FindIndex(calls, text => text.Contains(UnrecognizedPromptText, StringComparison.Ordinal));
        var launchIndex = Array.FindIndex(calls, text => text.Contains($"Starte {AppName}", StringComparison.Ordinal));
        Assert.True(unrecognizedIndex >= 0, "Expected a spoken re-listen prompt for the unhandled intent.");
        Assert.True(launchIndex > unrecognizedIndex, "Expected the dialog to keep listening and launch after the spoken re-prompt.");
    }

    [Fact]
    public async Task LanguageChange_Declined_KeepsLanguageAudiblyAndContinues()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var harness = CreateCatalogHarness(fixture, ConfirmationPolicy.Never);

        // Die Zustimmung zur Sprache wird vor den Eingaben für den Dialog eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(harness.SpeechInputClient, harness.UserLanguageState);
        await fixture.EnqueueUtterancesAsync(harness.SpeechInputClient, 7);
        harness.SpeechInputClient.EnqueueTranscript("Okay");
        harness.SpeechInputClient.EnqueueTranscript("sprache wechseln englisch");
        harness.SpeechInputClient.EnqueueTranscript("nein");
        harness.SpeechInputClient.EnqueueTranscript(AppName, confidencePercent: 95);
        harness.SpeechInputClient.EnqueueTranscript("fertig");

        await using var assistant = new SpeechAssistantService(harness.Context);

        await assistant.StartAsync(CancellationToken.None);

        await WaitForSpokenAsync(fixture, harness.Tts, LanguageConfirmationFragment);
        var cursor = fixture.GetFeedbackCursor(harness.ReadFeedback);
        await WaitForSpokenAsync(fixture, harness.Tts, LanguageKeptFragment);
        await fixture.AssertFeedbackProducedAfterAsync(harness.ReadFeedback, cursor, EndToEndTimeout, "declined language change");

        await WaitForSpokenAsync(fixture, harness.Tts, "fertig");
        Assert.Equal("de", harness.UserLanguageState.SelectedIsoCode);
        Assert.Contains(harness.Tts.Calls, call => call.Text.Contains(LanguageKeptFragment, StringComparison.Ordinal));
    }

    [Fact]
    public async Task LanguageChange_Cancel_ExitsSelectionDialogAudibly()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var harness = CreateCatalogHarness(fixture, ConfirmationPolicy.Never);

        // Die Zustimmung zur Sprache wird vor den Eingaben für den Dialog eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(harness.SpeechInputClient, harness.UserLanguageState);
        await fixture.EnqueueUtterancesAsync(harness.SpeechInputClient, 6);
        harness.SpeechInputClient.EnqueueTranscript("Okay");
        harness.SpeechInputClient.EnqueueTranscript("sprache wechseln englisch");
        harness.SpeechInputClient.EnqueueTranscript("abbrechen"); // Während der Sprachwechselbestätigung abbrechen.

        await using var assistant = new SpeechAssistantService(harness.Context);

        await assistant.StartAsync(CancellationToken.None);

        await WaitForSpokenAsync(fixture, harness.Tts, LanguageConfirmationFragment);
        var cursor = fixture.GetFeedbackCursor(harness.ReadFeedback);
        await WaitForSpokenAsync(fixture, harness.Tts, AppCancelledText);
        await fixture.AssertFeedbackProducedAfterAsync(harness.ReadFeedback, cursor, EndToEndTimeout, "cancelled language change");

        Assert.Equal("de", harness.UserLanguageState.SelectedIsoCode);
        Assert.Contains(harness.Tts.Calls, call => call.Text.Contains(AppCancelledText, StringComparison.Ordinal));
    }

    [Fact]
    public async Task FormSelectionConfirmingPhase_Cancel_ExitsDialogAudibly()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var recorders = fixture.CreateFeedbackRecorders();
        var tts = recorders.TextToSpeechPlayer;

        // Die Zustimmung zur Sprache wird vor den Eingaben für den Dialog eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 6);
        speechInputClient.EnqueueTranscript("Okay bitte Anwendungskatalog oeffnen");
        speechInputClient.EnqueueTranscript("eins");      // Erstes Formular auswählen.
        speechInputClient.EnqueueTranscript("abbrechen"); // Während der Formularbestätigung abbrechen.

        var catalog = fixture.CreateMultiFormCatalog(
            "test-app",
            "de",
            new VoiceDialogSettings(),
            ("form1", "Form Eins"),
            ("form2", "Form Zwei"));
        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var formSelectionDialog = FormSelectionDialogTestFactory.Create(
            interactionSignals,
            speechInputClient,
            tts,
            mdaCatalog,
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new FormSelectionMatcher(),
            userLanguageState);

        var context = SpeechAssistantContextTestFactory.Create(
            interactionSignals,
            userLanguageState,
            speechInputClient,
            tts,
            recorders.EarconPlayer,
            new FixedWakeWordProvider(["Okay"]),
            new FixedFormsCatalogDialog("test-app", "Test Application", 2, "form1"),
            formSelectionDialog,
            new EchoVoiceCommandDialog(speechInputClient, tts),
            NullLogger<SpeechAssistantService>.Instance);

        await using var assistant = new SpeechAssistantService(context);

        await assistant.StartAsync(CancellationToken.None);

        await WaitForSpokenAsync(fixture, tts, "Ist das richtig");
        var cursor = fixture.GetFeedbackCursor(recorders.ReadFeedback);
        await WaitForSpokenAsync(fixture, tts, FormCancelledText);
        await fixture.AssertFeedbackProducedAfterAsync(recorders.ReadFeedback, cursor, EndToEndTimeout, "cancel in confirming phase");

        Assert.Contains(tts.Calls, call => call.Text.Contains("Form Eins", StringComparison.Ordinal) && call.Text.Contains("Ist das richtig", StringComparison.Ordinal));
        Assert.Contains(tts.Calls, call => call.Text.Contains(FormCancelledText, StringComparison.Ordinal));
    }

    [Fact]
    public async Task RecoverableCaptureFailure_SpeaksDistinctCueAndStillCompletes()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var harness = CreateCatalogHarness(fixture, ConfirmationPolicy.Never);

        // Die Zustimmung zur Sprache wird vor den Eingaben für den Dialog eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(harness.SpeechInputClient, harness.UserLanguageState);
        await fixture.EnqueueUtterancesAsync(harness.SpeechInputClient, 6);
        harness.SpeechInputClient.EnqueueTranscript("Okay");
        // Drei aufeinanderfolgende Erkennungsfehler sollen die Wiederholungen des Erkennungsdienstes ausschöpfen und eine Fehlermeldung im Dialog auslösen.
        // Danach folgt ein gültiges Transkript, damit die Fortsetzung geprüft werden kann.
        harness.SpeechInputClient.EnqueueSpeechToTextException(new InvalidOperationException("stt-fault"));
        harness.SpeechInputClient.EnqueueSpeechToTextException(new InvalidOperationException("stt-fault"));
        harness.SpeechInputClient.EnqueueSpeechToTextException(new InvalidOperationException("stt-fault"));
        harness.SpeechInputClient.EnqueueTranscript(AppName, confidencePercent: 95); // Nach dem Fehler die Anwendung auswählen.
        harness.SpeechInputClient.EnqueueTranscript("fertig");

        await using var assistant = new SpeechAssistantService(harness.Context);

        await assistant.StartAsync(CancellationToken.None);

        await WaitForSpokenAsync(fixture, harness.Tts, $"Es gibt eine Anwendung: {AppName}");
        var cursor = fixture.GetFeedbackCursor(harness.ReadFeedback);
        await WaitForSpokenAsync(fixture, harness.Tts, RecoverableFailureFragment);
        await fixture.AssertFeedbackProducedAfterAsync(harness.ReadFeedback, cursor, EndToEndTimeout, "recoverable capture failure");

        // Die nachfolgende Testeingabe muss weiterhin ausgegeben werden.
        await WaitForSpokenAsync(fixture, harness.Tts, "fertig");
        Assert.Contains(harness.Tts.Calls, call => call.Text.Contains(RecoverableFailureFragment, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConfirmedLanguageChange_RestartStage_SuccessorAnnouncesInNewLanguage()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var harness = CreateCatalogHarness(fixture, ConfirmationPolicy.Never);

        // Die Zustimmung zur Sprache wird vor den Eingaben für den Dialog eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(harness.SpeechInputClient, harness.UserLanguageState);
        await fixture.EnqueueUtterancesAsync(harness.SpeechInputClient, 7);
        harness.SpeechInputClient.EnqueueTranscript("Okay");
        harness.SpeechInputClient.EnqueueTranscript("sprache wechseln englisch");
        harness.SpeechInputClient.EnqueueTranscript("ja");
        harness.SpeechInputClient.EnqueueTranscript("one", confidencePercent: 95); // Im neu gestarteten englischen Dialog auswählen.
        harness.SpeechInputClient.EnqueueTranscript("done");

        await using var assistant = new SpeechAssistantService(harness.Context);

        await assistant.StartAsync(CancellationToken.None);

        await WaitForSpokenAsync(fixture, harness.Tts, LanguageConfirmationFragment);
        var cursor = fixture.GetFeedbackCursor(harness.ReadFeedback);
        // Nach dem Neustart muss der Folgedialog seine Ankündigung in der neuen Sprache erzeugen.
        await WaitForSpokenAsync(fixture, harness.Tts, EnglishAnnounceFragment);
        await fixture.AssertFeedbackProducedAfterAsync(harness.ReadFeedback, cursor, EndToEndTimeout, "restart-requested stage");

        await WaitForSpokenAsync(fixture, harness.Tts, "done");
        Assert.Equal("en", harness.UserLanguageState.SelectedIsoCode);
        Assert.Contains(harness.Tts.Calls, call => call.Text.Contains(EnglishAnnounceFragment, StringComparison.Ordinal) && call.LanguageCode == "en");
    }

    [Fact]
    public async Task SelectionLaunchStage_SuccessorVoiceCommandSpeaks()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var harness = CreateCatalogHarness(fixture, ConfirmationPolicy.Never);

        // Die Zustimmung zur Sprache wird vor den Eingaben für den Dialog eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(harness.SpeechInputClient, harness.UserLanguageState);
        await fixture.EnqueueUtterancesAsync(harness.SpeechInputClient, 5);
        harness.SpeechInputClient.EnqueueTranscript("Okay");
        harness.SpeechInputClient.EnqueueTranscript(AppName, confidencePercent: 95);
        harness.SpeechInputClient.EnqueueTranscript("fertig");

        await using var assistant = new SpeechAssistantService(harness.Context);

        await assistant.StartAsync(CancellationToken.None);

        // Nach der Startmeldung des Auswahldialogs muss der anschließende Testdialog eine Ausgabe erzeugen.
        await WaitForSpokenAsync(fixture, harness.Tts, $"Starte {AppName}");
        var cursor = fixture.GetFeedbackCursor(harness.ReadFeedback);
        await WaitForSpokenAsync(fixture, harness.Tts, "fertig");
        await fixture.AssertFeedbackProducedAfterAsync(harness.ReadFeedback, cursor, EndToEndTimeout, "launch hand-off stage");

        Assert.Contains(harness.Tts.Calls, call => call.Text == "fertig");
    }

    private async Task WaitForSpokenAsync(SpeechAssistantEndToEndFixture fixture, RecordingTextToSpeechPlayer tts, string fragment)
    {
        try
        {
            await fixture.WaitUntilAsync(
                () => tts.Calls.Any(call => call.Text.Contains(fragment, StringComparison.Ordinal)),
                EndToEndTimeout);
        }
        catch (TimeoutException ex)
        {
            var spoken = string.Join(" | ", tts.Calls.Select(call => $"{call.LanguageCode}:{call.Text}"));
            throw new TimeoutException($"Expected spoken feedback containing '{fragment}'. Spoken=[{spoken}]", ex);
        }
    }

    private static (QueuedSpeechInputClient SpeechInputClient, RecordingTextToSpeechPlayer Tts, Func<IReadOnlyList<string>> ReadFeedback, UserLanguageState UserLanguageState, SpeechAssistantContext Context) CreateCatalogHarness(
        SpeechAssistantEndToEndFixture fixture,
        ConfirmationPolicy confirmationPolicy)
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var recorders = fixture.CreateFeedbackRecorders();
        var tts = recorders.TextToSpeechPlayer;

        var catalog = CreateCatalog(new VoiceDialogSettings { ConfirmationPolicy = confirmationPolicy });
        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var catalogDialog = FormsCatalogDialogTestFactory.Create(
            interactionSignals,
            speechInputClient,
            tts,
            mdaCatalog,
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            userLanguageState);

        var context = SpeechAssistantContextTestFactory.Create(
            interactionSignals,
            userLanguageState,
            speechInputClient,
            tts,
            recorders.EarconPlayer,
            new FixedWakeWordProvider(["Okay"]),
            catalogDialog,
            new FixedFormSelectionDialog("main-form"),
            new EchoVoiceCommandDialog(speechInputClient, tts),
            NullLogger<SpeechAssistantService>.Instance);

        return (speechInputClient, tts, recorders.ReadFeedback, userLanguageState, context);
    }

    private static FormsCatalog CreateCatalog(VoiceDialogSettings settings)
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            VoiceDialogSettings = settings,
            SelectionDialog = CreateSelectionDialog(),
            Applications = [CreateApplication("beispiel.app", AppName)]
        };
    }

    private static ApplicationDescriptor CreateApplication(string id, string displayName)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = id,
            DisplayName = new LocalizedText { ["de"] = displayName, ["en"] = displayName },
            Description = new LocalizedText { ["de"] = $"{displayName} Beschreibung", ["en"] = $"{displayName} description" },
            SupportedLanguages = ["de", "en"],
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("main-form"),
                    DisplayName = new LocalizedText { ["de"] = "Hauptformular", ["en"] = "Main form" },
                    Description = new LocalizedText { ["de"] = "Hauptformular", ["en"] = "Main form" },
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
                CreatePrompt(CatalogDialogPromptIds.AppAnnouncingSingle,
                    ("de", "Es gibt eine Anwendung: {{name}}. Sagen Sie den Namen oder Hilfe."),
                    ("en", "There is one application: {{name}}. Say the name or help.")),
                CreatePrompt(CatalogDialogPromptIds.AppAnnouncingMultiple,
                    ("de", "Es gibt {{count}} Anwendungen. Ich nenne sie jetzt."),
                    ("en", "There are {{count}} applications. Here they are.")),
                CreatePrompt(CatalogDialogPromptIds.AppListIntro,
                    ("de", "Verfuegbare Anwendungen: {{count}}."),
                    ("en", "Available applications: {{count}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppListItem,
                    ("de", "Nummer {{number}}: {{name}}."),
                    ("en", "Number {{number}}: {{name}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt,
                    ("de", "Sagen Sie die Nummer oder den Namen."),
                    ("en", "Say the number or name.")),
                CreatePrompt(CatalogDialogPromptIds.AppSearchResultsIntro,
                    ("de", "Gefunden: {{count}} Anwendungen fuer {{query}}."),
                    ("en", "Found {{count}} applications for {{query}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppSearchResultsItem,
                    ("de", "Nummer {{number}}: {{name}}."),
                    ("en", "Number {{number}}: {{name}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppConfirmation,
                    ("de", "{{name}}. Ist das richtig?"),
                    ("en", "{{name}}. Is that correct?")),
                CreatePrompt(CatalogDialogPromptIds.AppConfirmationRepeat,
                    ("de", "Bitte sagen Sie ja oder nein."),
                    ("en", "Please say yes or no.")),
                CreatePrompt(CatalogDialogPromptIds.AppLaunch,
                    ("de", "Starte {{name}}."),
                    ("en", "Starting {{name}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppHelp,
                    ("de", "Sagen Sie alle Anwendungen, Suche nach, Name, Nummer, wiederholen oder abbrechen."),
                    ("en", "Say all applications, search for, name, number, repeat or cancel.")),
                CreatePrompt(CatalogDialogPromptIds.AppCancelled,
                    ("de", AppCancelledText),
                    ("en", "Application selection cancelled.")),
                CreatePrompt(CatalogDialogPromptIds.AppChangeLanguage,
                    ("de", "Sagen Sie Sprache wechseln plus eine unterstuetzte Sprache."),
                    ("en", "Say change language plus a supported language.")),
                CreatePrompt(CatalogDialogPromptIds.AppUnrecognized,
                    ("de", UnrecognizedPromptText),
                    ("en", "I did not understand. Please repeat.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoInput,
                    ("de", "Ich habe nichts gehört. Bitte sprechen Sie noch einmal."),
                    ("en", "I didn't hear anything. Please speak again.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppReListen,
                    ("de", "Verstanden. Ich höre weiter zu."),
                    ("en", "Got it. I am still listening.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoApplications,
                    ("de", "Keine Anwendungen verfuegbar."),
                    ("en", "No applications are available.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoSearchResults,
                    ("de", "Keine Anwendungen fuer {{query}} gefunden."),
                    ("en", "No applications found for {{query}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoMatch,
                    ("de", "{{name}} wurde nicht gefunden."),
                    ("en", "{{name}} was not found.")),
                CreatePrompt(CatalogDialogPromptIds.AppInvalidNumber,
                    ("de", "Bitte waehlen Sie eine Zahl zwischen 1 und {{max}}."),
                    ("en", "Please choose a number between 1 and {{max}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoDescription,
                    ("de", "Keine Beschreibung verfuegbar."),
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
