using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Bestätigung und begrenzte Wiederholungen der Formularauswahl bei unzureichender oder fehlender Erkennungskonfidenz.
/// Die Tests erfassen Ausgabeaufrufe, Aufnahmemodi und Ergebniszustände mit Testdiensten; echte Audiowiedergabe wird nicht geprüft.
/// </summary>
[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantFormSelectionConfidenceRecoveryEndToEndTests
{
    private const string ApplicationId = "test-app";
    private const string SelectedFormId = "alpha-simple";
    private const string SelectedFormName = "Einfaches Formular";
    private const string OtherFormName = "Komplexes Formular";
    private const string InitialConfirmation = "Einfaches Formular. Ist das richtig?";
    private const string ConfirmationRepeat = "Bitte sagen Sie ja oder nein.";
    private const string ExplicitAuthorization = "Ich habe Ihr „Ja“ verstanden, aber nicht sicher genug, um Einfaches Formular zu öffnen. Sagen Sie bitte noch einmal „ja“. Alternativ sagen Sie „Öffnen Sie Einfaches Formular“ oder „Nummer 1“. Sie können auch „nein“ oder „abbrechen“ sagen.";
    private const string VerificationUnavailable = "Ich habe Ihr „Ja“ verstanden, konnte die Erkennung aber technisch nicht sicher prüfen. Sagen Sie bitte noch einmal „ja“. Alternativ sagen Sie „Öffnen Sie Einfaches Formular“ oder „Nummer 1“. Sie können auch „nein“ oder „abbrechen“ sagen.";
    private const string NotOpenedReselect = "Das Formular Einfaches Formular wurde nicht geöffnet. Wählen Sie erneut per Name oder Nummer, oder sagen Sie „abbrechen“.";
    private const string VerificationFailedTerminal = "Bestätigungen können derzeit nicht zuverlässig geprüft werden. Das Formular Einfaches Formular wurde nicht geöffnet. Die Formularauswahl ist beendet.";
    private const string RepairExhaustedTerminal = "Das Formular Einfaches Formular konnte nicht eindeutig bestätigt werden und wurde nicht geöffnet. Die Formularauswahl ist beendet.";
    private const string Launch = "Starte Einfaches Formular.";

    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(25);

    [Theory]
    [InlineData(65)]
    [InlineData(68)]
    [InlineData(82)]
    public async Task FormDialog_BoundYesAtConfirmThresholdOrAbove_LaunchesWithoutSecondAuthorization(
        int confirmationConfidence)
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateFormSelectionDialog(fixture);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 2);
        speechInputClient.EnqueueTranscript("einfaches formular", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: confirmationConfidence);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync(
            "de",
            ApplicationId,
            "form-candidate-authorization",
            [],
            cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal(SelectedFormId, result.SelectedFormId);
        Assert.Equal(SelectedFormName, result.SelectedFormName);
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
        Assert.DoesNotContain(
            spoken[..launchIndex],
            text => text.StartsWith("Starte ", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("öffnen sie komplexes formular ja")]
    [InlineData("ja öffnen sie komplexes formular")]
    [InlineData("komplexes formular ja")]
    [InlineData("ja komplexes formular")]
    [InlineData("komplexes ja")]
    [InlineData("ja komplexes")]
    public async Task FormDialog_ExplicitAuthorizationWithConflictingTargetAndYes_NeverLaunchesEitherCandidate(
        string conflictingUtterance)
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateFormSelectionDialog(fixture);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 5);
        speechInputClient.EnqueueTranscript("einfaches formular", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);
        speechInputClient.EnqueueTranscript(conflictingUtterance, confidencePercent: 100);
        speechInputClient.EnqueueTranscript("einfaches formular", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 68);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync(
            "de",
            ApplicationId,
            "form-conflicted-candidate-authorization",
            [],
            cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal(SelectedFormId, result.SelectedFormId);
        Assert.Equal(SelectedFormName, result.SelectedFormName);

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
        Assert.DoesNotContain(spoken, text => text == $"Starte {OtherFormName}.");
    }

    [Fact]
    public async Task FormDialog_CapturedButEmptyConfirmation_UsesShortReminderThenAcceptsBoundYes()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateFormSelectionDialog(fixture);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 5);
        speechInputClient.EnqueueTranscript("einfaches formular", confidencePercent: 88);
        speechInputClient.EnqueueTranscript(string.Empty);
        speechInputClient.EnqueueTranscript(string.Empty);
        speechInputClient.EnqueueTranscript(string.Empty);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 68);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync(
            "de",
            ApplicationId,
            "form-captured-empty-confirmation-recovery",
            [],
            cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal(SelectedFormId, result.SelectedFormId);
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
    public async Task FormDialog_ExactCandidateAuthorization_RejectsBelowAndAcceptsAtConfirmThreshold()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateFormSelectionDialog(fixture);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 6);
        speechInputClient.EnqueueTranscript("einfaches formular", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);
        speechInputClient.EnqueueTranscript("öffnen sie einfaches formular", confidencePercent: 64);
        speechInputClient.EnqueueTranscript("einfaches formular", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);
        speechInputClient.EnqueueTranscript("öffnen sie einfaches formular", confidencePercent: 65);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync(
            "de",
            ApplicationId,
            "form-candidate-authorization-confidence-floor",
            [],
            cts.Token);

        Assert.False(result.WasCancelled);
        Assert.Equal(SelectedFormId, result.SelectedFormId);
        Assert.Equal(SelectedFormName, result.SelectedFormName);
        Assert.Equal(6, speechInputClient.RecordCallCount);
        Assert.Equal(
            [
                SpeechCaptureMode.Listening,
                SpeechCaptureMode.ShortConfirmation,
                SpeechCaptureMode.ShortConfirmation,
                SpeechCaptureMode.Listening,
                SpeechCaptureMode.ShortConfirmation,
                SpeechCaptureMode.ShortConfirmation
            ],
            speechInputClient.RecordModes);

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
    public async Task FormDialog_MissingConfirmationConfidenceTwice_EndsWithFormTerminalWithoutLaunch()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateFormSelectionDialog(fixture);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 3);
        speechInputClient.EnqueueTranscript("einfaches formular", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: null);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: null);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync(
            "de",
            ApplicationId,
            "form-missing-confirmation-confidence",
            [],
            cts.Token);

        Assert.True(result.WasCancelled);
        Assert.Equal(DialogCancellationReason.ConfirmationVerificationFailed, result.CancellationReason);
        Assert.Null(result.SelectedFormId);
        Assert.Null(result.SelectedFormName);
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
        Assert.DoesNotContain(spoken, text => text.StartsWith("Starte ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FormDialog_BareLowConfidenceYesAcrossFreshCandidates_ReselectsThenEndsWithNeutralRepairTerminal()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var (dialog, speechInputClient, textToSpeechPlayer) = CreateFormSelectionDialog(fixture);

        await fixture.EnqueueUtterancesAsync(speechInputClient, 6);
        speechInputClient.EnqueueTranscript("einfaches formular", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);
        speechInputClient.EnqueueTranscript("einfaches formular", confidencePercent: 88);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);
        speechInputClient.EnqueueTranscript("ja", confidencePercent: 64);

        using var cts = new CancellationTokenSource(EndToEndTimeout);
        var result = await dialog.ExecuteAsync(
            "de",
            ApplicationId,
            "form-repair-budget",
            [],
            cts.Token);

        Assert.True(result.WasCancelled);
        Assert.Equal(DialogCancellationReason.ConfirmationRepairExhausted, result.CancellationReason);
        Assert.Null(result.SelectedFormId);
        Assert.Null(result.SelectedFormName);
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

    private static (FormSelectionDialog Dialog, QueuedSpeechInputClient SpeechInputClient, RecordingTextToSpeechPlayer TextToSpeechPlayer)
        CreateFormSelectionDialog(SpeechAssistantEndToEndFixture fixture)
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        var catalog = fixture.CreateMultiFormCatalog(
            ApplicationId,
            "de",
            new VoiceDialogSettings
            {
                ConfirmationPolicy = ConfirmationPolicy.Always,
                MaxRetries = 3,
                PauseBetweenItemsMs = 0,
                SttMinConfidenceToAccept = 85,
                SttMinConfidenceToConfirm = 65
            },
            (SelectedFormId, SelectedFormName),
            ("beta-complex", "Komplexes Formular"));

        var dialog = FormSelectionDialogTestFactory.Create(
            interactionSignals,
            speechInputClient,
            textToSpeechPlayer,
            new FixedFormsCatalogDataClient(catalog),
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            new FormSelectionMatcher(),
            userLanguageState);

        return (dialog, speechInputClient, textToSpeechPlayer);
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
