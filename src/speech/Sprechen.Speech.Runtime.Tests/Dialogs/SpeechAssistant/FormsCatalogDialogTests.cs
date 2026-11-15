using System.Security.Cryptography;
using System.Text;
using Sprechen.Proto;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Mda.Contracts.V1.Identifiers;
using AppSpeechTranscript = Sprechen.Speech.App.Contracts.Services.SpeechTranscript;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class FormsCatalogDialogTests
{
    [Fact]
    public async Task ExecuteAsync_WhenMaxRetriesIsZero_FirstCaptureFailureTerminatesWithoutConstructionCrash()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var catalog = CreateCatalog(
            new VoiceDialogSettings
            {
                ConfirmationPolicy = ConfirmationPolicy.Never,
                MaxRetries = 0
            },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"]));
        await speechClient.EnqueueRecordStepAsync(
            _ => Task.FromException<CapturedSpeechAudio>(
                new OperationCanceledException("Synthetic capture interruption.")));
        var dialog = CreateDialog(
            new NoOpClientMessageBus(),
            speechClient,
            tts,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new UserLanguageState(),
            trace);

        var result = await dialog.ExecuteAsync("de", CancellationToken.None);

        Assert.True(result.WasCancelled);
        Assert.Equal(DialogCancellationReason.CaptureFaultBudgetExhausted, result.CancellationReason);
        Assert.Equal(1, speechClient.RecordCallCount);
        var terminalPrompt = Assert.Single(
            tts.Calls,
            call => call.Text.Contains("Aufnahmefehler", StringComparison.Ordinal));
        Assert.Contains(
            trace.Entries,
            entry => entry.Message ==
                "catalog-selection event=prompt_scheduled promptId=selection.cancelled promptSha256=" +
                ComputePromptSha256(terminalPrompt.Text));
    }

    [Fact]
    public async Task ExecuteAsync_FiltersApplicationsByLanguage()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("app.de", "Deutsch App", supportedLanguages: ["de"]),
            CreateApplication("app.en", "English App", supportedLanguages: ["en"]));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        await EnqueueUtterancesAsync(speechClient, 1);
        speechClient.EnqueueTranscript("eins", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            intentParser,
            new UserLanguageState(),
            trace);

        var result = await dialog.ExecuteAsync("de", "application-selection", CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("app.de", result.SelectedApplicationId);
        Assert.Equal(1, result.FormCount);

        Assert.Equal(1, speechClient.RecordCallCount);
        Assert.Equal(1, speechClient.SpeechToTextCallCount);

        var calls = tts.Calls.ToArray();
        Assert.Equal(2, calls.Length);
        Assert.Contains("Deutsch App", calls[0].Text);
        Assert.Contains("Deutsch App", calls[1].Text);
        var evidenceEntries = trace.Entries
            .Where(static entry => entry.Message.StartsWith("catalog-selection event=", StringComparison.Ordinal))
            .ToArray();
        Assert.Collection(
            evidenceEntries,
            entry => Assert.Equal(
                "catalog-selection event=prompt_scheduled promptId=selection.announce promptSha256=" +
                ComputePromptSha256(calls[0].Text),
                entry.Message),
            entry => Assert.Equal(
                "catalog-selection event=candidates_matched candidateIds=[app.de]",
                entry.Message),
            entry => Assert.Equal(
                "catalog-selection event=candidate_chosen candidateId=app.de",
                entry.Message),
            entry => Assert.Equal(
                "catalog-selection event=prompt_scheduled promptId=selection.launch candidateId=app.de promptSha256=" +
                ComputePromptSha256(calls[1].Text),
                entry.Message),
            entry => Assert.Equal(
                "catalog-selection event=selection_launch candidateId=app.de",
                entry.Message));
        Assert.All(evidenceEntries, entry => Assert.Equal("application-selection", entry.CorrelationId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenListeningInputIsUnrecognized_SpeaksUnrecognizedPromptAndReListens()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"]));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript("ja", confidencePercent: 95);
        speechClient.EnqueueTranscript("eins", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            intentParser,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync("de", CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha", result.SelectedApplicationId);
        Assert.Equal(2, speechClient.RecordCallCount);
        Assert.Contains(
            tts.Calls,
            call => call.Text.Contains("Das habe ich nicht verstanden", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserCancelsDirectlyFromListening_TracesCancelledPromptFromAppliedTerminal()
    {
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"]));

        await EnqueueUtterancesAsync(speechClient, 1);
        speechClient.EnqueueTranscript("abbrechen", confidencePercent: 95);
        var dialog = CreateDialog(
            new NoOpClientMessageBus(),
            speechClient,
            tts,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new UserLanguageState(),
            trace);

        var result = await dialog.ExecuteAsync(
            "de",
            "application-direct-cancel",
            CancellationToken.None);

        Assert.True(result.WasCancelled);
        Assert.Equal(DialogCancellationReason.UserCancelled, result.CancellationReason);
        var terminalPrompt = Assert.Single(
            tts.Calls,
            call => call.Text.Contains("abgebrochen", StringComparison.Ordinal));
        var terminalDigest = ComputePromptSha256(terminalPrompt.Text);
        Assert.Contains(
            trace.Entries,
            entry => entry.Message ==
                $"catalog-selection event=prompt_scheduled promptId=selection.cancelled promptSha256={terminalDigest}");
        Assert.DoesNotContain(
            trace.Entries,
            entry => entry.Message ==
                $"catalog-selection event=prompt_scheduled promptId=selection.listening promptSha256={terminalDigest}");
    }

    [Fact]
    public async Task ExecuteAsync_SearchUsesTags_AndSelectionByNumberUsesFilteredResults()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"], tags: ["other"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"], tags: ["target"]));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript("suche nach target", confidencePercent: 95);
        speechClient.EnqueueTranscript("zwei", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            intentParser,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync("de", CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("beta", result.SelectedApplicationId);

        Assert.Equal(2, speechClient.RecordCallCount);
        Assert.Equal(2, speechClient.SpeechToTextCallCount);

        var calls = tts.Calls.ToArray();
        Assert.Equal(8, calls.Length);
        Assert.True(calls.Count(call => call.Text.Contains("Beta", StringComparison.Ordinal)) >= 3);
        Assert.Contains(
            calls,
            call => call.Text.Contains("Nummer 2", StringComparison.Ordinal)
                && call.Text.Contains("Beta", StringComparison.Ordinal));
        Assert.Contains("Beta", calls[^1].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenApplicationSearchHasNoResults_EvidenceHashesUserDerivedPromptWithoutLeakingQuery()
    {
        const string secretQuery = "privatcodeqzxv7391";
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"]));

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript($"suche nach {secretQuery}", confidencePercent: 95);
        speechClient.EnqueueTranscript("eins", confidencePercent: 95);

        var dialog = CreateDialog(
            new NoOpClientMessageBus(),
            speechClient,
            tts,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new UserLanguageState(),
            trace);

        var result = await dialog.ExecuteAsync(
            "de",
            "application-selection-privacy",
            CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha", result.SelectedApplicationId);
        var userDerivedPrompt = Assert.Single(
            tts.Calls,
            call => call.Text.Contains(secretQuery, StringComparison.Ordinal));
        var expectedDigest = ComputePromptSha256(userDerivedPrompt.Text);
        var evidenceEntries = trace.Entries
            .Where(static entry => entry.Message.StartsWith("catalog-selection event=", StringComparison.Ordinal))
            .ToArray();
        Assert.Contains(
            evidenceEntries,
            entry => entry.Message ==
                $"catalog-selection event=prompt_scheduled promptId=selection.listening promptSha256={expectedDigest}");
        Assert.All(trace.Entries, entry => Assert.DoesNotContain(secretQuery, entry.Message, StringComparison.Ordinal));
        Assert.All(
            evidenceEntries.Where(static entry => entry.Message.Contains("event=prompt_scheduled", StringComparison.Ordinal)),
            entry => Assert.DoesNotContain(" text=", entry.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfirmationPolicyAlways_SelectByNumber_RequiresConfirmation()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Always },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"]));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript("eins", confidencePercent: 95);
        speechClient.EnqueueTranscript("ja", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            intentParser,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync("de", CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha", result.SelectedApplicationId);
        Assert.Equal(2, speechClient.RecordCallCount);
        Assert.Equal(2, speechClient.SpeechToTextCallCount);

        var calls = tts.Calls.ToArray();
        Assert.Equal(6, calls.Length);
        Assert.Contains("Alpha", calls[^2].Text, StringComparison.Ordinal);
        Assert.Contains("Ist das richtig", calls[^2].Text, StringComparison.Ordinal);
        Assert.Contains("Alpha", calls[^1].Text, StringComparison.Ordinal);
    }

    // Der genaue Anwendungsname bei Konfidenz 63 verlangt in allen fünf Sprachen die normale Bestätigung; eine Zustimmung wählt das einfache Beispiel.
    // Beide Beispiele teilen ein Wort im Namen. Die vollständige lokalisierte Bezeichnung
    // muss die gewählte Anwendung in der normalen Bestätigung unterscheiden.
    [Theory]
    [InlineData("de", "Einfaches Beispiel", "Komplexes Beispiel", "ja", "Ist das richtig", "Meinten Sie")]
    [InlineData("en", "Simple example", "Complex example", "yes", "Is that correct", "Did you mean")]
    [InlineData("fr", "Exemple simple", "Exemple complexe", "oui", "Est-ce correct", "Vouliez-vous dire")]
    [InlineData("es", "Ejemplo sencillo", "Ejemplo complejo", "si", "Es correcto", "Queria decir")]
    [InlineData("it", "Esempio semplice", "Esempio complesso", "si", "E corretto", "Intendevi")]
    public async Task ExecuteAsync_ExactUnambiguousNameAtLowConfidence_RoutesToCleanConfirm_AndLaunches(
        string isoCode,
        string simpleName,
        string complexName,
        string confirmWord,
        string cleanConfirmMarker,
        string hedgeMarker)
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Always },
            CreateExampleApplication("sample.simple", ("Einfaches Beispiel", "Simple example", "Exemple simple", "Ejemplo sencillo", "Esempio semplice")),
            CreateExampleApplication("sample.complex", ("Komplexes Beispiel", "Complex example", "Exemple complexe", "Ejemplo complejo", "Esempio complesso")));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript(simpleName, confidencePercent: 63);
        speechClient.EnqueueTranscript(confirmWord, confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            intentParser,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync(isoCode, CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("sample.simple", result.SelectedApplicationId);

        var calls = tts.Calls.ToArray();
        var confirmCall = calls[^2].Text;
        Assert.Contains(simpleName, confirmCall, StringComparison.Ordinal);
        Assert.Contains(cleanConfirmMarker, confirmCall, StringComparison.Ordinal);
        Assert.DoesNotContain(hedgeMarker, confirmCall, StringComparison.Ordinal);
        Assert.Contains(simpleName, calls[^1].Text, StringComparison.Ordinal);
        _ = complexName;
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfirmationPolicyAmbiguous_ExactNameSkipsConfirmation()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Ambiguous },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"]));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        await EnqueueUtterancesAsync(speechClient, 1);
        speechClient.EnqueueTranscript("Alpha", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            intentParser,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync("de", CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha", result.SelectedApplicationId);
        Assert.Equal(1, speechClient.RecordCallCount);
        Assert.Equal(1, speechClient.SpeechToTextCallCount);

        var calls = tts.Calls.ToArray();
        Assert.Equal(5, calls.Length);
        Assert.DoesNotContain(calls, call => call.Text.Contains("Ist das richtig", StringComparison.Ordinal));
        Assert.Contains("Alpha", calls[^1].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfirmationPolicyAmbiguous_PartialNameRequiresConfirmation()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Ambiguous },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"]));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript("Alp", confidencePercent: 95);
        speechClient.EnqueueTranscript("ja", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            intentParser,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync("de", CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha", result.SelectedApplicationId);
        Assert.Equal(2, speechClient.RecordCallCount);
        Assert.Equal(2, speechClient.SpeechToTextCallCount);

        var calls = tts.Calls.ToArray();
        Assert.Equal(6, calls.Length);
        Assert.Contains("Alpha", calls[^2].Text, StringComparison.Ordinal);
        Assert.Contains("Ist das richtig", calls[^2].Text, StringComparison.Ordinal);
        Assert.Contains("Alpha", calls[^1].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserRequestsLanguageChange_PersistsLanguageAndRestartsCatalog()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de", "en"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de", "en"]));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        await EnqueueUtterancesAsync(speechClient, 4);
        speechClient.EnqueueTranscript("sprache wechseln", confidencePercent: 95);
        speechClient.EnqueueTranscript("englisch", confidencePercent: 95);
        speechClient.EnqueueTranscript("ja", confidencePercent: 95);
        speechClient.EnqueueTranscript("one", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            intentParser,
            userLanguageState);

        var result = await dialog.ExecuteAsync("de", CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha", result.SelectedApplicationId);
        Assert.Equal("en", result.EffectiveIsoCode);
        Assert.Equal("en", userLanguageState.SelectedIsoCode);
        Assert.Contains(
            tts.Calls,
            call => call.LanguageCode == "en"
                && call.Text.Contains("There are 2 applications", StringComparison.Ordinal));
        Assert.Contains(
            tts.Calls,
            call => call.LanguageCode == "en"
                && call.Text.Contains("Say the number or name", StringComparison.Ordinal));
        Assert.DoesNotContain(tts.Calls, call => call.Text.Contains("Einstellungen", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDeclinesLanguageChange_SpeaksLanguageKeptAndStaysInCurrentLanguage()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de", "en"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de", "en"]));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        await EnqueueUtterancesAsync(speechClient, 4);
        speechClient.EnqueueTranscript("sprache wechseln", confidencePercent: 95);
        speechClient.EnqueueTranscript("englisch", confidencePercent: 95);
        speechClient.EnqueueTranscript("nein", confidencePercent: 95);
        speechClient.EnqueueTranscript("eins", confidencePercent: 95);

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, intentParser, userLanguageState);

        var result = await dialog.ExecuteAsync("de", CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha", result.SelectedApplicationId);
        Assert.Equal("de", result.EffectiveIsoCode);
        Assert.Equal("de", userLanguageState.SelectedIsoCode);
        Assert.Contains(
            tts.Calls,
            call => call.Text.Contains("Die Sprache bleibt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserCancelsLanguageChangeConfirmation_ExitsDialogWithSpokenCancel()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de", "en"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de", "en"]));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        await EnqueueUtterancesAsync(speechClient, 4);
        speechClient.EnqueueTranscript("sprache wechseln", confidencePercent: 95);
        speechClient.EnqueueTranscript("englisch", confidencePercent: 95);
        speechClient.EnqueueTranscript("abbrechen", confidencePercent: 95);
        speechClient.EnqueueTranscript("eins", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            intentParser,
            userLanguageState,
            trace);

        var result = await dialog.ExecuteAsync("de", "language-change-cancel", CancellationToken.None);

        // Abbrechen an der Sprachwechselbestätigung beendet den Dialog mit Rückmeldung; Nein behält die Sprache und setzt die Auswahl fort.
        Assert.True(result.WasCancelled);
        Assert.Equal("de", userLanguageState.SelectedIsoCode);
        var terminalPrompt = Assert.Single(
            tts.Calls,
            call => call.Text.Contains("abgebrochen", StringComparison.Ordinal));
        Assert.Contains(
            trace.Entries,
            entry => entry.Message ==
                "catalog-selection event=prompt_scheduled promptId=selection.cancelled promptSha256=" +
                ComputePromptSha256(terminalPrompt.Text));
    }

    [Fact]
    public async Task ExecuteAsync_WhenLanguageChangeConfirmationCaptureIsExhausted_TracesAppliedCaptureTerminal()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        var catalog = CreateCatalog(
            new VoiceDialogSettings
            {
                ConfirmationPolicy = ConfirmationPolicy.Never,
                MaxRetries = 0
            },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de", "en"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de", "en"]));
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));
        await speechClient.EnqueueRecordStepAsync(
            _ => Task.FromException<CapturedSpeechAudio>(
                new OperationCanceledException("Synthetic language-confirmation capture interruption.")));
        speechClient.EnqueueTranscript("sprache wechseln", confidencePercent: 95);
        speechClient.EnqueueTranscript("englisch", confidencePercent: 95);
        var dialog = CreateDialog(
            new NoOpClientMessageBus(),
            speechClient,
            tts,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            userLanguageState,
            trace);

        var result = await dialog.ExecuteAsync(
            "de",
            "language-change-capture-terminal",
            CancellationToken.None);

        Assert.True(result.WasCancelled);
        Assert.Equal(DialogCancellationReason.CaptureFaultBudgetExhausted, result.CancellationReason);
        Assert.Equal("de", userLanguageState.SelectedIsoCode);
        var terminalPrompt = Assert.Single(
            tts.Calls,
            call => call.Text.Contains("Aufnahmefehler", StringComparison.Ordinal));
        var terminalDigest = ComputePromptSha256(terminalPrompt.Text);
        Assert.Contains(
            trace.Entries,
            entry => entry.Message ==
                $"catalog-selection event=prompt_scheduled promptId=selection.cancelled promptSha256={terminalDigest}");
        Assert.DoesNotContain(
            trace.Entries,
            entry => entry.Message ==
                $"catalog-selection event=prompt_scheduled promptId=selection.listening promptSha256={terminalDigest}");
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserSelectedInformalPoliteness_UsesInformalSystemPrompt()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Informal);

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"]));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        await EnqueueUtterancesAsync(speechClient, 1);
        speechClient.EnqueueTranscript("eins", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            intentParser,
            userLanguageState);

        var result = await dialog.ExecuteAsync("de", CancellationToken.None);

        Assert.False(result.WasCancelled);
        var calls = tts.Calls.ToArray();
        Assert.True(calls.Length >= 4);
        var announcementText = string.Join(" ", calls.Take(4).Select(static call => call.Text));
        Assert.Contains("Alpha", announcementText, StringComparison.Ordinal);
        Assert.Contains("Beta", announcementText, StringComparison.Ordinal);
        Assert.Contains("Sag die Nummer oder den Namen", announcementText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitialTranscriptIsExactAndHighConfidence_SelectsWithoutListeningCapture()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"]));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            intentParser,
            new UserLanguageState(),
            new NoOpSpeechAssistantTrace());

        var result = await dialog.ExecuteAsync(
            "de",
            "test-correlation",
            new AppSpeechTranscript("Alpha", ConfidencePercent: 95),
            CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha", result.SelectedApplicationId);
        Assert.Equal(0, speechClient.RecordCallCount);
        Assert.Equal(0, speechClient.SpeechToTextCallCount);
        Assert.Collection(
            tts.Calls,
            call => Assert.Contains("Alpha", call.Text, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitialTranscriptConfidenceIsAbsent_ConfirmsBeforeLaunch()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"]));

        await EnqueueUtterancesAsync(speechClient, 1);
        speechClient.EnqueueTranscript("ja", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new UserLanguageState());

        var result = await dialog.ExecuteAsync(
            "de",
            "test-correlation",
            new AppSpeechTranscript("Alpha", ConfidencePercent: null),
            CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha", result.SelectedApplicationId);
        Assert.Equal(1, speechClient.RecordCallCount);
        Assert.Equal(1, speechClient.SpeechToTextCallCount);
        Assert.Contains(
            tts.Calls,
            call => call.Text.Contains("Alpha", StringComparison.Ordinal)
                && call.Text.Contains("Ist das richtig", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ConfirmationPolicy.Ambiguous)]
    [InlineData(ConfirmationPolicy.Never)]
    public async Task ExecuteAsync_WhenInitialTranscriptHasAmbiguousApplicationCandidate_ConfirmsBeforeLaunch(
        ConfirmationPolicy confirmationPolicy)
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = confirmationPolicy },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"]));

        await EnqueueUtterancesAsync(speechClient, 1);
        speechClient.EnqueueTranscript("ja", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new UserLanguageState());

        var result = await dialog.ExecuteAsync(
            "de",
            "test-correlation",
            new AppSpeechTranscript("Alp", ConfidencePercent: 95),
            CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha", result.SelectedApplicationId);
        Assert.Equal(1, speechClient.RecordCallCount);
        Assert.Equal(1, speechClient.SpeechToTextCallCount);
        var calls = tts.Calls.ToArray();
        Assert.Equal(2, calls.Length);
        Assert.Contains("Alpha", calls[0].Text, StringComparison.Ordinal);
        Assert.Contains("Ist das richtig", calls[0].Text, StringComparison.Ordinal);
        Assert.Contains("Alpha", calls[1].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitialTranscriptDoesNotResolveToCatalogIntent_FallsBackToAnnouncement()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"]));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        await EnqueueUtterancesAsync(speechClient, 1);
        speechClient.EnqueueTranscript("zwei", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            intentParser,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync(
            "de",
            "test-correlation",
            new AppSpeechTranscript("bitte Anwendungskatalog oeffnen", ConfidencePercent: 95),
            CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("beta", result.SelectedApplicationId);
        Assert.Equal(1, speechClient.RecordCallCount);
        Assert.Contains(
            tts.Calls,
            call => call.Text.Contains("Nummer 1", StringComparison.Ordinal)
                && call.Text.Contains("Alpha", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitialTranscriptIsPhaseInvalidAtEntry_FallsBackToAnnouncement()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]),
            CreateApplication("beta", "Beta", supportedLanguages: ["de"]));

        await EnqueueUtterancesAsync(speechClient, 1);
        speechClient.EnqueueTranscript("zwei", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new UserLanguageState(),
            trace);

        var result = await dialog.ExecuteAsync(
            "de",
            "test-correlation",
            new AppSpeechTranscript("okay", ConfidencePercent: 95),
            CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("beta", result.SelectedApplicationId);
        Assert.Equal(1, speechClient.RecordCallCount);
        Assert.Equal(1, speechClient.SpeechToTextCallCount);
        Assert.Contains(
            tts.Calls,
            call => call.Text.Contains("Nummer 1", StringComparison.Ordinal)
                && call.Text.Contains("Alpha", StringComparison.Ordinal));
        Assert.DoesNotContain(
            tts.Calls,
            call => call.Text.Contains("Das habe ich nicht verstanden", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            trace.Entries,
            entry => entry.Message == "Application catalog dialog discarded the prefetched transcript because it was not actionable.");
        Assert.Contains(
            trace.Entries,
            entry => entry.Message == "Selection runtime starts without a prefetched transcript.");
        Assert.DoesNotContain(
            trace.Entries,
            entry => entry.Message == "Application catalog dialog accepted the prefetched transcript for the first listening turn.");
        Assert.DoesNotContain(
            trace.Entries,
            entry => entry.Message == "Selection runtime starts with a prefetched transcript.");
    }

    [Fact]
    public async Task ExecuteAsync_WhenIsoCodeIsInvalid_ThrowsArgumentException()
    {
        var speechClient = new QueuedSpeechInputClient();
        var dialog = CreateDialog(
            new NoOpClientMessageBus(),
            speechClient,
            new RecordingTextToSpeechPlayer(),
            new FixedFormsCatalogDataClient(CreateCatalog(
                new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
                CreateApplication("alpha", "Alpha", supportedLanguages: ["de"]))),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new UserLanguageState(),
            new NoOpSpeechAssistantTrace());

        await Assert.ThrowsAsync<ArgumentException>(() => dialog.ExecuteAsync("german", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCatalogLoadFails_PropagatesFailure()
    {
        var mdaCatalog = new Mock<IFormsCatalogDataClient>(MockBehavior.Strict);
        mdaCatalog
            .Setup(client => client.GetFormsCatalogAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var speechClient = new QueuedSpeechInputClient();
        var dialog = CreateDialog(
            new NoOpClientMessageBus(),
            speechClient,
            new RecordingTextToSpeechPlayer(),
            mdaCatalog.Object,
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new UserLanguageState());

        await Assert.ThrowsAsync<InvalidOperationException>(() => dialog.ExecuteAsync("de", CancellationToken.None));
        mdaCatalog.VerifyAll();
    }

    private static async Task EnqueueUtterancesAsync(QueuedSpeechInputClient client, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await client.EnqueueRecordingAsync(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1 } }));
        }
    }

    private static string ComputePromptSha256(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    private static FormsCatalogDialog CreateDialog(
        ISpeechCaptureRecordingIndicator recordingIndicator,
        ISpeechInputClient speechInputClient,
        ITextToSpeechPlayer textToSpeechPlayer,
        IFormsCatalogDataClient formsCatalogDataClient,
        IFormsCatalogIntentParser intentParser,
        IUserLanguageState userLanguageState,
        ISpeechAssistantTrace? speechAssistantTrace = null,
        IUserLanguageSettingsWorkflow? userLanguageSettingsWorkflow = null)
    {
        ArgumentNullException.ThrowIfNull(recordingIndicator);
        ArgumentNullException.ThrowIfNull(speechInputClient);
        ArgumentNullException.ThrowIfNull(textToSpeechPlayer);
        ArgumentNullException.ThrowIfNull(formsCatalogDataClient);
        ArgumentNullException.ThrowIfNull(intentParser);
        ArgumentNullException.ThrowIfNull(userLanguageState);

        var languageSelectionWriter = userLanguageSettingsWorkflow as IUserLanguageSelectionWriter
            ?? CreateUserLanguageSettingsWorkflow(userLanguageState);

        return new FormsCatalogDialog(
            new SelectionDialogSpeechTurnRunner(
                recordingIndicator,
                speechInputClient,
                speechInputClient as IModeAwareSpeechCaptureClient
                    ?? throw new InvalidOperationException("FormsCatalogDialog tests require a mode-aware speech input double."),
                new SpeechRecognitionWorker(speechInputClient),
                textToSpeechPlayer,
                NullLogger<SelectionDialogSpeechTurnRunner>.Instance),
            formsCatalogDataClient,
            intentParser,
            new FormsCatalogLanguageChangeWorkflow(
                languageSelectionWriter,
                new LanguageChangeTargetResolver(VoiceInterfaceLanguageCatalog.CreateDefault())),
            userLanguageState,
            speechAssistantTrace ?? NullSpeechAssistantTrace.Instance);
    }

    private static UserLanguageSettingsWorkflow CreateUserLanguageSettingsWorkflow(IUserLanguageState userLanguageState)
    {
        ArgumentNullException.ThrowIfNull(userLanguageState);

        return new UserLanguageSettingsWorkflow(
            userLanguageState,
            Mock.Of<IUserLanguageSettingsStore>(),
            new SettingsVoiceLanguageResolver());
    }

    private static FormsCatalog CreateCatalog(VoiceDialogSettings? settings, params ApplicationDescriptor[] applications)
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

    private static ApplicationDescriptor CreateApplication(
        string id,
        string displayName,
        string[] supportedLanguages,
        string[]? tags = null)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = id,
            DisplayName = new LocalizedText { ["de"] = displayName },
            Description = new LocalizedText { ["de"] = displayName },
            SupportedLanguages = supportedLanguages.ToList(),
            DefaultLanguage = supportedLanguages[0],
            Tags = tags?.ToList(),
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("form-1"),
                    DisplayName = new LocalizedText { ["de"] = "Form 1" },
                    Description = new LocalizedText { ["de"] = "Form 1" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        };
    }

    // Lokalisierte Namen in allen fünf Sprachen erlauben die Auswahl derselben Anwendung in jeder Testsprache.
    private static ApplicationDescriptor CreateExampleApplication(
        string id,
        (string De, string En, string Fr, string Es, string It) names)
    {
        var displayName = new LocalizedText
        {
            ["de"] = names.De,
            ["en"] = names.En,
            ["fr"] = names.Fr,
            ["es"] = names.Es,
            ["it"] = names.It
        };

        return new ApplicationDescriptor
        {
            ApplicationId = id,
            DisplayName = displayName,
            Description = displayName,
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("form-1"),
                    DisplayName = new LocalizedText { ["de"] = "Form 1" },
                    Description = new LocalizedText { ["de"] = "Form 1" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        };
    }

    private static ApplicationSelectionDialog CreateSelectionDialog()
    {
        var sharedSelectionDialog = FormsCatalogIntentTestCatalog.Create().SelectionDialog;
        return new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "1",
            SelectionDialogId = "default.selectionDialog",
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
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
                    ("de@formal", "Es gibt {{count}} Anwendungen. Ich nenne sie jetzt."),
                    ("de@informal", "Es gibt {{count}} Anwendungen. Ich nenne sie jetzt."),
                    ("en", "There are {{count}} applications. Here they are."),
                    ("fr", "Il y a {{count}} applications. Les voici."),
                    ("es", "Hay {{count}} aplicaciones. Aqui estan."),
                    ("it", "Ci sono {{count}} applicazioni. Eccole.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppListIntro,
                    ("de", "Verfügbare Anwendungen: {{count}}."),
                    ("en", "Available applications: {{count}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppListItem,
                    ("de", "Nummer {{number}}: {{name}}."),
                    ("en", "Number {{number}}: {{name}}."),
                    ("fr", "Numero {{number}} : {{name}}."),
                    ("es", "Numero {{number}}: {{name}}."),
                    ("it", "Numero {{number}}: {{name}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppListSelectPrompt,
                    ("de", "Sagen Sie die Nummer oder den Namen."),
                    ("de@informal", "Sag die Nummer oder den Namen."),
                    ("en", "Say the number or name."),
                    ("fr", "Dites le numero ou le nom."),
                    ("es", "Diga el numero o el nombre."),
                    ("it", "Dica il numero o il nome.")),
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
                    ("en", "{{name}}. Is that correct?"),
                    ("fr", "{{name}}. Est-ce correct ?"),
                    ("es", "{{name}}. Es correcto?"),
                    ("it", "{{name}}. E corretto?")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppLowConfidenceConfirm,
                    ("de", "Ich bin unsicher. Meinten Sie {{name}}?"),
                    ("de@informal", "Ich bin unsicher. Meintest du {{name}}?"),
                    ("en", "I am unsure. Did you mean {{name}}?"),
                    ("fr", "Je ne suis pas sur. Vouliez-vous dire {{name}} ?"),
                    ("es", "No estoy seguro. Queria decir {{name}}?"),
                    ("it", "Non sono sicuro. Intendevi {{name}}?")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppConfirmationRepeat,
                    ("de", "Bitte sagen Sie ja oder nein."),
                    ("de@informal", "Bitte sag ja oder nein."),
                    ("en", "Please say yes or no.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppLaunch,
                    ("de", "Starte {{name}}."),
                    ("en", "Starting {{name}}."),
                    ("fr", "Demarrage de {{name}}."),
                    ("es", "Iniciando {{name}}."),
                    ("it", "Avvio di {{name}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppHelp,
                    ("de@formal", "Sagen Sie alle Anwendungen, Suche nach, Nummer, wiederholen oder abbrechen."),
                    ("de@informal", "Sag alle Anwendungen, Suche nach, Nummer, wiederholen oder abbrechen."),
                    ("en", "Say all applications, search for, number, repeat or cancel.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppCancelled,
                    ("de", "Anwendungsauswahl abgebrochen."),
                    ("en", "Application selection cancelled.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppChangeLanguage,
                    ("de", "Sagen Sie Sprache wechseln plus eine unterstützte Sprache."),
                    ("de@informal", "Sag Sprache wechseln plus eine unterstützte Sprache."),
                    ("en", "Say change language plus a supported language.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppUnrecognized,
                    ("de", "Das habe ich nicht verstanden. Bitte wiederholen Sie oder sagen Sie Hilfe."),
                    ("de@informal", "Das habe ich nicht verstanden. Bitte wiederhole oder sag Hilfe."),
                    ("en", "I did not understand that. Please repeat or say help.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppNoInput,
                    ("de@formal", "Ich habe nichts gehört. Bitte sprechen Sie noch einmal."),
                    ("de@informal", "Ich habe nichts gehört. Bitte sprich noch einmal."),
                    ("en", "I didn't hear anything. Please speak again.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppReListen,
                    ("de", "Verstanden. Ich höre weiter zu."),
                    ("en", "Got it. I am still listening.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppCaptureFailedTerminal,
                    ("de", "Aufnahmefehler. Die Anwendungsauswahl wird beendet."),
                    ("en", "Capture failed. Ending application selection.")),
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
                    ("de@informal", "Bitte wähle eine Zahl zwischen 1 und {{max}}."),
                    ("en", "Please choose a number between 1 and {{max}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppNoDescription,
                    ("de", "Keine Beschreibung verfügbar."),
                    ("en", "No description available."))
            ],
            Intents = [.. sharedSelectionDialog.Intents],
            NumberWords = [.. sharedSelectionDialog.NumberWords]
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
