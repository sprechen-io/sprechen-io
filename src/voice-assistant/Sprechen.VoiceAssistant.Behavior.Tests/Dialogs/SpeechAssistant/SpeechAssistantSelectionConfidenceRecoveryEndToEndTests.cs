using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Anwendungsauswahl bei unzureichender oder fehlender Erkennungskonfidenz.
/// Die Fälle prüfen kandidatenbezogene Bestätigungen, begrenzte Wiederholungen und Start- oder Abbruchergebnisse anhand von Testdiensten und Ausgabeaufrufen.
/// </summary>
[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantSelectionConfidenceRecoveryEndToEndTests
{
    private const string SelectedApplicationName = "Einfaches Beispiel";
    private const string OtherApplicationName = "Komplexes Beispiel";
    private const string InitialConfirmation = "Einfaches Beispiel. Ist das richtig?";
    private const string ConfirmationRepeat = "Bitte sagen Sie ja oder nein.";
    private const string LowConfidenceConfirmation = "Ich habe Einfaches Beispiel verstanden. Ist das richtig?";
    private const string ExplicitAuthorization = "Ich habe Ihr „Ja“ verstanden, aber nicht sicher genug, um Einfaches Beispiel zu öffnen. Sagen Sie bitte noch einmal „ja“. Alternativ sagen Sie „Öffnen Sie Einfaches Beispiel“ oder „Nummer 1“. Sie können auch „nein“ oder „abbrechen“ sagen.";
    private const string VerificationUnavailable = "Ich habe Ihr „Ja“ verstanden, konnte die Erkennung aber technisch nicht sicher prüfen. Sagen Sie bitte noch einmal „ja“. Alternativ sagen Sie „Öffnen Sie Einfaches Beispiel“ oder „Nummer 1“. Sie können auch „nein“ oder „abbrechen“ sagen.";
    private const string NotOpenedReselect = "Einfaches Beispiel wurde nicht geöffnet. Wählen Sie erneut per Name oder Nummer.";
    private const string VerificationFailedTerminal = "Bestätigungen können derzeit nicht zuverlässig geprüft werden. Einfaches Beispiel wurde nicht geöffnet. Die Anwendungsauswahl ist beendet.";
    private const string RepairExhaustedTerminal = "Einfaches Beispiel konnte nicht eindeutig bestätigt werden und wurde nicht geöffnet. Die Anwendungsauswahl ist beendet.";
    private const string Launch = "Starte Einfaches Beispiel.";

    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(25);

    [Fact]
    public async Task CatalogDialog_MissingConfirmationConfidenceTwice_SpeaksBoundedVerificationFailureWithoutLaunching()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateCatalogDialog(ConfirmationPolicy.Always);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 3);
        speechInputClient.EnqueueTranscript("eins", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: null);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: null);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync("de", "missing-confirmation-confidence", cts.Token);

        Assert.True(result.WasCancelled);
        Assert.Equal(DialogCancellationReason.ConfirmationVerificationFailed, result.CancellationReason);
        Assert.Null(result.SelectedApplicationId);
        Assert.Equal(3, speechInputClient.RecordCallCount);
        Assert.Equal(
            [
                SpeechCaptureMode.Listening,
                SpeechCaptureMode.ShortConfirmation,
                SpeechCaptureMode.ShortConfirmation
            ],
            speechInputClient.RecordModes);

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var confirmationIndex = FindIndex(spoken, InitialConfirmation);
        var verificationIndex = FindIndex(spoken, VerificationUnavailable, confirmationIndex + 1);
        var terminalIndex = FindIndex(spoken, VerificationFailedTerminal, verificationIndex + 1);

        Assert.True(confirmationIndex >= 0, DescribeCalls(spoken));
        Assert.True(verificationIndex > confirmationIndex, DescribeCalls(spoken));
        Assert.True(terminalIndex > verificationIndex, DescribeCalls(spoken));
        Assert.Equal(1, spoken.Count(text => text == VerificationUnavailable));
        Assert.Equal(1, spoken.Count(text => text == VerificationFailedTerminal));
        Assert.DoesNotContain(spoken, text => text.StartsWith("Starte ", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(65)]
    [InlineData(68)]
    [InlineData(82)]
    public async Task CatalogDialog_BoundYesAtConfirmThresholdOrAbove_LaunchesWithoutSecondAuthorization(
        int confirmationConfidence)
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateCatalogDialog(ConfirmationPolicy.Always);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 2);
        speechInputClient.EnqueueTranscript("einfaches beispiel", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: confirmationConfidence);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync("de", "collaborative-yes-recovery", cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha-target", result.SelectedApplicationId);
        Assert.Equal(SelectedApplicationName, result.SelectedApplicationName);
        Assert.Equal(2, speechInputClient.RecordCallCount);
        Assert.Equal(
            [
                SpeechCaptureMode.Listening,
                SpeechCaptureMode.ShortConfirmation
            ],
            speechInputClient.RecordModes);

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var confirmationIndex = FindIndex(spoken, InitialConfirmation);
        var launchIndex = FindIndex(spoken, Launch, confirmationIndex + 1);

        Assert.True(confirmationIndex >= 0, DescribeCalls(spoken));
        Assert.True(launchIndex > confirmationIndex, DescribeCalls(spoken));
        Assert.DoesNotContain(spoken, text => text == ExplicitAuthorization);
        Assert.DoesNotContain(spoken, text => text == VerificationUnavailable);
        Assert.DoesNotContain(spoken[..launchIndex], text => text.StartsWith("Starte ", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("öffnen sie komplexes beispiel ja")]
    [InlineData("ja öffnen sie komplexes beispiel")]
    [InlineData("komplexes beispiel ja")]
    [InlineData("ja komplexes beispiel")]
    [InlineData("komplexes ja")]
    [InlineData("ja komplexes")]
    public async Task CatalogDialog_ExplicitAuthorizationWithConflictingTargetAndYes_NeverLaunchesEitherCandidate(
        string conflictingUtterance)
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateCatalogDialog(ConfirmationPolicy.Always);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 5);
        speechInputClient.EnqueueTranscript("einfaches beispiel", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);
        speechInputClient.EnqueueTranscript(conflictingUtterance, confidencePercent: 100);
        speechInputClient.EnqueueTranscript("einfaches beispiel", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 68);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync("de", "conflicted-candidate-authorization", cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha-target", result.SelectedApplicationId);
        Assert.Equal(SelectedApplicationName, result.SelectedApplicationName);

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var firstConfirmationIndex = FindIndex(spoken, InitialConfirmation);
        var authorizationIndex = FindIndex(spoken, ExplicitAuthorization, firstConfirmationIndex + 1);
        var reselectIndex = FindIndex(spoken, NotOpenedReselect, authorizationIndex + 1);
        var secondConfirmationIndex = FindIndex(spoken, InitialConfirmation, reselectIndex + 1);
        var launchIndex = FindIndex(spoken, Launch, secondConfirmationIndex + 1);

        Assert.True(firstConfirmationIndex >= 0, DescribeCalls(spoken));
        Assert.True(authorizationIndex > firstConfirmationIndex, DescribeCalls(spoken));
        Assert.True(reselectIndex > authorizationIndex, DescribeCalls(spoken));
        Assert.True(secondConfirmationIndex > reselectIndex, DescribeCalls(spoken));
        Assert.True(launchIndex > secondConfirmationIndex, DescribeCalls(spoken));
        Assert.DoesNotContain(spoken[..secondConfirmationIndex], text => text.StartsWith("Starte ", StringComparison.Ordinal));
        Assert.DoesNotContain(spoken, text => text == $"Starte {OtherApplicationName}.");
    }

    [Fact]
    public async Task CatalogDialog_CapturedButEmptyConfirmation_UsesShortReminderThenAcceptsBoundYes()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateCatalogDialog(ConfirmationPolicy.Always);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 5);
        speechInputClient.EnqueueTranscript("einfaches beispiel", confidencePercent: 88);
        speechInputClient.EnqueueTranscript(string.Empty);
        speechInputClient.EnqueueTranscript(string.Empty);
        speechInputClient.EnqueueTranscript(string.Empty);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 68);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync("de", "captured-empty-confirmation-recovery", cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha-target", result.SelectedApplicationId);
        Assert.Equal(5, speechInputClient.RecordCallCount);
        Assert.Equal(
            [
                SpeechCaptureMode.Listening,
                SpeechCaptureMode.ShortConfirmation,
                SpeechCaptureMode.ShortConfirmation,
                SpeechCaptureMode.ShortConfirmation,
                SpeechCaptureMode.ShortConfirmation
            ],
            speechInputClient.RecordModes);

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var confirmationIndex = FindIndex(spoken, InitialConfirmation);
        var reminderIndex = FindIndex(spoken, ConfirmationRepeat, confirmationIndex + 1);
        var launchIndex = FindIndex(spoken, Launch, reminderIndex + 1);

        Assert.True(confirmationIndex >= 0, DescribeCalls(spoken));
        Assert.True(reminderIndex > confirmationIndex, DescribeCalls(spoken));
        Assert.True(launchIndex > reminderIndex, DescribeCalls(spoken));
        Assert.Equal(1, spoken.Count(text => text == ConfirmationRepeat));
        Assert.DoesNotContain(spoken, text => text == ExplicitAuthorization);
    }

    [Fact]
    public async Task CatalogDialog_ExactCandidateAuthorization_RejectsBelowAndAcceptsAtConfirmThreshold()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateCatalogDialog(ConfirmationPolicy.Always);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 6);
        speechInputClient.EnqueueTranscript("einfaches beispiel", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);
        speechInputClient.EnqueueTranscript("öffnen sie einfaches beispiel", confidencePercent: 64);
        speechInputClient.EnqueueTranscript("einfaches beispiel", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);
        speechInputClient.EnqueueTranscript("öffnen sie einfaches beispiel", confidencePercent: 65);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync("de", "candidate-authorization-confidence-floor", cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha-target", result.SelectedApplicationId);
        Assert.Equal(SelectedApplicationName, result.SelectedApplicationName);
        Assert.Equal(6, speechInputClient.RecordCallCount);

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var firstConfirmationIndex = FindIndex(spoken, InitialConfirmation);
        var firstAuthorizationIndex = FindIndex(spoken, ExplicitAuthorization, firstConfirmationIndex + 1);
        var reselectIndex = FindIndex(spoken, NotOpenedReselect, firstAuthorizationIndex + 1);
        var secondConfirmationIndex = FindIndex(spoken, InitialConfirmation, reselectIndex + 1);
        var secondAuthorizationIndex = FindIndex(spoken, ExplicitAuthorization, secondConfirmationIndex + 1);
        var launchIndex = FindIndex(spoken, Launch, secondAuthorizationIndex + 1);

        Assert.True(firstConfirmationIndex >= 0, DescribeCalls(spoken));
        Assert.True(firstAuthorizationIndex > firstConfirmationIndex, DescribeCalls(spoken));
        Assert.True(reselectIndex > firstAuthorizationIndex, DescribeCalls(spoken));
        Assert.True(secondConfirmationIndex > reselectIndex, DescribeCalls(spoken));
        Assert.True(secondAuthorizationIndex > secondConfirmationIndex, DescribeCalls(spoken));
        Assert.True(launchIndex > secondAuthorizationIndex, DescribeCalls(spoken));
        Assert.DoesNotContain(
            spoken[..launchIndex],
            text => text.StartsWith("Starte ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CatalogDialog_YesWithoutConfidence_AcknowledgesSystemLimitAndAcceptsStableNumber()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateCatalogDialog(ConfirmationPolicy.Always);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 3);
        speechInputClient.EnqueueTranscript("eins", confidencePercent: 95);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: null);
        speechInputClient.EnqueueTranscript("eins", confidencePercent: 65);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync("de", "missing-confidence-stable-number-recovery", cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha-target", result.SelectedApplicationId);
        Assert.Equal(SelectedApplicationName, result.SelectedApplicationName);
        Assert.Equal(3, speechInputClient.RecordCallCount);

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var confirmationIndex = FindIndex(spoken, InitialConfirmation);
        var verificationIndex = FindIndex(spoken, VerificationUnavailable, confirmationIndex + 1);
        var launchIndex = FindIndex(spoken, Launch, verificationIndex + 1);

        Assert.True(confirmationIndex >= 0, DescribeCalls(spoken));
        Assert.True(verificationIndex > confirmationIndex, DescribeCalls(spoken));
        Assert.True(launchIndex > verificationIndex, DescribeCalls(spoken));
        Assert.DoesNotContain(spoken[..launchIndex], text => text.StartsWith("Starte ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CatalogDialog_LowConfidenceSelection_WrongNumberCannotAuthorizeCandidateButExactNumberCan()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateCatalogDialog(ConfirmationPolicy.Never);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 6);
        speechInputClient.EnqueueTranscript("eins", confidencePercent: 40);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 60);
        speechInputClient.EnqueueTranscript("zwei", confidencePercent: 65);
        speechInputClient.EnqueueTranscript("eins", confidencePercent: 40);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 60);
        speechInputClient.EnqueueTranscript("eins", confidencePercent: 65);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync("de", "candidate-bound-authorization", cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal("alpha-target", result.SelectedApplicationId);
        Assert.Equal(SelectedApplicationName, result.SelectedApplicationName);
        Assert.Equal(6, speechInputClient.RecordCallCount);

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var firstLowConfidenceIndex = FindIndex(spoken, LowConfidenceConfirmation);
        var firstAuthorizationIndex = FindIndex(spoken, ExplicitAuthorization, firstLowConfidenceIndex + 1);
        var reselectIndex = FindIndex(spoken, NotOpenedReselect, firstAuthorizationIndex + 1);
        var secondLowConfidenceIndex = FindIndex(spoken, LowConfidenceConfirmation, reselectIndex + 1);
        var secondAuthorizationIndex = FindIndex(spoken, ExplicitAuthorization, secondLowConfidenceIndex + 1);
        var launchIndex = FindIndex(spoken, Launch, secondAuthorizationIndex + 1);

        Assert.True(firstLowConfidenceIndex >= 0, DescribeCalls(spoken));
        Assert.True(firstAuthorizationIndex > firstLowConfidenceIndex, DescribeCalls(spoken));
        Assert.True(reselectIndex > firstAuthorizationIndex, DescribeCalls(spoken));
        Assert.True(secondLowConfidenceIndex > reselectIndex, DescribeCalls(spoken));
        Assert.True(secondAuthorizationIndex > secondLowConfidenceIndex, DescribeCalls(spoken));
        Assert.True(launchIndex > secondAuthorizationIndex, DescribeCalls(spoken));
        Assert.DoesNotContain(
            spoken[..secondAuthorizationIndex],
            text => text.StartsWith("Starte ", StringComparison.Ordinal));
        Assert.DoesNotContain(spoken, text => text == $"Starte {OtherApplicationName}.");
    }

    [Fact]
    public async Task CatalogDialog_BareLowConfidenceYesAcrossFreshCandidates_ReselectsThenEndsWithNeutralRepairTerminal()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateCatalogDialog(ConfirmationPolicy.Always);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 6);
        speechInputClient.EnqueueTranscript("einfaches beispiel", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);
        speechInputClient.EnqueueTranscript("einfaches beispiel", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync("de", "application-repair-budget", cts.Token);

        Assert.True(result.WasCancelled);
        Assert.Equal(DialogCancellationReason.ConfirmationRepairExhausted, result.CancellationReason);
        Assert.Null(result.SelectedApplicationId);
        Assert.Null(result.SelectedApplicationName);
        Assert.Equal(6, speechInputClient.RecordCallCount);

        var spoken = textToSpeechPlayer.Calls.Select(call => call.Text).ToArray();
        var firstConfirmationIndex = FindIndex(spoken, InitialConfirmation);
        var firstAuthorizationIndex = FindIndex(spoken, ExplicitAuthorization, firstConfirmationIndex + 1);
        var reselectIndex = FindIndex(spoken, NotOpenedReselect, firstAuthorizationIndex + 1);
        var secondConfirmationIndex = FindIndex(spoken, InitialConfirmation, reselectIndex + 1);
        var secondAuthorizationIndex = FindIndex(spoken, ExplicitAuthorization, secondConfirmationIndex + 1);
        var terminalIndex = FindIndex(spoken, RepairExhaustedTerminal, secondAuthorizationIndex + 1);

        Assert.True(firstConfirmationIndex >= 0, DescribeCalls(spoken));
        Assert.True(firstAuthorizationIndex > firstConfirmationIndex, DescribeCalls(spoken));
        Assert.True(reselectIndex > firstAuthorizationIndex, DescribeCalls(spoken));
        Assert.True(secondConfirmationIndex > reselectIndex, DescribeCalls(spoken));
        Assert.True(secondAuthorizationIndex > secondConfirmationIndex, DescribeCalls(spoken));
        Assert.True(terminalIndex > secondAuthorizationIndex, DescribeCalls(spoken));
        Assert.Equal(1, spoken.Count(text => text == NotOpenedReselect));
        Assert.Equal(1, spoken.Count(text => text == RepairExhaustedTerminal));
        Assert.DoesNotContain(spoken, text => text.StartsWith("Starte ", StringComparison.Ordinal));
    }

    private static (FormsCatalogDialog Dialog, QueuedSpeechInputClient SpeechInputClient, RecordingTextToSpeechPlayer TextToSpeechPlayer)
        CreateCatalogDialog(ConfirmationPolicy confirmationPolicy)
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var dialog = FormsCatalogDialogTestFactory.Create(
            interactionSignals,
            speechInputClient,
            textToSpeechPlayer,
            new FixedFormsCatalogDataClient(CreateCatalog(confirmationPolicy)),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            CreateGermanUserLanguageState());

        return (dialog, speechInputClient, textToSpeechPlayer);
    }

    private static FormsCatalog CreateCatalog(ConfirmationPolicy confirmationPolicy)
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            VoiceDialogSettings = new VoiceDialogSettings
            {
                ConfirmationPolicy = confirmationPolicy,
                MaxRetries = 3,
                PauseBetweenItemsMs = 0,
                SttMinConfidenceToAccept = 85,
                SttMinConfidenceToConfirm = 65
            },
            SelectionDialog = CreateSelectionDialog(),
            Applications =
            [
                CreateApplication("alpha-target", SelectedApplicationName),
                CreateApplication("beta-other", OtherApplicationName)
            ]
        };
    }

    private static ApplicationSelectionDialog CreateSelectionDialog()
    {
        return new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "5",
            SelectionDialogId = "selection-confidence-recovery.behavior-test",
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Prompts =
            [
                CreatePrompt(CatalogDialogPromptIds.AppAnnouncingMultiple, "Es gibt {{count}} Anwendungen."),
                CreatePrompt(CatalogDialogPromptIds.AppConfirmation, "{{name}}. Ist das richtig?"),
                CreatePrompt(CatalogDialogPromptIds.AppLowConfidenceConfirm, "Ich habe {{name}} verstanden. Ist das richtig?"),
                CreatePrompt(CatalogDialogPromptIds.AppConfirmationRepeat, "Bitte sagen Sie ja oder nein."),
                CreatePrompt(CatalogDialogPromptIds.AppExplicitAuthorization, "Ich habe Ihr „Ja“ verstanden, aber nicht sicher genug, um {{name}} zu öffnen. Sagen Sie bitte noch einmal „ja“. Alternativ sagen Sie „Öffnen Sie {{name}}“ oder „Nummer {{number}}“. Sie können auch „nein“ oder „abbrechen“ sagen."),
                CreatePrompt(CatalogDialogPromptIds.AppVerificationUnavailable, "Ich habe Ihr „Ja“ verstanden, konnte die Erkennung aber technisch nicht sicher prüfen. Sagen Sie bitte noch einmal „ja“. Alternativ sagen Sie „Öffnen Sie {{name}}“ oder „Nummer {{number}}“. Sie können auch „nein“ oder „abbrechen“ sagen."),
                CreatePrompt(CatalogDialogPromptIds.AppNotOpenedReselect, "{{name}} wurde nicht geöffnet. Wählen Sie erneut per Name oder Nummer."),
                CreatePrompt(CatalogDialogPromptIds.AppVerificationFailedTerminal, "Bestätigungen können derzeit nicht zuverlässig geprüft werden. {{name}} wurde nicht geöffnet. Die Anwendungsauswahl ist beendet."),
                CreatePrompt(CatalogDialogPromptIds.AppRepairExhaustedTerminal, "{{name}} konnte nicht eindeutig bestätigt werden und wurde nicht geöffnet. Die Anwendungsauswahl ist beendet."),
                CreatePrompt(CatalogDialogPromptIds.AppCaptureFailedTerminal, "Die Spracherkennung ist derzeit nicht verfügbar. Es wurde keine Anwendung geöffnet. Die Anwendungsauswahl ist beendet."),
                CreatePrompt(CatalogDialogPromptIds.AppLaunch, "Starte {{name}}."),
                CreatePrompt(CatalogDialogPromptIds.AppNoInput, "Ich habe nichts gehört. Bitte sprechen Sie noch einmal."),
                CreatePrompt(CatalogDialogPromptIds.AppReListen, "Verstanden. Ich höre weiter zu."),
                CreatePrompt(CatalogDialogPromptIds.AppCancelled, "Anwendungsauswahl abgebrochen."),
                CreatePrompt(CatalogDialogPromptIds.AppHelp, "Sagen Sie Name, Nummer, wiederholen oder abbrechen."),
                CreatePrompt(CatalogDialogPromptIds.AppListIntro, "Verfügbare Anwendungen: {{count}}."),
                CreatePrompt(CatalogDialogPromptIds.AppListItem, "Nummer {{number}}: {{name}}."),
                CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt, "Sagen Sie die Nummer oder den Namen."),
                CreatePrompt(CatalogDialogPromptIds.AppUnrecognized, "Das habe ich nicht verstanden. Bitte wiederholen Sie."),
                CreatePrompt(CatalogDialogPromptIds.AppNoMatch, "{{name}} wurde nicht gefunden."),
                CreatePrompt(CatalogDialogPromptIds.AppInvalidNumber, "Bitte wählen Sie eine Zahl zwischen 1 und {{max}}.")
            ],
            Intents = [.. SpeechAssistantCatalogIntentHints.CreateIntents()],
            NumberWords = [.. SpeechAssistantCatalogIntentHints.CreateNumberWords()]
        };
    }

    private static ApplicationDescriptor CreateApplication(string id, string displayName)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = id,
            DisplayName = new LocalizedText { ["de"] = displayName },
            Description = new LocalizedText { ["de"] = displayName },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From($"{id}-form"),
                    DisplayName = new LocalizedText { ["de"] = $"Formular {displayName}" },
                    Description = new LocalizedText { ["de"] = $"Formular {displayName}" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"],
                    Sequence = 1
                }
            ]
        };
    }

    private static UserLanguageState CreateGermanUserLanguageState()
    {
        var state = new UserLanguageState();
        state.SetSelectedIsoCode("de");
        state.SetSelectedPolitenessLevel(PolitenessLevel.Formal);
        return state;
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
