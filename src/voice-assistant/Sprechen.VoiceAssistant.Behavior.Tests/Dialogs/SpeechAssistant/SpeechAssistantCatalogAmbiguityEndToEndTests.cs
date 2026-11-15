using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Auswahlabläufe mit mehrdeutigen Anwendungs- und Formularnamen anhand aufgezeichneter Sprachausgabeaufrufe.
/// Die Fälle prüfen Reihenfolge, stabile Nummern und Startmeldungen nach eindeutiger Auswahl; echte Audioausgabe wird nicht ausgeführt.
/// </summary>
[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantCatalogAmbiguityEndToEndTests
{
    private const string AmbiguityShortTemplate = "Ich habe {{count}} passende Anwendungen gefunden.";
    private const string AmbiguityRefineOrListTemplate = "Ich habe {{count}} passende Anwendungen gefunden. Nennen Sie einen genaueren Teil des Namens, sagen Sie „alle Anwendungen“ für die vollständige Liste oder „abbrechen“.";
    private const string ListIntroTemplate = "Verfügbare Anwendungen: {{count}}.";
    private const string ListItemTemplate = "Nummer {{number}}: {{name}}.";
    private const string ListSelectPrompt = "Sagen Sie die Nummer oder den Namen.";
    private const string ConfirmationTemplate = "{{name}}. Ist das richtig?";
    private const string LaunchTemplate = "Starte {{name}}.";

    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(25);

    [Fact]
    public async Task Workflow_ThreeAmbiguousApplications_AnnouncesStableOrderAndLaunchesOnlyAfterUniqueNumber()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var catalog = CreateCatalog(
            ConfirmationPolicy.Always,
            CreateApplication("reise-alpha", "Reise Alpha"),
            CreateApplication("reise-beta", "Reise Beta"),
            CreateApplication("reise-gamma", "Reise Gamma"),
            CreateApplication("werkzeuge", "Werkzeuge"));
        var catalogClient = new FixedFormsCatalogDataClient(catalog);
        var userLanguageState = CreateGermanUserLanguageState();
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();

        // Die vorbereitete Zustimmung behält beim vorgeschalteten Spracheinstieg Deutsch bereits im ersten Angebot.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 5);
        speechInputClient.EnqueueTranscript("Okay");
        speechInputClient.EnqueueTranscript("Reise", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("zwei", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("Nachfolger erreicht");

        var catalogDialog = FormsCatalogDialogTestFactory.Create(
            interactionSignals,
            speechInputClient,
            textToSpeechPlayer,
            catalogClient,
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            userLanguageState);
        var assistantContext = SpeechAssistantContextTestFactory.Create(
            interactionSignals,
            userLanguageState,
            speechInputClient,
            textToSpeechPlayer,
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            catalogDialog,
            new FixedFormSelectionDialog("main-form"),
            new EchoVoiceCommandDialog(speechInputClient, textToSpeechPlayer),
            NullLogger<SpeechAssistantService>.Instance,
            catalogClient);

        await using var assistant = new SpeechAssistantService(assistantContext);
        await assistant.StartAsync(CancellationToken.None);

        await fixture.WaitUntilAsync(
            () => textToSpeechPlayer.Calls.Any(call => call.Text == "Nachfolger erreicht"),
            EndToEndTimeout);

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var ambiguityIndex = FindIndex(spoken, "Ich habe 3 passende Anwendungen gefunden.");
        var firstIndex = FindIndex(spoken, "Nummer 1: Reise Alpha.", ambiguityIndex + 1);
        var secondIndex = FindIndex(spoken, "Nummer 2: Reise Beta.", firstIndex + 1);
        var thirdIndex = FindIndex(spoken, "Nummer 3: Reise Gamma.", secondIndex + 1);
        var selectPromptIndex = FindIndex(spoken, ListSelectPrompt, thirdIndex + 1);
        var confirmationIndex = FindIndex(spoken, "Reise Beta. Ist das richtig?", selectPromptIndex + 1);
        var launchIndex = FindIndex(spoken, "Starte Reise Beta.", confirmationIndex + 1);

        Assert.True(ambiguityIndex >= 0, DescribeCalls(spoken));
        Assert.True(firstIndex > ambiguityIndex, DescribeCalls(spoken));
        Assert.True(secondIndex > firstIndex, DescribeCalls(spoken));
        Assert.True(thirdIndex > secondIndex, DescribeCalls(spoken));
        Assert.True(selectPromptIndex > thirdIndex, DescribeCalls(spoken));
        Assert.True(confirmationIndex > selectPromptIndex, DescribeCalls(spoken));
        Assert.True(launchIndex > confirmationIndex, DescribeCalls(spoken));

        var ambiguityWindow = spoken[(ambiguityIndex + 1)..confirmationIndex];
        Assert.Equal(1, ambiguityWindow.Count(text => text == "Nummer 1: Reise Alpha."));
        Assert.Equal(1, ambiguityWindow.Count(text => text == "Nummer 2: Reise Beta."));
        Assert.Equal(1, ambiguityWindow.Count(text => text == "Nummer 3: Reise Gamma."));
        Assert.DoesNotContain(ambiguityWindow, text => text.Contains("Ist das richtig", StringComparison.Ordinal));
        Assert.DoesNotContain(ambiguityWindow, text => text.StartsWith("Starte ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CatalogDialog_SixAmbiguousApplications_RefinesSilentlyUntilExplicitListWithOriginalNumbers()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var catalog = CreateCatalog(
            ConfirmationPolicy.Always,
            CreateApplication("team-alpha", "Alpha Team"),
            CreateApplication("other-bravo", "Bravo Team"),
            CreateApplication("team-charlie", "Charlie Team"),
            CreateApplication("other-delta", "Delta Team"),
            CreateApplication("team-echo", "Echo Team"),
            CreateApplication("team-foxtrot", "Foxtrot Team"));
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateCatalogDialog(catalog);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 4);
        speechInputClient.EnqueueTranscript("Team", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("alle Anwendungen", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("fuenf", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 95);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync("de", "ambiguity-four", cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal("team-echo", result.SelectedApplicationId);

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var refineIndex = FindIndex(spoken, "Ich habe 6 passende Anwendungen gefunden. Nennen Sie einen genaueren Teil des Namens, sagen Sie „alle Anwendungen“ für die vollständige Liste oder „abbrechen“.");
        var listIntroIndex = FindIndex(spoken, "Verfügbare Anwendungen: 6.", refineIndex + 1);
        var firstIndex = FindIndex(spoken, "Nummer 1: Alpha Team.", listIntroIndex + 1);
        var thirdIndex = FindIndex(spoken, "Nummer 3: Charlie Team.", firstIndex + 1);
        var fifthIndex = FindIndex(spoken, "Nummer 5: Echo Team.", thirdIndex + 1);
        var sixthIndex = FindIndex(spoken, "Nummer 6: Foxtrot Team.", fifthIndex + 1);
        var selectPromptIndex = FindIndex(spoken, ListSelectPrompt, sixthIndex + 1);
        var confirmationIndex = FindIndex(spoken, "Echo Team. Ist das richtig?", selectPromptIndex + 1);
        var launchIndex = FindIndex(spoken, "Starte Echo Team.", confirmationIndex + 1);

        Assert.True(refineIndex >= 0, DescribeCalls(spoken));
        Assert.True(listIntroIndex > refineIndex, DescribeCalls(spoken));
        Assert.DoesNotContain(
            spoken[(refineIndex + 1)..listIntroIndex],
            text => text.Contains("Alpha Team", StringComparison.Ordinal)
                || text.Contains("Charlie Team", StringComparison.Ordinal)
                || text.Contains("Echo Team", StringComparison.Ordinal)
                || text.Contains("Foxtrot Team", StringComparison.Ordinal));
        Assert.True(firstIndex > listIntroIndex, DescribeCalls(spoken));
        Assert.True(thirdIndex > firstIndex, DescribeCalls(spoken));
        Assert.True(fifthIndex > thirdIndex, DescribeCalls(spoken));
        Assert.True(sixthIndex > fifthIndex, DescribeCalls(spoken));
        Assert.True(selectPromptIndex > sixthIndex, DescribeCalls(spoken));
        Assert.True(confirmationIndex > selectPromptIndex, DescribeCalls(spoken));
        Assert.True(launchIndex > confirmationIndex, DescribeCalls(spoken));
    }

    [Fact]
    public async Task CatalogDialog_DuplicateExactDisplayNames_RemainsAmbiguousUntilStableNumberSelection()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var catalog = CreateCatalog(
            ConfirmationPolicy.Always,
            CreateApplication("duplicate-alpha", "Gemeinsam"),
            CreateApplication("duplicate-beta", "Gemeinsam"),
            CreateApplication("duplicate-gamma", "Gemeinsam"));
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateCatalogDialog(catalog);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 3);
        speechInputClient.EnqueueTranscript("Gemeinsam", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("drei", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 95);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync("de", "ambiguity-duplicate", cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal("duplicate-gamma", result.SelectedApplicationId);

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var ambiguityIndex = FindIndex(spoken, "Ich habe 3 passende Anwendungen gefunden.");
        var firstIndex = FindIndex(spoken, "Nummer 1: Gemeinsam.", ambiguityIndex + 1);
        var secondIndex = FindIndex(spoken, "Nummer 2: Gemeinsam.", firstIndex + 1);
        var thirdIndex = FindIndex(spoken, "Nummer 3: Gemeinsam.", secondIndex + 1);
        var confirmationIndex = FindIndex(spoken, "Gemeinsam. Ist das richtig?", thirdIndex + 1);
        var launchIndex = FindIndex(spoken, "Starte Gemeinsam.", confirmationIndex + 1);

        Assert.True(ambiguityIndex >= 0, DescribeCalls(spoken));
        Assert.True(firstIndex > ambiguityIndex, DescribeCalls(spoken));
        Assert.True(secondIndex > firstIndex, DescribeCalls(spoken));
        Assert.True(thirdIndex > secondIndex, DescribeCalls(spoken));
        Assert.True(confirmationIndex > thirdIndex, DescribeCalls(spoken));
        Assert.True(launchIndex > confirmationIndex, DescribeCalls(spoken));
        Assert.DoesNotContain(
            spoken[(ambiguityIndex + 1)..confirmationIndex],
            text => text.Contains("Ist das richtig", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FormSelectionDialog_ThreeForms_AnnouncesEveryStableNumberAndDoesNotPreselect()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var catalog = CreateCatalog(
            ConfirmationPolicy.Never,
            CreateApplicationWithForms(
                "forms-app",
                "Formularsammlung",
                ("form-alpha", "Antrag Alpha"),
                ("form-beta", "Antrag Beta"),
                ("form-gamma", "Antrag Gamma")));
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var dialog = FormSelectionDialogTestFactory.Create(
            interactionSignals,
            speechInputClient,
            textToSpeechPlayer,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new FormSelectionMatcher(),
            CreateGermanUserLanguageState());

        await fixture.EnqueueUtterancesAsync(speechInputClient, 1);
        speechInputClient.EnqueueTranscript("zwei", confidencePercent: 95);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync(
            "de",
            "forms-app",
            "form-ambiguity",
            Array.Empty<string>(),
            cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal("form-beta", result.SelectedFormId);

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var announcementIndex = FindIndex(spoken, "Diese Anwendung hat 3 Formulare. Sagen Sie alle Formulare für eine Liste.");
        var firstIndex = FindIndex(spoken, "Nummer 1: Antrag Alpha.", announcementIndex + 1);
        var secondIndex = FindIndex(spoken, "Nummer 2: Antrag Beta.", firstIndex + 1);
        var thirdIndex = FindIndex(spoken, "Nummer 3: Antrag Gamma.", secondIndex + 1);
        var selectPromptIndex = FindIndex(spoken, "Sagen Sie die Nummer, den Namen oder „abbrechen“.", thirdIndex + 1);
        var launchIndex = FindIndex(spoken, "Starte Antrag Beta.", selectPromptIndex + 1);

        Assert.True(announcementIndex >= 0, DescribeCalls(spoken));
        Assert.True(firstIndex > announcementIndex, DescribeCalls(spoken));
        Assert.True(secondIndex > firstIndex, DescribeCalls(spoken));
        Assert.True(thirdIndex > secondIndex, DescribeCalls(spoken));
        Assert.True(selectPromptIndex > thirdIndex, DescribeCalls(spoken));
        Assert.True(launchIndex > selectPromptIndex, DescribeCalls(spoken));
        Assert.DoesNotContain(
            spoken[..launchIndex],
            text => text.Contains("Ist das richtig", StringComparison.Ordinal)
                || text.StartsWith("Starte ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CatalogDialog_AmbiguousDescribeNumber_SpeaksDescriptionWithoutLaunchingAndResetsSelection()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var catalog = CreateCatalog(
            ConfirmationPolicy.Never,
            CreateApplication("reise-alpha", "Reise Alpha"),
            CreateApplication("reise-beta", "Reise Beta"),
            CreateApplication("reise-gamma", "Reise Gamma"),
            CreateApplication("werkzeuge", "Werkzeuge"));
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateCatalogDialog(catalog);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 3);
        speechInputClient.EnqueueTranscript("beschreibe Reise", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("zwei", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("vier", confidencePercent: 95);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync("de", "ambiguity-describe", cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal("werkzeuge", result.SelectedApplicationId);

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var ambiguityIndex = FindIndex(spoken, "Ich habe 3 passende Anwendungen gefunden.");
        var firstIndex = FindIndex(spoken, "Nummer 1: Reise Alpha.", ambiguityIndex + 1);
        var secondIndex = FindIndex(spoken, "Nummer 2: Reise Beta.", firstIndex + 1);
        var thirdIndex = FindIndex(spoken, "Nummer 3: Reise Gamma.", secondIndex + 1);
        var descriptionIndex = FindIndex(spoken, "Beschreibung für Reise Beta", thirdIndex + 1);
        var laterLaunchIndex = FindIndex(spoken, "Starte Werkzeuge.", descriptionIndex + 1);

        Assert.True(ambiguityIndex >= 0, DescribeCalls(spoken));
        Assert.True(firstIndex > ambiguityIndex, DescribeCalls(spoken));
        Assert.True(secondIndex > firstIndex, DescribeCalls(spoken));
        Assert.True(thirdIndex > secondIndex, DescribeCalls(spoken));
        Assert.True(descriptionIndex > thirdIndex, DescribeCalls(spoken));
        Assert.True(laterLaunchIndex > descriptionIndex, DescribeCalls(spoken));
        Assert.DoesNotContain(spoken, text => text == "Starte Reise Beta.");
        Assert.DoesNotContain(
            spoken[(descriptionIndex + 1)..laterLaunchIndex],
            text => text.Contains("Ist das richtig", StringComparison.Ordinal));
    }

    private static (FormsCatalogDialog Dialog, QueuedSpeechInputClient SpeechInputClient, RecordingTextToSpeechPlayer TextToSpeechPlayer)
        CreateCatalogDialog(FormsCatalog catalog)
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var dialog = FormsCatalogDialogTestFactory.Create(
            interactionSignals,
            speechInputClient,
            textToSpeechPlayer,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            CreateGermanUserLanguageState());

        return (dialog, speechInputClient, textToSpeechPlayer);
    }

    private static UserLanguageState CreateGermanUserLanguageState()
    {
        var state = new UserLanguageState();
        state.SetSelectedIsoCode("de");
        state.SetSelectedPolitenessLevel(PolitenessLevel.Formal);
        return state;
    }

    private static FormsCatalog CreateCatalog(
        ConfirmationPolicy confirmationPolicy,
        params ApplicationDescriptor[] applications)
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            VoiceDialogSettings = new VoiceDialogSettings
            {
                ConfirmationPolicy = confirmationPolicy,
                PauseBetweenItemsMs = 0
            },
            SelectionDialog = CreateSelectionDialog(),
            Applications = applications.ToList()
        };
    }

    private static ApplicationDescriptor CreateApplication(string id, string displayName)
        => CreateApplicationWithForms(id, displayName, ("main-form", "Hauptformular"));

    private static ApplicationDescriptor CreateApplicationWithForms(
        string id,
        string displayName,
        params (string FormId, string DisplayName)[] forms)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = id,
            DisplayName = new LocalizedText { ["de"] = displayName },
            Description = new LocalizedText { ["de"] = $"Beschreibung für {displayName}" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = forms
                .Select((form, index) => new FormDescriptor
                {
                    FormId = FormId.From(form.FormId),
                    DisplayName = new LocalizedText { ["de"] = form.DisplayName },
                    Description = new LocalizedText { ["de"] = $"Beschreibung für {form.DisplayName}" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"],
                    Sequence = index + 1
                })
                .ToList()
        };
    }

    private static ApplicationSelectionDialog CreateSelectionDialog()
    {
        return new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "5",
            SelectionDialogId = "catalog-ambiguity.behavior-test",
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Prompts =
            [
                CreatePrompt(CatalogDialogPromptIds.AppAnnouncingSingle, "Es gibt eine Anwendung: {{name}}."),
                CreatePrompt(CatalogDialogPromptIds.AppAnnouncingMultiple, "Es gibt {{count}} Anwendungen."),
                CreatePrompt(CatalogDialogPromptIds.AppAmbiguityShortIntro, AmbiguityShortTemplate),
                CreatePrompt(CatalogDialogPromptIds.AppAmbiguityRefineOrList, AmbiguityRefineOrListTemplate),
                CreatePrompt(CatalogDialogPromptIds.AppListIntro, ListIntroTemplate),
                CreatePrompt(CatalogDialogPromptIds.AppListItem, ListItemTemplate),
                CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt, ListSelectPrompt),
                CreatePrompt(CatalogDialogPromptIds.AppSearchResultsIntro, "Gefunden: {{count}} Anwendungen für {{query}}."),
                CreatePrompt(CatalogDialogPromptIds.AppSearchResultsItem, ListItemTemplate),
                CreatePrompt(CatalogDialogPromptIds.AppConfirmation, ConfirmationTemplate),
                CreatePrompt(CatalogDialogPromptIds.AppLowConfidenceConfirm, "Ich habe {{name}} verstanden. Ist das richtig?"),
                CreatePrompt(CatalogDialogPromptIds.AppExplicitAuthorization, "{{name}} ist noch nicht bestätigt. Sagen Sie Nummer {{number}}."),
                CreatePrompt(CatalogDialogPromptIds.AppVerificationUnavailable, "Die Bestätigung für {{name}} konnte nicht geprüft werden."),
                CreatePrompt(CatalogDialogPromptIds.AppNotOpenedReselect, "{{name}} wurde nicht geöffnet. Wählen Sie erneut."),
                CreatePrompt(CatalogDialogPromptIds.AppConfirmationRepeat, "Bitte sagen Sie ja oder nein."),
                CreatePrompt(CatalogDialogPromptIds.AppLaunch, LaunchTemplate),
                CreatePrompt(CatalogDialogPromptIds.AppHelp, "Sagen Sie Nummer, Name, alle Anwendungen oder abbrechen."),
                CreatePrompt(CatalogDialogPromptIds.AppCancelled, "Anwendungsauswahl abgebrochen."),
                CreatePrompt(CatalogDialogPromptIds.AppChangeLanguage, "Sagen Sie Sprache wechseln plus Zielsprache."),
                CreatePrompt(CatalogDialogPromptIds.AppUnrecognized, "Das habe ich nicht verstanden. Bitte wiederholen Sie."),
                CreatePrompt(CatalogDialogPromptIds.AppNoInput, "Ich habe nichts gehört. Bitte sprechen Sie noch einmal."),
                CreatePrompt(CatalogDialogPromptIds.AppReListen, "Verstanden. Ich höre weiter zu."),
                CreatePrompt(CatalogDialogPromptIds.AppNoApplications, "Keine Anwendungen verfügbar."),
                CreatePrompt(CatalogDialogPromptIds.AppNoSearchResults, "Keine Anwendungen für {{query}} gefunden."),
                CreatePrompt(CatalogDialogPromptIds.AppNoMatch, "{{name}} wurde nicht gefunden."),
                CreatePrompt(CatalogDialogPromptIds.AppInvalidNumber, "Bitte wählen Sie eine Zahl zwischen 1 und {{max}}."),
                CreatePrompt(CatalogDialogPromptIds.AppNoDescription, "Keine Beschreibung verfügbar."),
                CreatePrompt(CatalogDialogPromptIds.FormAnnouncing, "Diese Anwendung hat {{count}} Formulare. Sagen Sie alle Formulare für eine Liste."),
                CreatePrompt(CatalogDialogPromptIds.FormAmbiguityShortIntro, "Ich habe {{count}} passende Formulare gefunden."),
                CreatePrompt(CatalogDialogPromptIds.FormAmbiguityRefineOrList, "Ich habe {{count}} passende Formulare gefunden. Nennen Sie einen genaueren Teil des Namens, sagen Sie „alle Formulare“ für die vollständige Liste oder „abbrechen“."),
                CreatePrompt(CatalogDialogPromptIds.FormSingle, "Starte Formular {{name}}."),
                CreatePrompt(CatalogDialogPromptIds.FormListIntro, "Verfügbare Formulare: {{count}}."),
                CreatePrompt(CatalogDialogPromptIds.FormListItem, "Nummer {{number}}: {{name}}."),
                CreatePrompt(CatalogDialogPromptIds.FormListSelectPrompt, "Sagen Sie die Nummer, den Namen oder „abbrechen“."),
                CreatePrompt(CatalogDialogPromptIds.FormConfirmation, "{{name}}. Ist das richtig?"),
                CreatePrompt(CatalogDialogPromptIds.FormLowConfidenceConfirm, "Ich habe {{name}} verstanden. Ist das richtig?"),
                CreatePrompt(CatalogDialogPromptIds.FormConfirmationRepeat, "Bitte sagen Sie ja oder nein."),
                CreatePrompt(CatalogDialogPromptIds.FormExplicitAuthorization, "{{name}} ist noch nicht bestätigt. Sagen Sie Nummer {{number}}."),
                CreatePrompt(CatalogDialogPromptIds.FormVerificationUnavailable, "Die Bestätigung für {{name}} konnte nicht geprüft werden."),
                CreatePrompt(CatalogDialogPromptIds.FormNotOpenedReselect, "Das Formular {{name}} wurde nicht geöffnet. Wählen Sie erneut."),
                CreatePrompt(CatalogDialogPromptIds.FormLaunch, "Starte {{name}}."),
                CreatePrompt(CatalogDialogPromptIds.FormPrerequisitesNotMet, "Das Formular {{name}} benötigt zuerst {{missingPrerequisites}}."),
                CreatePrompt(CatalogDialogPromptIds.FormHelp, "Sagen Sie alle Formulare, Name, Nummer, wiederholen oder abbrechen."),
                CreatePrompt(CatalogDialogPromptIds.FormCancelled, "Formularauswahl abgebrochen."),
                CreatePrompt(CatalogDialogPromptIds.FormUnrecognized, "Das habe ich nicht verstanden. Bitte wiederholen Sie oder sagen Sie Hilfe."),
                CreatePrompt(CatalogDialogPromptIds.FormNoInput, "Ich habe nichts gehört. Bitte sprechen Sie noch einmal."),
                CreatePrompt(CatalogDialogPromptIds.FormReListen, "Verstanden. Ich höre weiter zu."),
                CreatePrompt(CatalogDialogPromptIds.FormNoForms, "Diese Anwendung hat keine Formulare."),
                CreatePrompt(CatalogDialogPromptIds.FormNoMatch, "{{name}} wurde nicht gefunden."),
                CreatePrompt(CatalogDialogPromptIds.FormInvalidNumber, "Bitte wählen Sie eine Zahl zwischen 1 und {{max}}.")
            ],
            Intents = [.. SpeechAssistantCatalogIntentHints.CreateIntents()],
            NumberWords = [.. SpeechAssistantCatalogIntentHints.CreateNumberWords()]
        };
    }

    private static CatalogVoicePrompt CreatePrompt(string promptId, string text)
    {
        return new CatalogVoicePrompt
        {
            PromptId = promptId,
            Text = new LocalizedText { ["de"] = text }
        };
    }

    private static int FindIndex(IReadOnlyList<string> calls, string expected, int startIndex = 0)
    {
        for (var index = Math.Max(0, startIndex); index < calls.Count; index++)
        {
            if (string.Equals(calls[index], expected, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private static string DescribeCalls(IEnumerable<string> calls)
        => $"Spoken calls: {string.Join(" | ", calls)}";
}
