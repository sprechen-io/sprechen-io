using System.Security.Cryptography;
using System.Text;
using Sprechen.Proto;
using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class FormSelectionDialogTests
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
            CreateMultiFormApplication("multi", ["de"], ("a", "Alpha"), ("b", "Beta")));
        await speechClient.EnqueueRecordStepAsync(
            _ => Task.FromException<CapturedSpeechAudio>(
                new OperationCanceledException("Synthetic capture interruption.")));
        var dialog = CreateDialog(
            new NoOpClientMessageBus(),
            speechClient,
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            tts,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new FormSelectionMatcher(),
            new UserLanguageState(),
            trace);

        var result = await dialog.ExecuteAsync("de", "multi", [], CancellationToken.None);

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
    public async Task ExecuteAsync_WhenPrerequisitesNotMet_DoesNotSelectFormAndKeepsListening()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            new ApplicationDescriptor
            {
                ApplicationId = "multi",
                DisplayName = new LocalizedText { ["de"] = "multi" },
                Description = new LocalizedText { ["de"] = "multi" },
                SupportedLanguages = ["de"],
                DefaultLanguage = "de",
                Forms =
                [
                    new FormDescriptor
                    {
                        FormId = FormId.From("part1"),
                        DisplayName = new LocalizedText { ["de"] = "Teil 1" },
                        Description = new LocalizedText { ["de"] = "Teil 1" },
                        LatestContractVersion = "1",
                        AvailableContractVersions = ["1"]
                    },
                    new FormDescriptor
                    {
                        FormId = FormId.From("part2"),
                        DisplayName = new LocalizedText { ["de"] = "Teil 2" },
                        Description = new LocalizedText { ["de"] = "Teil 2" },
                        LatestContractVersion = "1",
                        AvailableContractVersions = ["1"],
                        PrerequisiteForms = ["part1"]
                    }
                ]
            });

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);
        var matcher = new FormSelectionMatcher();

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript("zwei", confidencePercent: 95);
        speechClient.EnqueueTranscript("eins", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            tts,
            mdaCatalog,
            intentParser,
            matcher,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync("de", "multi", Array.Empty<string>(), CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("part1", result.SelectedFormId);

        var calls = tts.Calls.ToArray();
        Assert.Equal(6, calls.Length);
        var prerequisitePrompt = Assert.Single(
            calls,
            call => call.Text.Contains("benötigt", StringComparison.Ordinal));
        Assert.Contains("Teil 2", prerequisitePrompt.Text, StringComparison.Ordinal);
        Assert.Contains("Teil 1", prerequisitePrompt.Text, StringComparison.Ordinal);
        Assert.Contains("Teil 1", calls[^1].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfirmationPolicyNever_SelectsWithoutConfirming()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateMultiFormApplication("multi", ["de"], ("a", "Alpha"), ("b", "Beta")));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);
        var matcher = new FormSelectionMatcher();

        await EnqueueUtterancesAsync(speechClient, 1);
        speechClient.EnqueueTranscript("eins", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            tts,
            mdaCatalog,
            intentParser,
            matcher,
            new UserLanguageState(),
            trace);

        var result = await dialog.ExecuteAsync(
            "de",
            "multi",
            "form-selection",
            Array.Empty<string>(),
            CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("a", result.SelectedFormId);

        Assert.Equal(1, speechClient.RecordCallCount);
        Assert.Equal(1, speechClient.SpeechToTextCallCount);

        var calls = tts.Calls.ToArray();
        Assert.Equal(5, calls.Length);
        Assert.Contains("Nummer 1", calls[1].Text, StringComparison.Ordinal);
        Assert.Contains("Alpha", calls[1].Text, StringComparison.Ordinal);
        Assert.Contains("Nummer 2", calls[2].Text, StringComparison.Ordinal);
        Assert.Contains("Beta", calls[2].Text, StringComparison.Ordinal);
        Assert.Contains("Alpha", calls[^1].Text, StringComparison.Ordinal);
        var evidenceEntries = trace.Entries
            .Where(static entry => entry.Message.StartsWith("catalog-selection event=", StringComparison.Ordinal))
            .ToArray();
        Assert.Collection(
            evidenceEntries,
            entry => Assert.Equal(
                "catalog-selection event=prompt_scheduled promptId=selection.announce promptSha256=" +
                ComputePromptSha256(string.Join(' ', calls.Take(4).Select(static call => call.Text))),
                entry.Message),
            entry => Assert.Equal(
                "catalog-selection event=candidates_matched candidateIds=[a]",
                entry.Message),
            entry => Assert.Equal(
                "catalog-selection event=candidate_chosen candidateId=a",
                entry.Message),
            entry => Assert.Equal(
                "catalog-selection event=prompt_scheduled promptId=selection.launch candidateId=a promptSha256=" +
                ComputePromptSha256(calls[^1].Text),
                entry.Message),
            entry => Assert.Equal(
                "catalog-selection event=selection_launch candidateId=a",
                entry.Message));
        Assert.All(evidenceEntries, entry => Assert.Equal("form-selection", entry.CorrelationId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenFormNameHasNoMatch_EvidenceHashesUserDerivedPromptWithoutLeakingName()
    {
        const string secretName = "form-geheimnis-xqz-7391";
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateMultiFormApplication("multi", ["de"], ("a", "Alpha"), ("b", "Beta")));

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript(secretName, confidencePercent: 95);
        speechClient.EnqueueTranscript("eins", confidencePercent: 95);

        var dialog = CreateDialog(
            new NoOpClientMessageBus(),
            speechClient,
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            tts,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new FormSelectionMatcher(),
            new UserLanguageState(),
            trace);

        var result = await dialog.ExecuteAsync(
            "de",
            "multi",
            "form-selection-privacy",
            Array.Empty<string>(),
            CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("a", result.SelectedFormId);
        var userDerivedPrompt = Assert.Single(
            tts.Calls,
            call => call.Text.Contains(secretName, StringComparison.Ordinal));
        var expectedDigest = ComputePromptSha256(userDerivedPrompt.Text);
        var evidenceEntries = trace.Entries
            .Where(static entry => entry.Message.StartsWith("catalog-selection event=", StringComparison.Ordinal))
            .ToArray();
        Assert.Contains(
            evidenceEntries,
            entry => entry.Message ==
                $"catalog-selection event=prompt_scheduled promptId=selection.listening promptSha256={expectedDigest}");
        Assert.All(trace.Entries, entry => Assert.DoesNotContain(secretName, entry.Message, StringComparison.Ordinal));
        Assert.All(
            evidenceEntries.Where(static entry => entry.Message.Contains("event=prompt_scheduled", StringComparison.Ordinal)),
            entry => Assert.DoesNotContain(" text=", entry.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenListeningInputIsUnrecognized_SpeaksUnrecognizedPromptAndReListens()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateMultiFormApplication("multi", ["de"], ("a", "Alpha"), ("b", "Beta")));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);
        var matcher = new FormSelectionMatcher();

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript("ja", confidencePercent: 95);
        speechClient.EnqueueTranscript("eins", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            tts,
            mdaCatalog,
            intentParser,
            matcher,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync("de", "multi", Array.Empty<string>(), CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("a", result.SelectedFormId);
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
            CreateMultiFormApplication("multi", ["de"], ("a", "Alpha"), ("b", "Beta")));

        await EnqueueUtterancesAsync(speechClient, 1);
        speechClient.EnqueueTranscript("abbrechen", confidencePercent: 95);
        var dialog = CreateDialog(
            new NoOpClientMessageBus(),
            speechClient,
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            tts,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new FormSelectionMatcher(),
            new UserLanguageState(),
            trace);

        var result = await dialog.ExecuteAsync(
            "de",
            "multi",
            "form-direct-cancel",
            Array.Empty<string>(),
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
    public async Task ExecuteAsync_WhenConfirmationPolicyAmbiguous_ConfirmsOnPartialNameMatch()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Ambiguous },
            CreateMultiFormApplication("multi", ["de"], ("address", "Adressdaten"), ("summary", "Zusammenfassung")));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);
        var matcher = new FormSelectionMatcher();

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript("adress", confidencePercent: 95);
        speechClient.EnqueueTranscript("ja", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            tts,
            mdaCatalog,
            intentParser,
            matcher,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync("de", "multi", Array.Empty<string>(), CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("address", result.SelectedFormId);

        Assert.Equal(2, speechClient.RecordCallCount);
        Assert.Equal(2, speechClient.SpeechToTextCallCount);

        var calls = tts.Calls.ToArray();
        Assert.Equal(6, calls.Length);
        Assert.Contains(calls, call =>
            call.Text.Contains("Adressdaten", StringComparison.Ordinal)
            && call.Text.Contains("richtig", StringComparison.Ordinal));
        Assert.Contains("Adressdaten", calls[^1].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LowConfidenceSelection_AsksHedge_InsteadOfSilentLaunch()
    {
        // Niedrige Konfidenz verlangt auch bei sonst automatischem Start eine Bestätigung mit dem verstandenen Wert.
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateMultiFormApplication("multi", ["de"], ("a", "Alpha"), ("b", "Beta")));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);
        var matcher = new FormSelectionMatcher();

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript("eins", confidencePercent: 40);
        speechClient.EnqueueTranscript("ja", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            tts,
            mdaCatalog,
            intentParser,
            matcher,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync("de", "multi", Array.Empty<string>(), CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("a", result.SelectedFormId);
        Assert.Equal(2, speechClient.RecordCallCount);
        Assert.Contains(tts.Calls, call => call.Text.Contains("Ich habe Alpha verstanden", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MidConfidenceSelection_AsksConfirmation_InsteadOfSilentLaunch()
    {
        // Konfidenz im Bestätigungsbereich führt zur neutralen Rückfrage.
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateMultiFormApplication("multi", ["de"], ("a", "Alpha"), ("b", "Beta")));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);
        var matcher = new FormSelectionMatcher();

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript("eins", confidencePercent: 70);
        speechClient.EnqueueTranscript("ja", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            tts,
            mdaCatalog,
            intentParser,
            matcher,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync("de", "multi", Array.Empty<string>(), CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("a", result.SelectedFormId);
        Assert.Equal(2, speechClient.RecordCallCount);
        Assert.Contains(tts.Calls, call => call.Text.Contains("Alpha. Ist das richtig?", StringComparison.Ordinal));
        Assert.DoesNotContain(tts.Calls, call => call.Text.Contains("verstanden", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AtAcceptConfidenceSelection_LaunchesWithoutConfirmation()
    {
        // Ab der Annahmeschwelle ist keine zusätzliche Bestätigung erforderlich.
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateMultiFormApplication("multi", ["de"], ("a", "Alpha"), ("b", "Beta")));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);
        var matcher = new FormSelectionMatcher();

        await EnqueueUtterancesAsync(speechClient, 1);
        speechClient.EnqueueTranscript("eins", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            tts,
            mdaCatalog,
            intentParser,
            matcher,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync("de", "multi", Array.Empty<string>(), CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("a", result.SelectedFormId);
        Assert.Equal(1, speechClient.RecordCallCount);
        var calls = tts.Calls.ToArray();
        Assert.Equal(5, calls.Length);
        Assert.Contains("Alpha", calls[^1].Text, StringComparison.Ordinal);
        Assert.DoesNotContain(calls, call => call.Text.Contains("Ist das richtig", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AbsentConfidenceSelection_AsksConfirmationBeforeLaunch()
    {
        // Fehlende Konfidenz bleibt unbekannt und verlangt eine Bestätigung;
        // erst die anschließend ausreichend sichere Zustimmung erlaubt den Start.
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            CreateMultiFormApplication("multi", ["de"], ("a", "Alpha"), ("b", "Beta")));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);
        var matcher = new FormSelectionMatcher();

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript("eins", confidencePercent: null);
        speechClient.EnqueueTranscript("ja", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            tts,
            mdaCatalog,
            intentParser,
            matcher,
            new UserLanguageState());

        var result = await dialog.ExecuteAsync("de", "multi", Array.Empty<string>(), CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("a", result.SelectedFormId);
        Assert.Equal(2, speechClient.RecordCallCount);
        Assert.Contains(tts.Calls, call => call.Text.Contains("Ist das richtig", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPolitenessInformal_UsesInformalAnnouncementPrompt()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Informal);

        var catalog = CreateCatalog(
            new VoiceDialogSettings
            {
                ConfirmationPolicy = ConfirmationPolicy.Never
            },
            CreateMultiFormApplication("multi", ["de"], ("a", "Alpha"), ("b", "Beta")));

        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);
        var matcher = new FormSelectionMatcher();

        await EnqueueUtterancesAsync(speechClient, 1);
        speechClient.EnqueueTranscript("eins", confidencePercent: 95);

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            tts,
            mdaCatalog,
            intentParser,
            matcher,
            userLanguageState);

        var result = await dialog.ExecuteAsync("de", "multi", Array.Empty<string>(), CancellationToken.None);

        Assert.False(result.WasCancelled);
        Assert.Equal("a", result.SelectedFormId);

        var calls = tts.Calls.ToArray();
        Assert.NotEmpty(calls);
        Assert.Contains("Sag 'alle Formulare'", calls[0].Text);
    }

    [Fact]
    public async Task ExecuteAsync_WhenIsoCodeIsInvalid_ThrowsArgumentException()
    {
        var dialog = CreateDialog(
            new NoOpClientMessageBus(),
            new QueuedSpeechInputClient(),
            new QueuedSpeechInputClient(),
            new SpeechRecognitionWorker(new QueuedSpeechInputClient()),
            new RecordingTextToSpeechPlayer(),
            new FixedFormsCatalogDataClient(CreateCatalog(
                new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
                CreateMultiFormApplication("multi", ["de"], ("a", "Alpha")))),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new FormSelectionMatcher(),
            new UserLanguageState());

        await Assert.ThrowsAsync<ArgumentException>(() => dialog.ExecuteAsync("german", "multi", Array.Empty<string>(), CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WhenApplicationIsMissing_ThrowsInvalidOperationException()
    {
        var dialog = CreateDialog(
            new NoOpClientMessageBus(),
            new QueuedSpeechInputClient(),
            new QueuedSpeechInputClient(),
            new SpeechRecognitionWorker(new QueuedSpeechInputClient()),
            new RecordingTextToSpeechPlayer(),
            new FixedFormsCatalogDataClient(CreateCatalog(
                new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
                CreateMultiFormApplication("multi", ["de"], ("a", "Alpha")))),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new FormSelectionMatcher(),
            new UserLanguageState());

        await Assert.ThrowsAsync<InvalidOperationException>(() => dialog.ExecuteAsync("de", "missing", Array.Empty<string>(), CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WhenApplicationHasNoForms_ThrowsInvalidOperationException()
    {
        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Never },
            new ApplicationDescriptor
            {
                ApplicationId = "empty",
                DisplayName = new LocalizedText { ["de"] = "Leer" },
                Description = new LocalizedText { ["de"] = "Leer" },
                SupportedLanguages = ["de"],
                DefaultLanguage = "de",
                Forms = []
            });

        var dialog = CreateDialog(
            new NoOpClientMessageBus(),
            new QueuedSpeechInputClient(),
            new QueuedSpeechInputClient(),
            new SpeechRecognitionWorker(new QueuedSpeechInputClient()),
            new RecordingTextToSpeechPlayer(),
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new FormSelectionMatcher(),
            new UserLanguageState());

        await Assert.ThrowsAsync<InvalidOperationException>(() => dialog.ExecuteAsync("de", "empty", Array.Empty<string>(), CancellationToken.None));
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

    private static FormSelectionDialog CreateDialog(
        ISpeechCaptureRecordingIndicator recordingIndicator,
        ISpeechCaptureClient speechCaptureClient,
        IModeAwareSpeechCaptureClient modeAwareSpeechCaptureClient,
        SpeechRecognitionWorker speechRecognitionWorker,
        ITextToSpeechPlayer textToSpeechPlayer,
        IFormsCatalogDataClient mdaCatalogClient,
        IFormsCatalogIntentParser intentParser,
        IFormSelectionMatcher matcher,
        IUserLanguageState userLanguageState,
        ISpeechAssistantTrace? speechAssistantTrace = null)
    {
        return new FormSelectionDialog(
            new SelectionDialogSpeechTurnRunner(
                recordingIndicator,
                speechCaptureClient,
                modeAwareSpeechCaptureClient,
                speechRecognitionWorker,
                textToSpeechPlayer,
                NullLogger<SelectionDialogSpeechTurnRunner>.Instance),
            mdaCatalogClient,
            intentParser,
            matcher,
            userLanguageState,
            speechAssistantTrace ?? NullSpeechAssistantTrace.Instance);
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

    private static ApplicationDescriptor CreateMultiFormApplication(
        string applicationId,
        string[] supportedLanguages,
        params (string Id, string DisplayName)[] forms)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = applicationId,
            DisplayName = new LocalizedText { ["de"] = applicationId },
            Description = new LocalizedText { ["de"] = applicationId },
            SupportedLanguages = supportedLanguages.ToList(),
            DefaultLanguage = supportedLanguages[0],
            Forms = forms
                .Select(form => new FormDescriptor
                {
                    FormId = FormId.From(form.Id),
                    DisplayName = new LocalizedText { ["de"] = form.DisplayName },
                    Description = new LocalizedText { ["de"] = form.DisplayName },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                })
                .ToList()
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
                    CatalogDialogPromptIds.FormAnnouncing,
                    ("de@formal", "Diese Anwendung hat {{count}} Formulare. Sagen Sie 'alle Formulare' für eine Liste."),
                    ("de@informal", "Diese Anwendung hat {{count}} Formulare. Sag 'alle Formulare' für eine Liste."),
                    ("en", "This application has {{count}} forms. Say 'all forms' for a list.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormSingle,
                    ("de", "Starte Formular {{name}}."),
                    ("en", "Starting form {{name}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormListIntro,
                    ("de", "Verfügbare Formulare: {{count}}."),
                    ("en", "Available forms: {{count}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormListItem,
                    ("de", "Nummer {{number}}: {{name}}."),
                    ("en", "Number {{number}}: {{name}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormListSelectPrompt,
                    ("de", "Sagen Sie die Nummer oder den Namen."),
                    ("de@informal", "Sag die Nummer oder den Namen."),
                    ("en", "Say the number or name.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormConfirmation,
                    ("de", "{{name}}. Ist das richtig?"),
                    ("en", "{{name}}. Is that correct?")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormLowConfidenceConfirm,
                    ("de", "Ich habe {{name}} verstanden. Ist das richtig?"),
                    ("en", "I understood {{name}}. Is that correct?")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormConfirmationRepeat,
                    ("de", "Bitte sagen Sie ja oder nein."),
                    ("de@informal", "Bitte sag ja oder nein."),
                    ("en", "Please say yes or no.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormLaunch,
                    ("de", "Starte {{name}}."),
                    ("en", "Starting {{name}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormPrerequisitesNotMet,
                    ("de", "Das Formular {{name}} benötigt zuerst {{missingPrerequisites}}."),
                    ("en", "The form {{name}} requires {{missingPrerequisites}} first.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormHelp,
                    ("de@formal", "Sagen Sie alle Formulare, einen Namen, eine Nummer, wiederholen oder abbrechen."),
                    ("de@informal", "Sag alle Formulare, einen Namen, eine Nummer, wiederholen oder abbrechen."),
                    ("en", "Say all forms, a name, a number, repeat or cancel.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormCancelled,
                    ("de", "Formularauswahl abgebrochen."),
                    ("en", "Form selection cancelled.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormUnrecognized,
                    ("de", "Das habe ich nicht verstanden. Bitte wiederholen Sie oder sagen Sie Hilfe."),
                    ("de@informal", "Das habe ich nicht verstanden. Bitte wiederhole oder sag Hilfe."),
                    ("en", "I did not understand that. Please repeat or say help.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormNoInput,
                    ("de@formal", "Ich habe nichts gehört. Bitte sprechen Sie noch einmal."),
                    ("de@informal", "Ich habe nichts gehört. Bitte sprich noch einmal."),
                    ("en", "I didn't hear anything. Please speak again.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormReListen,
                    ("de", "Verstanden. Ich höre weiter zu."),
                    ("en", "Got it. I am still listening.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormCaptureFailedTerminal,
                    ("de", "Aufnahmefehler. Die Formularauswahl wird beendet."),
                    ("en", "Capture failed. Ending form selection.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormNoForms,
                    ("de", "Diese Anwendung hat keine Formulare."),
                    ("en", "This application has no forms.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormNoMatch,
                    ("de", "{{name}} wurde nicht gefunden."),
                    ("en", "{{name}} was not found.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormInvalidNumber,
                    ("de", "Bitte wählen Sie eine Zahl zwischen 1 und {{max}}."),
                    ("de@informal", "Bitte wähle eine Zahl zwischen 1 und {{max}}."),
                    ("en", "Please choose a number between 1 and {{max}}."))
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
