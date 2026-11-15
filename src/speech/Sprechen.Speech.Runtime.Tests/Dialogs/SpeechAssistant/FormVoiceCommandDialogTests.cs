using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Proto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Contracts.FormSurfaces;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.Integration;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.Runtime.Tests.Testing;
using ReferenceFormDefinitionBuilder = Sprechen.Contracts.FormSurfaces.FormSurfaceDefinitionBuilder;
using SpeechSupportedLanguage = Sprechen.Speech.LanguageContracts.SupportedLanguage;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

// Interne Helfer teilen den Dialogaufbau mit FormVoiceCommandSpokenSequenceTests, damit beide Testgruppen dieselben Abhängigkeiten verwenden.
public sealed class FormVoiceCommandDialogTests
{
    // Die lokale Stillstandsgrenze lässt bei gemeinsam ausgeführten Dialogtests Spielraum für Lastschwankungen.
    internal static readonly TimeSpan DialogExecutionTimeout = TimeSpan.FromSeconds(15);
    private const int DiscoveryDemoSetupRecordingCount = 10;

    [Fact]
    public async Task ExecuteAsync_WhenApplicationProvided_PromptsAndConfirmsEachField_AndEmitsEvaluationTelemetry()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var mdaCatalog = new FixedFormsCatalogDataClient(CreateFormsCatalog());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateShortFormContract());
        var evaluationSession = new EvaluationSession("test-session", EvaluationChannel.Voice, "short-form", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.StartSessionAsync("short-form", "short-form", "de", EvaluationChannel.Voice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationSession);
        telemetry
            .Setup(t => t.EndSessionAsync(evaluationSession, EvaluationOutcome.Submitted, It.IsAny<EvaluationTaskCompletion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAcceptance(evaluationSession));
        ConfigureAcceptedTerminalCommit(telemetry, evaluationSession);
        telemetry
            .Setup(t => t.TrackFieldFocus(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStatusSnapshot(evaluationSession, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry
            .Setup(t => t.TrackFieldAndGroupStatus(evaluationSession, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry
            .Setup(t => t.TrackFieldAttempt(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStateEntry(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackGlobalCommand(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackAmbiguityResolved(
                evaluationSession,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CommandSignalClass.NoCommand,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()));

        await EnqueueUtterancesAsync(speechClient, 9);
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("Mustermann");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("max@example.com");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("max@example.com");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry.Object);

        var result = await dialog.ExecuteAsync(
            "de",
            "short-form",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.Equal(9, speechClient.RecordCallCount);
        Assert.Equal(9, speechClient.SpeechToTextCallCount);
        Assert.True(result.WasSubmitted);

        var calls = tts.Calls.ToArray();
        Assert.Equal(15, calls.Length);

        Assert.Equal("Formular Kurzformular. Es gibt 1 relevante Feldgruppen. Nun die Liste, Nummer 1 ist Kontakt.. Eingabefelder abgeschlossen 0 von 4. Für Hilfe sagen Sie Okay Alex Hilfe. Für alle Befehle sagen Sie: Okay Alex welche Befehle.", calls[0].Text);

        Assert.Equal("Feldgruppe Kontakt.", calls[1].Text);

        Assert.Equal("Feld 1 von 4 heißt Vorname. Oder sagen Sie buchstabieren.", calls[2].Text);
        Assert.Equal("Vorname ist Max. Zum Übernehmen sagen Sie übernehmen. Zum Korrigieren sagen Sie ändern oder buchstabieren.", calls[3].Text);
        Assert.Equal("Der Wert wurde übernommen.", calls[4].Text);

        Assert.Equal("Feld 2 von 4 heißt Nachname.", calls[5].Text);
        Assert.Equal("Nachname ist Mustermann. Zum Übernehmen sagen Sie übernehmen. Zum Korrigieren sagen Sie ändern oder buchstabieren.", calls[6].Text);
        Assert.Equal("Der Wert wurde übernommen.", calls[7].Text);

        // Nach zwei ausführlichen Wertbestätigungen folgt an Feld 3 und 4 nur die kurze Erinnerung.
        Assert.Equal("Feld 3 von 4 heißt E-Mail.", calls[8].Text);
        Assert.Equal("E-Mail ist max@example.com. Übernehmen oder ändern?", calls[9].Text);
        Assert.Equal("Der Wert wurde übernommen.", calls[10].Text);

        Assert.Equal("Feld 4 von 4 heißt E-Mail wiederholen.", calls[11].Text);
        Assert.Equal("E-Mail wiederholen ist max@example.com. Übernehmen oder ändern?", calls[12].Text);

        // Nach dem letzten Feld bleibt die Abschlussprüfung offen, bis ausdrücklich abgesendet wird.
        Assert.Equal(FormDialogFlowPromptTextResolver.GetFormClosingReviewPrompt("de", PolitenessLevel.Formal), calls[13].Text);
        Assert.Equal("Das Formular wurde abgesendet.", calls[14].Text);

        telemetry.Verify(t => t.EndSessionAsync(evaluationSession, EvaluationOutcome.Submitted, It.IsAny<EvaluationTaskCompletion>(), It.IsAny<CancellationToken>()), Times.Once);
        telemetry.Verify(t => t.TrackFieldFocus(evaluationSession, It.IsAny<string>()), Times.Exactly(4));
        telemetry.Verify(t => t.TrackFieldAttempt(evaluationSession, It.IsAny<string>(), "voice", true, It.IsAny<int?>()), Times.Exactly(4));
        telemetry.Verify(t => t.TrackConstraintViolation(
            evaluationSession,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
        telemetry.Verify(t => t.TrackGlobalCommand(evaluationSession, "submit"), Times.Once);
        telemetry.Verify(t => t.TrackStateEntry(evaluationSession, "PROMPTING"), Times.Exactly(5));
        telemetry.Verify(t => t.TrackStateEntry(evaluationSession, "LISTENING"), Times.Exactly(5));
        telemetry.Verify(t => t.TrackStateEntry(evaluationSession, "CONFIRMATION"), Times.Exactly(4));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoInputOnce_RepromptsAndEmitsRepairTelemetry()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalogWithSettings(new VoiceDialogSettings { MaxRetries = 0 }, CreateOneFieldApplication());
        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateOneFieldContract());
        var evaluationSession = new EvaluationSession("repair-session", EvaluationChannel.Voice, "one-field", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.StartSessionAsync("one-field", "one-field", "de", EvaluationChannel.Voice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationSession);
        telemetry
            .Setup(t => t.EndSessionAsync(evaluationSession, EvaluationOutcome.Submitted, It.IsAny<EvaluationTaskCompletion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAcceptance(evaluationSession));
        ConfigureAcceptedTerminalCommit(telemetry, evaluationSession);
        telemetry
            .Setup(t => t.TrackFieldFocus(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStatusSnapshot(evaluationSession, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry
            .Setup(t => t.TrackFieldAndGroupStatus(evaluationSession, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry
            .Setup(t => t.TrackFieldAttempt(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStateEntry(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackGlobalCommand(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackRepairAttempt(evaluationSession, It.IsAny<string>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackNoSpeechDetected(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackCommandRecognitionFailure(evaluationSession, It.IsAny<string>(), It.IsAny<string?>()));
        telemetry
            .Setup(t => t.TrackAmbiguityResolved(
                evaluationSession,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CommandSignalClass.NoCommand,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()));

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromException<CapturedSpeechAudio>(new NoSpeechDetectedException()));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry.Object);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal(4, speechClient.RecordCallCount);
        Assert.Equal(3, speechClient.SpeechToTextCallCount);

        var calls = tts.Calls.ToArray();
        Assert.StartsWith("Formular ", calls[0].Text);
        calls = calls.Skip(1).ToArray();
        Assert.Equal(6, calls.Length);
        Assert.Equal("Feldgruppe Main.", calls[0].Text);
        Assert.Equal("Feld 1 von 1 heißt Name. Oder sagen Sie buchstabieren.", calls[1].Text);
        Assert.Equal("Feld 1 von 1 heißt Name.", calls[2].Text);
        Assert.Equal("Name ist Max. Zum Übernehmen sagen Sie übernehmen. Zum Korrigieren sagen Sie ändern oder buchstabieren.", calls[3].Text);
        Assert.Equal(FormDialogFlowPromptTextResolver.GetFormClosingReviewPrompt("de", PolitenessLevel.Formal), calls[4].Text);
        Assert.Equal("Das Formular wurde abgesendet.", calls[5].Text);

        telemetry.Verify(t => t.TrackFieldAttempt(evaluationSession, "name", "voice", false, It.IsAny<int?>()), Times.Once);
        telemetry.Verify(t => t.TrackConstraintViolation(
            evaluationSession,
            "name",
            "no_speech_detected",
            "warning",
            TelemetryErrorTriggers.SystemRecognition), Times.Once);
        telemetry.Verify(t => t.TrackRepairAttempt(evaluationSession, "name", "no_speech_detected"), Times.Once);
        telemetry.Verify(t => t.TrackFieldAttempt(evaluationSession, "name", "voice", true, It.IsAny<int?>()), Times.Once);
        telemetry.Verify(t => t.TrackGlobalCommand(evaluationSession, It.Is<string>(c => c != "submit")), Times.Never);
    }

    // Die echte Sprachdienstkomposition mit eingespeisten Transkripten muss für Bestätigung ShortConfirmation, sonst Listening verwenden; der Test misst keine Mikrofonaufnahme.
    [Fact]
    public async Task ExecuteAsync_ValueReadbackConfirmTurn_ArmsShortConfirmationNotListening()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalogWithSettings(new VoiceDialogSettings { MaxRetries = 0 }, CreateOneFieldApplication());
        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateOneFieldContract());
        var evaluationSession = new EvaluationSession("confirm-mode-session", EvaluationChannel.Voice, "one-field", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        telemetry
            .Setup(t => t.StartSessionAsync("one-field", "one-field", "de", EvaluationChannel.Voice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationSession);

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        speechClient.EnqueueTranscript("Max");
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        speechClient.EnqueueTranscript("übernehmen");
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry.Object);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal(
            [
                SpeechCaptureMode.Listening,
                SpeechCaptureMode.ShortConfirmation,
                SpeechCaptureMode.Listening,
            ],
            speechClient.RecordModes);
    }

    // Ein gestörter oder beendeter Audiostrom soll zur erneuten Feldabfrage führen, ohne das Formular aufzugeben.
    // capture_interrupted beschreibt den technischen Fehler; daraus folgt nicht, dass die Person nichts gesprochen hat.
    [Theory]
    [InlineData("stream-stalled")]
    [InlineData("session-ended")]
    public async Task ExecuteAsync_WhenCaptureStreamStallsOrSessionEnds_StaysInFieldAndRepromptsWithoutAbandoning(
        string recoverableCaptureFailureKind)
    {
        Exception recoverableCaptureFailure = recoverableCaptureFailureKind switch
        {
            "stream-stalled" => new SpeechCaptureStreamStalledException(),
            "session-ended" => new SpeechCaptureSessionEndedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(recoverableCaptureFailureKind)),
        };

        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalogWithSettings(new VoiceDialogSettings { MaxRetries = 0 }, CreateOneFieldApplication());
        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateOneFieldContract());
        var evaluationSession = new EvaluationSession("stall-recovery-session", EvaluationChannel.Voice, "one-field", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.StartSessionAsync("one-field", "one-field", "de", EvaluationChannel.Voice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationSession);
        telemetry
            .Setup(t => t.EndSessionAsync(evaluationSession, EvaluationOutcome.Submitted, It.IsAny<EvaluationTaskCompletion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAcceptance(evaluationSession));
        ConfigureAcceptedTerminalCommit(telemetry, evaluationSession);
        telemetry
            .Setup(t => t.TrackFieldFocus(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStatusSnapshot(evaluationSession, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry
            .Setup(t => t.TrackFieldAndGroupStatus(evaluationSession, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry
            .Setup(t => t.TrackFieldAttempt(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStateEntry(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackGlobalCommand(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackRepairAttempt(evaluationSession, It.IsAny<string>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackNoSpeechDetected(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackCommandRecognitionFailure(evaluationSession, It.IsAny<string>(), It.IsAny<string?>()));
        telemetry
            .Setup(t => t.TrackAmbiguityResolved(
                evaluationSession,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CommandSignalClass.NoCommand,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()));

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromException<CapturedSpeechAudio>(recoverableCaptureFailure));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry.Object);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal(4, speechClient.RecordCallCount);
        Assert.Equal(3, speechClient.SpeechToTextCallCount);

        var calls = tts.Calls.Skip(1).ToArray();
        Assert.Equal(6, calls.Length);
        Assert.Equal("Feld 1 von 1 heißt Name. Oder sagen Sie buchstabieren.", calls[1].Text);
        Assert.Equal("Feld 1 von 1 heißt Name.", calls[2].Text);
        Assert.Equal("Name ist Max. Zum Übernehmen sagen Sie übernehmen. Zum Korrigieren sagen Sie ändern oder buchstabieren.", calls[3].Text);
        Assert.Equal(FormDialogFlowPromptTextResolver.GetFormClosingReviewPrompt("de", PolitenessLevel.Formal), calls[4].Text);
        Assert.Equal("Das Formular wurde abgesendet.", calls[5].Text);

        telemetry.Verify(t => t.TrackFieldAttempt(evaluationSession, "name", "voice", false, It.IsAny<int?>()), Times.Once);
        telemetry.Verify(t => t.TrackConstraintViolation(
            evaluationSession,
            "name",
            "capture_interrupted",
            "warning",
            TelemetryErrorTriggers.SystemRecognition), Times.Once);
        telemetry.Verify(t => t.TrackRepairAttempt(evaluationSession, "name", "capture_interrupted"), Times.Once);
        telemetry.Verify(t => t.TrackNoSpeechDetected(evaluationSession, It.IsAny<string>()), Times.Never);
        telemetry.Verify(t => t.TrackFieldAttempt(evaluationSession, "name", "voice", true, It.IsAny<int?>()), Times.Once);
        telemetry.Verify(t => t.TrackGlobalCommand(evaluationSession, It.Is<string>(c => c != "submit")), Times.Never);
    }

    // Ein Fehler bei der Bestätigung in Gruppe 2 darf weder den Wert aus Gruppe 1 noch den gerade erkannten Auswahlwert verlieren.
    [Fact]
    public async Task ExecuteAsync_WhenRecoverableFaultAtGroupBoundaryEnumConfirmation_StaysInFormAndPreservesCommittedGroup()
    {
        var contract = CreateTwoGroupEnumBoundaryContract();
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalogWithSettings(
            new VoiceDialogSettings { MaxRetries = 0 },
            CreateCatalogForContract(contract).Applications[0]);
        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        telemetry
            .Setup(t => t.StartSessionAsync("boundary-form", "boundary-form", "de", EvaluationChannel.Voice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvaluationSession("boundary-session", EvaluationChannel.Voice, "boundary-form", "form-1"));

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromException<CapturedSpeechAudio>(new SpeechCaptureStreamStalledException()));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));

        speechClient.EnqueueTranscript("max@example.com");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("buch");
        speechClient.EnqueueTranscript("übernehmen");

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry.Object);

        var executionTask = dialog.ExecuteAsync(
            "de",
            "boundary-form",
            formId: "boundary-form",
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var completedTask = await Task.WhenAny(executionTask, Task.Delay(DialogExecutionTimeout));
        if (!ReferenceEquals(executionTask, completedTask))
        {
            var prompts = string.Join(" || ", tts.Calls.Select(call => call.Text));
            throw new Xunit.Sdk.XunitException(
                $"Boundary-enum dialog timed out. RecordCallCount={speechClient.RecordCallCount}, SpeechToTextCallCount={speechClient.SpeechToTextCallCount}, Prompts={prompts}");
        }

        var result = await executionTask;

        Assert.True(result.WasSubmitted, $"Prompts={string.Join(" || ", tts.Calls.Select(call => call.Text))}");
        Assert.Equal("max@example.com", result.ValuesByVariableId["email"]);
        Assert.Equal("buch", result.ValuesByVariableId["productCategory"]);
    }

    // Aufnahmefehler dürfen den Ziffernpuffer nicht vorzeitig abschließen. Nach der Wiederholung müssen auch die später genannten Ziffern enthalten sein.
    [Theory]
    [InlineData("stream-stalled")]
    public async Task ExecuteAsync_WhenRecoverableFaultDuringDigitsCapture_StaysInFieldAndCommitsTheWholeValue(
        string recoverableCaptureFailureKind)
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateDigitFriendlyInputModeContract(InputModes.Voice, InputModes.Digits));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        var recoverableFaultStep = CreateRecoverableCaptureFaultStep(recoverableCaptureFailureKind);

        // Die Fehler verbrauchen Wiederholungsversuche, aber nicht die einmalige Nachfrage bei tatsächlicher Pause.
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(recoverableFaultStep);
        await speechClient.EnqueueRecordStepAsync(recoverableFaultStep);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));

        speechClient.EnqueueTranscript("befehl ziffernmodus");
        speechClient.EnqueueTranscript("eins zwei drei");
        speechClient.EnqueueTranscript("vier funf sechs");
        speechClient.EnqueueTranscript("weiter");
        speechClient.EnqueueTranscript("übernehmen");

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var executionTask = dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var completedTask = await Task.WhenAny(executionTask, Task.Delay(DialogExecutionTimeout));
        if (!ReferenceEquals(executionTask, completedTask))
        {
            var prompts = string.Join(" || ", tts.Calls.Select(call => call.Text));
            throw new Xunit.Sdk.XunitException(
                $"Digits-capture recoverable-fault dialog timed out. RecordCallCount={speechClient.RecordCallCount}, Prompts={prompts}");
        }

        var result = await executionTask;

        Assert.True(result.WasSubmitted, $"Prompts={string.Join(" || ", tts.Calls.Select(call => call.Text))}");
        Assert.Equal("123456", result.ValuesByVariableId["name"]);
    }

    // Aufnahmefehler beim Buchstabieren müssen den bisherigen Puffer erhalten, bis weitere Zeichen und der ausdrückliche Abschluss folgen.
    [Theory]
    [InlineData("stream-stalled")]
    public async Task ExecuteAsync_WhenRecoverableFaultDuringSpellingCapture_StaysInFieldAndCommitsTheWholeValue(
        string recoverableCaptureFailureKind)
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateInputModeContract(InputModes.Voice, InputModes.Spelling));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        var recoverableFaultStep = CreateRecoverableCaptureFaultStep(recoverableCaptureFailureKind);

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(recoverableFaultStep);
        await speechClient.EnqueueRecordStepAsync(recoverableFaultStep);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));

        speechClient.EnqueueTranscript("befehl buchstabieren an");
        speechClient.EnqueueTranscript("a wie anton b wie berta drei");
        speechClient.EnqueueTranscript("c wie caesar");
        speechClient.EnqueueTranscript("fertig");
        speechClient.EnqueueTranscript("übernehmen");

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var executionTask = dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var completedTask = await Task.WhenAny(executionTask, Task.Delay(DialogExecutionTimeout));
        if (!ReferenceEquals(executionTask, completedTask))
        {
            var prompts = string.Join(" || ", tts.Calls.Select(call => call.Text));
            throw new Xunit.Sdk.XunitException(
                $"Spelling-capture recoverable-fault dialog timed out. RecordCallCount={speechClient.RecordCallCount}, Prompts={prompts}");
        }

        var result = await executionTask;

        Assert.True(result.WasSubmitted, $"Prompts={string.Join(" || ", tts.Calls.Select(call => call.Text))}");
        Assert.Equal("ab3c", result.ValuesByVariableId["name"]);
    }

    // Verankerte Hilfe bleibt an einer Mehrdeutigkeitsfrage über den echten Router erreichbar; geprüft werden angeforderte Texte und Abschluss.
    [Fact]
    public async Task ExecuteAsync_WhenAnchoredHelpAnswersTheAmbiguousCommandQuestion_HelpSoundsAndTheFormCompletes()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateInputModeContract(InputModes.Voice, InputModes.Keyboard));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 8);
        speechClient.EnqueueTranscript("befehl weiter", 70);
        speechClient.EnqueueTranscript("befehl hilfe");
        speechClient.EnqueueTranscript("Max", 95);
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var spoken = tts.Calls.Select(call => call.Text).ToList();
        var joined = string.Join(" || ", spoken);
        Assert.True(result.WasSubmitted, $"Prompts={joined}");
        Assert.Equal("Max", result.ValuesByVariableId["name"]);

        Assert.Equal(
            1,
            spoken.Count(text => text.Contains("zwei Optionen", StringComparison.Ordinal)));
        var questionIndex = spoken.FindIndex(text => text.Contains("zwei Optionen", StringComparison.Ordinal));
        var helpIndex = spoken.FindIndex(questionIndex + 1, text => text.Contains("welche Befehle", StringComparison.Ordinal));
        Assert.True(helpIndex > questionIndex, $"help must follow the ambiguity question. Prompts={joined}");
        Assert.DoesNotContain(spoken, text => text.Contains("nicht verstanden", StringComparison.Ordinal));
    }

    // Auch nach unsicherer Buchstabierung muss verankerte Hilfe den Einstieg wieder ermöglichen, ohne eine falsche Nichtverstehensmeldung.
    [Fact]
    public async Task ExecuteAsync_WhenAnchoredHelpAnswersTheSpellingEscalation_HelpSoundsAndTheFieldCompletes()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateInputModeContract(InputModes.Voice, InputModes.Keyboard));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 8);
        speechClient.EnqueueTranscript("befehl buchstabieren an");
        speechClient.EnqueueTranscript("a wie anton", 40);
        speechClient.EnqueueTranscript("fertig");
        speechClient.EnqueueTranscript("befehl hilfe");
        speechClient.EnqueueTranscript("b wie berta", 90);
        speechClient.EnqueueTranscript("fertig");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var spoken = tts.Calls.Select(call => call.Text).ToList();
        var joined = string.Join(" || ", spoken);
        Assert.True(result.WasSubmitted, $"Prompts={joined}");
        Assert.Equal("b", result.ValuesByVariableId["name"]);

        Assert.Equal(
            1,
            spoken.Count(text => text.Contains("möglicherweise falsch verstanden", StringComparison.Ordinal)));
        var escalationIndex = spoken.FindIndex(text => text.Contains("möglicherweise falsch verstanden", StringComparison.Ordinal));
        var helpIndex = spoken.FindIndex(escalationIndex + 1, text => text.Contains("welche Befehle", StringComparison.Ordinal));
        Assert.True(helpIndex > escalationIndex, $"help must follow the escalation readback. Prompts={joined}");
        Assert.DoesNotContain(spoken, text => text.Contains("nicht verstanden", StringComparison.Ordinal));
    }

    // Erschöpfte Aufnahmeversuche verwerfen den Ziffernteilwert. Die nächste Feldabfrage muss einen vollständigen neuen Wert erhalten.
    [Fact]
    public async Task ExecuteAsync_WhenRecognizerNeverRecoversDuringDigitsCapture_ReAsksTheFieldAndNeverCommitsThePartialValue()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateDigitFriendlyInputModeContract(InputModes.Voice, InputModes.Digits));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        var recoverableFaultStep = CreateRecoverableCaptureFaultStep("stream-stalled");

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        await speechClient.EnqueueRecordStepAsync(recoverableFaultStep);
        await speechClient.EnqueueRecordStepAsync(recoverableFaultStep);
        await speechClient.EnqueueRecordStepAsync(recoverableFaultStep);
        await speechClient.EnqueueRecordStepAsync(recoverableFaultStep);
        for (var i = 0; i < 6; i++)
        {
            await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        }

        speechClient.EnqueueTranscript("befehl ziffernmodus");
        speechClient.EnqueueTranscript("eins zwei drei");
        speechClient.EnqueueTranscript("befehl ziffernmodus");
        speechClient.EnqueueTranscript("sieben acht neun");
        speechClient.EnqueueTranscript("weiter");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var executionTask = dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var completedTask = await Task.WhenAny(executionTask, Task.Delay(DialogExecutionTimeout));
        if (!ReferenceEquals(executionTask, completedTask))
        {
            var prompts = string.Join(" || ", tts.Calls.Select(call => call.Text));
            throw new Xunit.Sdk.XunitException(
                $"Dead-recognizer digits dialog timed out. RecordCallCount={speechClient.RecordCallCount}, Prompts={prompts}");
        }

        var result = await executionTask;

        var spoken = string.Join(" || ", tts.Calls.Select(call => call.Text));
        Assert.True(result.WasSubmitted, $"Prompts={spoken}");

        Assert.DoesNotContain("Name ist 1 2 3", spoken, StringComparison.Ordinal);

        // Vor dem Neustart muss der Zifferndienst eine technische Fehlermeldung anfordern, damit die Person nicht von einer Fortsetzung des verworfenen Puffers ausgeht.
        Assert.Contains(
            RecoverableCapturePromptTextResolver.GetRecognizerUnavailablePrompt("de", PolitenessLevel.Formal),
            spoken,
            StringComparison.Ordinal);
        Assert.Equal("789", result.ValuesByVariableId["name"]);
    }

    internal static Func<CancellationToken, Task<CapturedSpeechAudio>> CreateRecoverableCaptureFaultStep(
        string recoverableCaptureFailureKind)
    {
        return recoverableCaptureFailureKind switch
        {
            "stream-stalled" => _ => Task.FromException<CapturedSpeechAudio>(new SpeechCaptureStreamStalledException()),
            "session-ended" => _ => Task.FromException<CapturedSpeechAudio>(new SpeechCaptureSessionEndedException()),
            _ => throw new ArgumentOutOfRangeException(nameof(recoverableCaptureFailureKind)),
        };
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelledDuringFirstField_DoesNotConfirmValue_AndEmitsCancelledTelemetry()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var mdaCatalog = new FixedFormsCatalogDataClient(CreateFormsCatalog());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateShortFormContract());
        var evaluationSession = new EvaluationSession("cancel-session", EvaluationChannel.Voice, "short-form", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.StartSessionAsync("short-form", "short-form", "de", EvaluationChannel.Voice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationSession);
        telemetry
            .Setup(t => t.EndSessionAsync(evaluationSession, It.IsAny<EvaluationOutcome>(), It.IsAny<EvaluationTaskCompletion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAcceptance(evaluationSession));
        ConfigureAcceptedTerminalCommit(telemetry, evaluationSession);
        telemetry
            .Setup(t => t.TrackFieldFocus(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStatusSnapshot(evaluationSession, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry
            .Setup(t => t.TrackFieldAndGroupStatus(evaluationSession, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry
            .Setup(t => t.TrackFieldAttempt(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStateEntry(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackGlobalCommand(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackDestructiveCommandResolved(
                evaluationSession,
                GlobalCommandIds.Cancel,
                true,
                false));
        telemetry
            .Setup(t => t.TrackAmbiguityResolved(
                evaluationSession,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CommandSignalClass.Clear,
                It.IsAny<string>(),
                It.IsAny<string>(),
                GlobalCommandIds.Cancel));

        await EnqueueUtterancesAsync(speechClient, 2);
        // Im Textfeld benötigt Abbrechen den ausdrücklich gesetzten Befehlsanker.
        speechClient.EnqueueTranscript("befehl abbrechen");
        speechClient.EnqueueTranscript("ja");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry.Object);

        var result = await dialog.ExecuteAsync(
            "de",
            "short-form",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var calls = tts.Calls.ToArray();
        Assert.StartsWith("Formular ", calls[0].Text);
        calls = calls.Skip(1).ToArray();
        Assert.Equal(4, calls.Length);
        Assert.Equal("Feldgruppe Kontakt.", calls[0].Text);
        Assert.Equal("Feld 1 von 4 heißt Vorname. Oder sagen Sie buchstabieren.", calls[1].Text);
        Assert.Equal("M\u00f6chten Sie wirklich abbrechen? Bitte sagen Sie ja oder nein.", calls[2].Text);
        Assert.Equal("Die Aufgabe wurde abgebrochen.", calls[3].Text);
        Assert.True(result.WasCancelled);

        telemetry.Verify(t => t.TrackGlobalCommand(evaluationSession, "cancel"), Times.Once);
        telemetry.Verify(t => t.TrackDestructiveCommandResolved(
            evaluationSession,
            GlobalCommandIds.Cancel,
            true,
            false), Times.Once);
        telemetry.Verify(t => t.EndSessionAsync(evaluationSession, EvaluationOutcome.Aborted, It.IsAny<EvaluationTaskCompletion>(), It.IsAny<CancellationToken>()), Times.Once);
        telemetry.Verify(t => t.TrackFieldAttempt(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>()), Times.Never);
        telemetry.Verify(t => t.TrackStateEntry(evaluationSession, "PROMPTING"), Times.Once);
        telemetry.Verify(t => t.TrackStateEntry(evaluationSession, "LISTENING"), Times.Once);
        telemetry.Verify(t => t.TrackStateEntry(evaluationSession, "CONFIRMATION"), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSharedVariablesConfigured_CarriesValuesFromPrerequisiteForm_AndAnnounces()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateSharedVariablesCatalog();
        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var mdaFormContracts = new MapMdaFormContractDataClient([CreateSharedVariablesTargetContract()]);
        var applicationSession = new SpeechAssistantApplicationSessionState(
            ImmutableDictionary.Create<string, SpeechAssistantFormSessionState>(StringComparer.OrdinalIgnoreCase)
                .Add(
                    "part1",
                    new SpeechAssistantFormSessionState(
                        ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase).Add("email", "max@example.com"),
                        IsSubmitted: true)));

        var evaluationSession = new EvaluationSession("shared-session", EvaluationChannel.Voice, "sample.multi", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.StartSessionAsync("sample.multi", "part2", "de", EvaluationChannel.Voice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationSession);
        telemetry
            .Setup(t => t.EndSessionAsync(evaluationSession, EvaluationOutcome.Submitted, It.IsAny<EvaluationTaskCompletion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAcceptance(evaluationSession));
        ConfigureAcceptedTerminalCommit(telemetry, evaluationSession);
        telemetry
            .Setup(t => t.TrackFieldFocus(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStatusSnapshot(evaluationSession, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry
            .Setup(t => t.TrackFieldAndGroupStatus(evaluationSession, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry
            .Setup(t => t.TrackFieldAttempt(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStateEntry(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackGlobalCommand(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackRepairAttempt(evaluationSession, It.IsAny<string>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackNoSpeechDetected(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackCommandRecognitionFailure(evaluationSession, It.IsAny<string>(), It.IsAny<string?>()));
        telemetry
            .Setup(t => t.TrackAmbiguityResolved(
                evaluationSession,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CommandSignalClass.Clear,
                It.IsAny<string>(),
                It.IsAny<string>(),
                GlobalCommandIds.Next));

        await EnqueueUtterancesAsync(speechClient, 2);
        speechClient.EnqueueTranscript("befehl weiter");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry.Object);

        var result = await dialog.ExecuteAsync(
            "de",
            "sample.multi",
            formId: "part2",
            applicationSession,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal("max@example.com", result.ValuesByVariableId["email"]);

        var calls = tts.Calls.ToArray();
        Assert.StartsWith("Formular ", calls[1].Text);
        calls = calls.Where((call, index) => index != 1).ToArray();
        Assert.Equal(4, calls.Length);
        Assert.Equal("Ein Hinweis vorab. Ich habe E-Mail aus dem Formular Teil 1 \u00fcbernommen.", calls[0].Text);
        Assert.Equal("Feldgruppe Main.", calls[1].Text);
        Assert.Equal("Feld 1 von 1 heißt E-Mail. Oder sagen Sie buchstabieren.", calls[2].Text);
        Assert.Equal("Das Formular wurde abgesendet.", calls[3].Text);

        telemetry.Verify(t => t.EndSessionAsync(evaluationSession, EvaluationOutcome.Submitted, It.IsAny<EvaluationTaskCompletion>(), It.IsAny<CancellationToken>()), Times.Once);
        telemetry.Verify(t => t.TrackGlobalCommand(evaluationSession, "next"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCatalogMaxRetriesIsZero_DoesNotRetryRecordingAndReprompts()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalogWithSettings(new VoiceDialogSettings { MaxRetries = 0 }, CreateOneFieldApplication());
        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateOneFieldContract());
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromException<CapturedSpeechAudio>(new NoSpeechDetectedException()));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1 } })));
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");

        var evaluationSession = new EvaluationSession("retry-session", EvaluationChannel.Voice, "one-field", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.StartSessionAsync("one-field", "one-field", "de", EvaluationChannel.Voice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationSession);
        telemetry
            .Setup(t => t.EndSessionAsync(evaluationSession, EvaluationOutcome.Submitted, It.IsAny<EvaluationTaskCompletion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAcceptance(evaluationSession));
        ConfigureAcceptedTerminalCommit(telemetry, evaluationSession);
        telemetry
            .Setup(t => t.TrackFieldFocus(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStatusSnapshot(evaluationSession, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry
            .Setup(t => t.TrackFieldAndGroupStatus(evaluationSession, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry
            .Setup(t => t.TrackFieldAttempt(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStateEntry(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackGlobalCommand(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackRepairAttempt(evaluationSession, It.IsAny<string>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackNoSpeechDetected(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackCommandRecognitionFailure(evaluationSession, It.IsAny<string>(), It.IsAny<string?>()));
        telemetry
            .Setup(t => t.TrackAmbiguityResolved(
                evaluationSession,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CommandSignalClass.NoCommand,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()));

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry.Object);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);

        Assert.Equal(4, speechClient.RecordCallCount);
        Assert.Equal(3, speechClient.SpeechToTextCallCount);

        var calls = tts.Calls.ToArray();
        Assert.StartsWith("Formular ", calls[0].Text);
        calls = calls.Skip(1).ToArray();
        Assert.Equal(6, calls.Length);
        Assert.Equal("Feldgruppe Main.", calls[0].Text);
        Assert.Equal("Feld 1 von 1 heißt Name. Oder sagen Sie buchstabieren.", calls[1].Text);
        Assert.Equal("Feld 1 von 1 heißt Name.", calls[2].Text);
        Assert.Equal("Name ist Max. Zum Übernehmen sagen Sie übernehmen. Zum Korrigieren sagen Sie ändern oder buchstabieren.", calls[3].Text);
        Assert.Equal(FormDialogFlowPromptTextResolver.GetFormClosingReviewPrompt("de", PolitenessLevel.Formal), calls[4].Text);
        Assert.Equal("Das Formular wurde abgesendet.", calls[5].Text);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCatalogNoInputTimeoutReached_CancelsRecordingAndReprompts()
    {
        var messageBus = new NoOpClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();

        var catalog = CreateCatalogWithSettings(
            new VoiceDialogSettings
            {
                NoInputTimeoutMs = (int)SpeechInputTestTimeouts.SchedulerSafeNoInputTimeout.TotalMilliseconds,
                MaxRetries = 0
            },
            CreateOneFieldApplication());
        var mdaCatalog = new FixedFormsCatalogDataClient(catalog);
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateOneFieldContract());
        // NoSpeechDetectedException simuliert die abgelaufene Anfangsstillefrist; der Test misst deren Dauer nicht.
        await speechClient.EnqueueRecordStepAsync(_ => throw new NoSpeechDetectedException());
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1 } })));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1 } })));
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");

        var evaluationSession = new EvaluationSession("timeout-session", EvaluationChannel.Voice, "one-field", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.StartSessionAsync("one-field", "one-field", "de", EvaluationChannel.Voice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationSession);
        telemetry
            .Setup(t => t.EndSessionAsync(evaluationSession, EvaluationOutcome.Submitted, It.IsAny<EvaluationTaskCompletion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAcceptance(evaluationSession));
        ConfigureAcceptedTerminalCommit(telemetry, evaluationSession);
        telemetry
            .Setup(t => t.TrackFieldFocus(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStatusSnapshot(evaluationSession, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry
            .Setup(t => t.TrackFieldAndGroupStatus(evaluationSession, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry
            .Setup(t => t.TrackFieldAttempt(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackConstraintViolation(evaluationSession, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStateEntry(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackGlobalCommand(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackRepairAttempt(evaluationSession, It.IsAny<string>(), It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackNoSpeechDetected(evaluationSession, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackCommandRecognitionFailure(evaluationSession, It.IsAny<string>(), It.IsAny<string?>()));
        telemetry
            .Setup(t => t.TrackAmbiguityResolved(
                evaluationSession,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CommandSignalClass.NoCommand,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()));

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry.Object);

        var executionTask = dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var completedTask = await Task.WhenAny(executionTask, Task.Delay(DialogExecutionTimeout));
        Assert.Same(executionTask, completedTask);

        var result = await executionTask;

        Assert.True(result.WasSubmitted);

        Assert.Equal(4, speechClient.RecordCallCount);
        Assert.Equal(3, speechClient.SpeechToTextCallCount);

        var calls = tts.Calls.ToArray();
        Assert.StartsWith("Formular ", calls[0].Text);
        calls = calls.Skip(1).ToArray();
        Assert.Equal(6, calls.Length);
        Assert.Equal("Feldgruppe Main.", calls[0].Text);
        Assert.Equal("Feld 1 von 1 heißt Name. Oder sagen Sie buchstabieren.", calls[1].Text);
        Assert.Equal("Feld 1 von 1 heißt Name.", calls[2].Text);
        Assert.Equal("Name ist Max. Zum Übernehmen sagen Sie übernehmen. Zum Korrigieren sagen Sie ändern oder buchstabieren.", calls[3].Text);
        Assert.Equal(FormDialogFlowPromptTextResolver.GetFormClosingReviewPrompt("de", PolitenessLevel.Formal), calls[4].Text);
        Assert.Equal("Das Formular wurde abgesendet.", calls[5].Text);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsingConcreteTelemetry_EmitsTaskEndWithoutAJudgeableTarget()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateOneFieldContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = CreateTelemetry(
            telemetryLogger);

        await EnqueueUtterancesAsync(speechClient, 3);
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);

        var taskStartEntry = Assert.Single(
            telemetryLogger.Entries,
            entry => string.Equals(entry.Values["event"] as string, "task_start", StringComparison.Ordinal));

        var taskEndEntry = Assert.Single(
            telemetryLogger.Entries,
            entry => string.Equals(entry.Values["event"] as string, "task_end", StringComparison.Ordinal));

        Assert.Equal("one-field", taskStartEntry.Values["task_id"]);
        Assert.Equal("one-field", taskEndEntry.Values["task_id"]);
        Assert.All(telemetryLogger.Entries, entry =>
        {
            Assert.Equal("one-field", entry.Values["task_id"]);
            Assert.Equal("one-field", entry.Values["application_id"]);
        });

        var taskEndData = Assert.IsType<string>(taskEndEntry.Values["data"]);
        using var taskEndJson = JsonDocument.Parse(taskEndData);
        // Dieses Kanalereignis belegt das Absenden, nicht das Erreichen des Aufgabenziels.
        // Ohne zweigliedrige Aufgabenkarte bleibt der Abweichungscode none.
        Assert.Equal("process_submitted", taskEndJson.RootElement.GetProperty("end_state").GetString());
        Assert.Equal(TelemetryMismatchCodes.None, taskEndJson.RootElement.GetProperty("mismatch_code").GetString());
        Assert.Equal(TelemetryCorrectnessBases.LogAndSystemCheck, taskEndJson.RootElement.GetProperty("correctness_basis").GetString());
        Assert.Equal(4, taskEndJson.RootElement.EnumerateObject().Count());

        Assert.DoesNotContain(
            telemetryLogger.Entries,
            entry => string.Equals(entry.Values["event"] as string, "manual_step", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_AtClosingTurn_ReviewsAllValuesThenSubmitsOnlyOnExplicitSubmit()
    {
        // Die Abschlussprüfung bleibt nach einer Zusammenfassung erneut offen; erst das ausdrückliche Absenden beendet sie.
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateRequiredOneFieldContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = CreateTelemetry(
            telemetryLogger);

        await EnqueueUtterancesAsync(speechClient, 4);
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("zusammenfassung");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry);

        var executionTask = dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);
        var completedTask = await Task.WhenAny(executionTask, Task.Delay(DialogExecutionTimeout));
        if (!ReferenceEquals(executionTask, completedTask))
        {
            var prompts = string.Join(" || ", tts.Calls.Select(call => call.Text));
            throw new Xunit.Sdk.XunitException(
                $"Closing-turn review dialog timed out. RecordCallCount={speechClient.RecordCallCount}, SpeechToTextCallCount={speechClient.SpeechToTextCallCount}, Prompts={prompts}");
        }

        var result = await executionTask;

        Assert.True(result.WasSubmitted);
        Assert.Equal("Max", result.ValuesByVariableId["name"]);

        var closingPrompt = FormDialogFlowPromptTextResolver.GetFormClosingReviewPrompt("de", PolitenessLevel.Formal);
        Assert.True(
            tts.Calls.Count(call => string.Equals(call.Text, closingPrompt, StringComparison.Ordinal)) >= 2,
            $"Expected the closing offer to be re-spoken. Prompts={string.Join(" || ", tts.Calls.Select(call => call.Text))}");
        Assert.Contains(tts.Calls, call => call.Text.Contains("Name ist Max", StringComparison.Ordinal));
        Assert.Contains(tts.Calls, call => string.Equals(call.Text, "Das Formular wurde abgesendet.", StringComparison.Ordinal));
        Assert.Contains(telemetryLogger.Entries, entry =>
            string.Equals(entry.Values["event"] as string, "intent_recognized", StringComparison.Ordinal)
            && (entry.Values["data"] as string)?.Contains("\"submit\"", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTextStartsWithOkayAndContainsCommandWord_ExecutesCommand()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateRequiredOneFieldContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = CreateTelemetry(
            telemetryLogger);

        await EnqueueUtterancesAsync(speechClient, 4);
        speechClient.EnqueueTranscript("okay weiter");
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal("Max", result.ValuesByVariableId["name"]);

        var intentEntry = Assert.Single(
            telemetryLogger.Entries,
            entry => string.Equals(entry.Values["event"] as string, "intent_recognized", StringComparison.Ordinal)
                && (entry.Values["data"] as string)?.Contains("\"next\"", StringComparison.Ordinal) == true);
        var intentData = Assert.IsType<string>(intentEntry.Values["data"]);
        using var intentJson = JsonDocument.Parse(intentData);
        Assert.Equal("next", intentJson.RootElement.GetProperty("intent").GetString());

        var valueChangeEntries = telemetryLogger.Entries
            .Where(entry => string.Equals(entry.Values["event"] as string, "value_change", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(valueChangeEntries);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTextMatchesCommandWithoutOkay_UsesValueCapture()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateRequiredOneFieldContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = CreateTelemetry(
            telemetryLogger);

        // Ohne Anker bleibt auch ein eingebettetes weiter Teil des vollständigen Feldwerts.
        await EnqueueUtterancesAsync(speechClient, 3);
        speechClient.EnqueueTranscript("ich gehe weiter nach hause");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal("ich gehe weiter nach hause", result.ValuesByVariableId["name"]);

        Assert.DoesNotContain(
            telemetryLogger.Entries,
            entry => string.Equals(entry.Values["event"] as string, "intent_recognized", StringComparison.Ordinal)
                && (entry.Values["data"] as string)?.Contains("\"submit\"", StringComparison.Ordinal) != true);

        var valueChangeEntries = telemetryLogger.Entries
            .Where(entry => string.Equals(entry.Values["event"] as string, "value_change", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(valueChangeEntries);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTextCommandHasExplicitPrefix_PrefersCommandBeforeValueCapture()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateRequiredOneFieldContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = CreateTelemetry(
            telemetryLogger);

        await EnqueueUtterancesAsync(speechClient, 4);
        speechClient.EnqueueTranscript("befehl weiter");
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal("Max", result.ValuesByVariableId["name"]);

        var intentEntry = Assert.Single(
            telemetryLogger.Entries,
            entry => string.Equals(entry.Values["event"] as string, "intent_recognized", StringComparison.Ordinal)
                && (entry.Values["data"] as string)?.Contains("\"next\"", StringComparison.Ordinal) == true);
        var intentData = Assert.IsType<string>(intentEntry.Values["data"]);
        using var intentJson = JsonDocument.Parse(intentData);
        Assert.Equal("next", intentJson.RootElement.GetProperty("intent").GetString());

        var valueChangeEntries = telemetryLogger.Entries
            .Where(entry => string.Equals(entry.Values["event"] as string, "value_change", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(valueChangeEntries);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_EmitsTaskEndWithoutAReachedTarget()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateOneFieldContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = CreateTelemetry(
            telemetryLogger);

        await EnqueueUtterancesAsync(speechClient, 2);
        // Der Abbruch während der Feldeingabe verwendet den ausdrücklichen Anker befehl.
        speechClient.EnqueueTranscript("befehl abbrechen");
        speechClient.EnqueueTranscript("ja");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasCancelled);

        var taskEnd = GetTaskEndData(telemetryLogger.Entries);
        // not_submitted bezeichnet den Kanalabschluss, unabhängig vom späteren Aufgabenurteil.
        Assert.Equal("not_submitted", taskEnd.GetProperty("end_state").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WhenReturningToAppSelection_EmitsTaskEndWithoutAReachedTarget()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateSharedVariablesCatalog());
        var mdaFormContracts = new MapMdaFormContractDataClient([CreateSharedVariablesTargetContract()]);
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = CreateTelemetry(
            telemetryLogger);

        await EnqueueUtterancesAsync(speechClient, 1);
        // Das Navigationskommando trägt den konfigurierten Anker Okay; ohne Anker wäre die Äußerung Feldinhalt.
        speechClient.EnqueueTranscript("Okay andere anwendung");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "sample.multi",
            formId: "part2",
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasAbandoned);
        Assert.Equal(VoiceCommandNavigationKind.BackToAppSelection, result.Navigation);

        var taskEnd = GetTaskEndData(telemetryLogger.Entries);
        // Die Rückkehr zur Auswahl meldet not_submitted, ohne das fachliche Aufgabenurteil festzulegen.
        Assert.Equal("not_submitted", taskEnd.GetProperty("end_state").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsingDiscoveryAndEditCommands_CompletesDiscoveryNavigationAndCorrectionFlow()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var contract = CreateDiscoveryDemoContract();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateCatalogForContract(contract));
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = CreateTelemetry(
            telemetryLogger);

        await EnqueueUtterancesAsync(speechClient, DiscoveryDemoSetupRecordingCount);
        await EnqueueUtterancesAsync(speechClient, 1);
        speechClient.EnqueueTranscript("Okay welche feldgruppen");
        speechClient.EnqueueTranscript("Okay gehe zu notizen");
        speechClient.EnqueueTranscript("Okay welche felder");
        speechClient.EnqueueTranscript("Okay gehe zu parkhinweis");
        speechClient.EnqueueTranscript("garage links");
        speechClient.EnqueueTranscript("Okay bearbeiten");
        speechClient.EnqueueTranscript("garage rechts");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("max@example.com");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(
            messageBus,
            speechClient,
            tts,
            mdaCatalog,
            mdaFormContracts,
            telemetry);

        var executionTask = dialog.ExecuteAsync(
            "de",
            contract.ApplicationId,
            formId: contract.FormId,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var completedTask = await Task.WhenAny(executionTask, Task.Delay(DialogExecutionTimeout));
        if (!ReferenceEquals(executionTask, completedTask))
        {
            var prompts = string.Join(" || ", tts.Calls.Select(call => call.Text));
            throw new Xunit.Sdk.XunitException(
                $"Dialog execution timed out. RecordCallCount={speechClient.RecordCallCount}, SpeechToTextCallCount={speechClient.SpeechToTextCallCount}, PromptCallCount={tts.Calls.Count}. Prompts={prompts}");
        }

        var result = await executionTask;

        Assert.True(result.WasSubmitted);
        Assert.Equal("garage rechts", result.ValuesByVariableId["parkingNote"]);
        Assert.Equal("max@example.com", result.ValuesByVariableId["email"]);
        Assert.Contains(tts.Calls, call => call.Text.Contains("Es gibt 2 relevante Feldgruppen", StringComparison.Ordinal));
        Assert.Contains(tts.Calls, call => call.Text.Contains("Notizen", StringComparison.Ordinal) && call.Text.Contains("Kontakt", StringComparison.Ordinal));
        Assert.Contains(tts.Calls, call => call.Text.Contains("In der Feldgruppe Notizen", StringComparison.Ordinal) && call.Text.Contains("Parkhinweis", StringComparison.Ordinal));
        Assert.Contains(tts.Calls, call => call.Text.Contains("Bitte nennen Sie den korrigierten Wert.", StringComparison.Ordinal));
        Assert.Contains(tts.Calls, call => call.Text.Contains("Parkhinweis ist garage rechts", StringComparison.Ordinal));

        var taskEnd = GetTaskEndData(telemetryLogger.Entries);
        Assert.Equal("process_submitted", taskEnd.GetProperty("end_state").GetString());
    }

    private static EvaluationSessionAcceptance CreateAcceptance(EvaluationSession session)
        => new(
            session.SessionId,
            "0123456789abcdef0123456789abcdef",
            new string('a', 64));

    private static void ConfigureAcceptedTerminalCommit(
        Mock<IEvaluationTelemetry> telemetry,
        EvaluationSession session)
    {
        var acceptance = CreateAcceptance(session);
        telemetry
            .Setup(candidate => candidate.CommitSessionAsync(acceptance, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    internal static FormVoiceCommandDialog CreateDialog(
        ISpeechInteractionSignals messageBus,
        ISpeechInputClient speechClient,
        ITextToSpeechPlayer textToSpeechPlayer,
        IFormsCatalogDataClient mdaCatalog,
        IMdaFormContractDataClient mdaFormContracts,
        IEvaluationTelemetry telemetry,
        ISystemClock? cancelHandlerClock = null)
    {
        var transcriptParser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());
        var recordingService = new FormVoiceCommandRecordingService(
            speechClient,
            speechClient as IModeAwareSpeechCaptureClient
                ?? throw new InvalidOperationException("FormVoiceCommandDialog tests require a mode-aware speech input double."));
        var signals = messageBus;
        var speechService = new FormVoiceCommandSpeechService(
            telemetry,
            signals,
            signals,
            new SpeechRecognitionWorker(speechClient),
            textToSpeechPlayer,
            recordingService,
            NullLogger<FormVoiceCommandSpeechService>.Instance);
        var sharedVariableAnnouncementService = new FormVoiceCommandSharedVariableAnnouncementService(
            speechService,
            new SharedVariableAnnouncementContentResolver(
                new SharedVariableAnnouncementLabelResolver(),
                new SharedVariableAnnouncementSourceResolver()),
            new SharedVariableAnnouncementPromptResolver());
        var promptService = new FormVoiceCommandPromptService(speechService, sharedVariableAnnouncementService);
        var submissionService = new FormVoiceCommandSubmissionService(telemetry, speechService);
        var formMatcher = new FormVoiceCommandFormMatcher();
        var languageResolver = new LanguageChangeTargetResolver(CreateLanguageCatalog());
        var politenessResolver = new FormVoiceCommandPolitenessResolver();
        var handlers = new IFormVoiceCommandGlobalCommandHandler[]
        {
            new FormVoiceCommandNavigationHandler(telemetry, submissionService, speechService),
            new FormVoiceCommandHelpHandler(promptService),
            new FormVoiceCommandStatusHandler(telemetry, speechService),
            new FormVoiceCommandDiscoveryHandler(speechService),
            new FormVoiceCommandJumpHandler(telemetry, speechService),
            new FormVoiceCommandInputModeHandler(telemetry, speechService),
            new FormVoiceCommandErrorHandler(telemetry, speechService),
            new FormVoiceCommandSubmitHandler(submissionService),
            new FormVoiceCommandCancelHandler(
                cancelHandlerClock ?? new FixedClock(new DateTimeOffset(2026, 2, 8, 15, 0, 0, TimeSpan.Zero)),
                telemetry,
                speechService),
            new FormVoiceCommandFormStatusHandler(telemetry, speechService),
            new FormVoiceCommandChangeLanguageHandler(telemetry, languageResolver, speechService),
            new FormVoiceCommandChangePolitenessHandler(telemetry, politenessResolver, speechService),
            CreateSwitchFormHandler(telemetry, formMatcher, speechService)
        };
        var router = new FormVoiceCommandGlobalCommandRouter(handlers);
        var earconPlayer = Mock.Of<IEarconPlayer>();
        var closingReviewTurnFlow = new FormClosingReviewTurnFlow(
            speechService,
            new FormVoiceCommandGlobalCommandExecutionService(telemetry, router));
        var fieldHandler = CreateFieldInteractionHandler(
            telemetry,
            speechService,
            submissionService,
            router);
        var runtimeFactory = new FormDialogRuntimeFactory(transcriptParser);
        var sessionResultFactory = new FormVoiceCommandSessionResultFactory();
        var surfacePublisher = new FormVoiceCommandSessionSurfacePublisher(
            new ReferenceFormDefinitionBuilder(),
            new SpeechAssistantFormSurfacePublication(
                new FormSurfaceSnapshotAssembler(),
                new SpeechAssistantFormSurfaceStore()));
        var sessionRunner = new FormVoiceCommandSessionRunner(
            telemetry,
            telemetry,
            messageBus,
            runtimeFactory,
            surfacePublisher,
            new FormVoiceCommandTerminalFeedbackOwner(
                speechService,
                earconPlayer,
                surfacePublisher,
                new FormVoiceCommandTerminalWorkLifetime()),
            new FormVoiceCommandFieldCoordinator(
                promptService,
                submissionService,
                fieldHandler,
                surfacePublisher,
                sessionResultFactory,
                closingReviewTurnFlow),
            sessionResultFactory,
            NullLogger<FormVoiceCommandSessionRunner>.Instance);
        var contextLoader = new FormVoiceCommandContextLoader(
            new FormVoiceCommandCatalogSnapshotLoader(
                mdaCatalog,
                Mock.Of<IVoiceDialogSettingsOverrideStore>(),
                NullLogger<FormVoiceCommandCatalogSnapshotLoader>.Instance),
            new FormVoiceCommandContractLoader(
                mdaFormContracts,
                NullLogger<FormVoiceCommandContractLoader>.Instance),
            new SharedVariableResolver(),
            new UserLanguageState(),
            new FixedWakeWordProvider(["Okay"]));

        return new FormVoiceCommandDialog(contextLoader, sessionRunner);
    }

    private static FormVoiceCommandSwitchFormHandler CreateSwitchFormHandler(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandFormMatcher formMatcher,
        IFormVoiceCommandSpeechService speechService)
        => new(
            new FormVoiceCommandSwitchFormNavigationExit(telemetry, speechService),
            new FormVoiceCommandSwitchFormTargetResolver(telemetry, formMatcher, speechService),
            new FormVoiceCommandSwitchFormPrerequisiteGuard(
                telemetry,
                speechService,
                new FormVoiceCommandSwitchFormPrerequisiteResolver()),
            new FormVoiceCommandSwitchFormConfirmationFlow(speechService));

    private static FormVoiceCommandFieldInteractionHandler CreateFieldInteractionHandler(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService,
        IFormVoiceCommandSubmissionService submissionService,
        IFormVoiceCommandGlobalCommandRouter commandRouter)
    {
        var commandExecutionService = new FormVoiceCommandGlobalCommandExecutionService(
            telemetry,
            commandRouter);
        var parser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());
        var replayBuffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var errorHandlingService = new FormVoiceCommandFieldErrorHandlingService(
            telemetry,
            speechService,
            replayBuffer,
            new CaptureReplayPolicy());
        var digitsWorkflowService = CreateDigitsWorkflowService(telemetry, speechService, parser);
        var ambiguityTelemetry = CreateInputVsCommandTelemetry(telemetry);
        var transcriptOrchestrator = new FormVoiceCommandFieldTranscriptOrchestrator(
            new FormVoiceCommandFieldTranscriptCapture(speechService, errorHandlingService),
            new FormVoiceCommandTranscriptInterpretationService(),
            new ParsedCommandBranch(
                commandExecutionService,
                errorHandlingService,
                ambiguityTelemetry,
                new FormVoiceCommandConfirmationDecisionService(
                    new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                    new FormVoiceCommandConfirmationTranscriptInterpreter(
                        new FormVoiceCommandConfirmationCommandDecisionMapper(),
                        parser),
                    new FormVoiceCommandConfirmationMissingDecisionPolicy()),
                telemetry),
            new FormVoiceCommandFieldEntryDependencies(
                new FormVoiceCommandInputAvailabilityGuard(errorHandlingService),
                new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                new FormVoiceCommandReadOnlyFieldSpeaker(speechService, replayBuffer)),
            new FormVoiceCommandDateComponentTranscriptBranch(
                new DateComponentEntryWorkflowDriver(speechService, parser, parser)),
            new FormVoiceCommandDigitsTranscriptBranch(digitsWorkflowService),
            new FormVoiceCommandSpellingTranscriptBranch(CreateSpellingWorkflowService(telemetry, speechService, parser)),
            new FormVoiceCommandSetTranscriptBranch(
                new FormVoiceCommandSetWorkflowService(
                    new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
                    AnchoredCommandFlowTestFactory.Create(commandExecutionService),
                    new SetWorkflowCommandResolver(new SetWorkflowGlobalCommandMapper()),
                    new SetWorkflowCommandActionFlow(telemetry),
                    new SetWorkflowInputRejectionFlow(new WorkflowUnrecognizedInputAnnouncer(telemetry, speechService)),
                    parser,
                    speechService)),
            new FormVoiceCommandAmbiguityTranscriptBranch(
                new FormVoiceCommandAmbiguityResolver(),
                errorHandlingService,
                ambiguityTelemetry));
        var fieldApplyService = new FormVoiceCommandFieldApplyService(
            telemetry,
            telemetry,
            errorHandlingService,
            new FormVoiceCommandConfirmationHandler(
                new FormVoiceCommandConfirmationValuePresenter(
                    speechService,
                    new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance)),
                new FormVoiceCommandConfirmationDecisionService(
                    new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                    new FormVoiceCommandConfirmationTranscriptInterpreter(
                        new FormVoiceCommandConfirmationCommandDecisionMapper(),
                        parser),
                    new FormVoiceCommandConfirmationMissingDecisionPolicy()),
                new FormVoiceCommandConfirmationRepairActionFlow(telemetry, speechService, commandExecutionService),
                new FormVoiceCommandConfirmedFieldCompletionFlow(
                    speechService,
                    Mock.Of<IEarconPlayer>(),
                    new FormClosingReviewTurnFlow(speechService, commandExecutionService)),
                errorHandlingService),
            new FormVoiceCommandConfirmationValuePresenter(
                speechService,
                new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance)),
            new FormVoiceCommandAdditionalFieldProposalHandler(
                telemetry,
                new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()),
                speechService),
            new FormVoiceCommandSpellingEscalationFlow(
                new SpellingConfidenceEscalationPolicy(),
                new FormVoiceCommandConfirmationValuePresenter(
                    speechService,
                    new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance)),
                new FormVoiceCommandConfirmationDecisionService(
                    new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                    new FormVoiceCommandConfirmationTranscriptInterpreter(
                        new FormVoiceCommandConfirmationCommandDecisionMapper(),
                        parser),
                    new FormVoiceCommandConfirmationMissingDecisionPolicy()),
                new FormVoiceCommandConfirmationRepairActionFlow(telemetry, speechService, commandExecutionService),
                Mock.Of<IEarconPlayer>()),
            new FormRepairReasonResolver());

        return new FormVoiceCommandFieldInteractionHandler(
            telemetry,
            speechService,
            transcriptOrchestrator,
            fieldApplyService,
            replayBuffer);
    }

    private static FormVoiceCommandConfirmationDecisionService CreateConfirmationDecisionService(
        IFormVoiceCommandSpeechService speechService,
        IFormTranscriptParser parser)
        => new(
            new FormVoiceCommandConfirmationTranscriptCapture(speechService),
            new FormVoiceCommandConfirmationTranscriptInterpreter(
                new FormVoiceCommandConfirmationCommandDecisionMapper(),
                parser),
            new FormVoiceCommandConfirmationMissingDecisionPolicy());

    private static FormVoiceCommandInputVsCommandTelemetry CreateInputVsCommandTelemetry(
        IEvaluationTelemetry telemetry)
        => new(telemetry, new InputVsCommandResolutionStrategyMapper());

    private static FormVoiceCommandDigitsWorkflowService CreateDigitsWorkflowService(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService,
        IFormTranscriptParser parser)
    {
        var transcriptCapture = new DigitsWorkflowTranscriptCapture(speechService);
        return new FormVoiceCommandDigitsWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            parser,
            CreateDigitsWorkflowCommandResolver(),
            new DigitsWorkflowCommandActionFlow(
                telemetry,
                speechService,
                new DigitsWorkflowNoValuePrompt(speechService)),
            new DigitsWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(telemetry, speechService)),
            transcriptCapture);
    }

    private static DigitsWorkflowCommandResolver CreateDigitsWorkflowCommandResolver()
        => new(
            new DigitsWorkflowLocalCommandMapper(),
            new DigitsWorkflowGlobalCommandMapper());

    private static FormVoiceCommandSpellingWorkflowService CreateSpellingWorkflowService(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService,
        IFormTranscriptParser parser)
        => new(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            new SpellingWorkflowCommandResolver(
                new SpellingWorkflowLocalCommandMapper(),
                new SpellingWorkflowGlobalCommandMapper()),
            parser,
            speechService,
            new SpellingWorkflowCommandActionFlow(
                telemetry,
                speechService,
                new SpellingWorkflowNoValuePrompt(speechService)),
            new SpellingWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(telemetry, speechService)),
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));

    internal static async Task EnqueueUtterancesAsync(QueuedSpeechInputClient client, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await client.EnqueueRecordingAsync(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } }));
        }
    }

    private static bool ContainsNormalizedToken(string text, string token)
    {
        var normalized = TextNormalizer.NormalizeForPhraseMatching(text);
        var normalizedToken = TextNormalizer.NormalizeForPhraseMatching(token);
        return TextNormalizer.MatchesAsWordOrPhrase(normalized, normalizedToken);
    }

    private static ILanguageCatalog CreateLanguageCatalog()
    {
        return new VoiceInterfaceLanguageCatalog(
            new SupportedLanguages(
                VoiceInterfaceModelCatalog.DefaultLanguage,
                VoiceInterfaceModelCatalog.SupportedLanguages
                    .Select(isoCode => new SpeechSupportedLanguage(
                        isoCode,
                        ResolveLanguageEndonym(isoCode)))
                    .ToArray()));
    }

    private static string ResolveLanguageEndonym(string isoCode)
    {
        if (VoiceInterfaceModelCatalog.Resources.TryGetResourceText(
                $"voice.languageName.{isoCode}",
                isoCode,
                out var endonym))
        {
            return endonym;
        }

        return isoCode;
    }

    private static FormsCatalog CreateFormsCatalog()
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            GeneratedAt = "2026-01-01T00:00:00Z",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "short-form",
                    DisplayName = new LocalizedText { ["de"] = "Kurzformular" },
                    Description = new LocalizedText { ["de"] = "Kurzformular" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("short-form"),
                            DisplayName = new LocalizedText { ["de"] = "Kurzformular" },
                            Description = new LocalizedText { ["de"] = "Kurzformular" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };
    }

    private static FormContract CreateShortFormContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "short-form",
            FormId = FormId.From("short-form"),
            DisplayName = new LocalizedText { ["de"] = "Kurzformular" },
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("contact"),
            StrictLocalization = true,
            Variables =
            [
                CreateStringVariable("firstName"),
                CreateStringVariable("lastName"),
                CreateStringVariable("email"),
                CreateStringVariable("emailConfirm")
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("firstName"),
                    VariableId = VariableId.From("firstName"),
                    Label = new LocalizedText { ["de"] = "Vorname" }
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("lastName"),
                    VariableId = VariableId.From("lastName"),
                    Label = new LocalizedText { ["de"] = "Nachname" }
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("email"),
                    VariableId = VariableId.From("email"),
                    Label = new LocalizedText { ["de"] = "E-Mail" }
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("emailConfirm"),
                    VariableId = VariableId.From("emailConfirm"),
                    Label = new LocalizedText { ["de"] = "E-Mail wiederholen" }
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("contact"),
                    Title = new LocalizedText { ["de"] = "Kontakt" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("firstName") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("lastName") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("email") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("emailConfirm") }
                    ]
                }
            ]
        };
    }

    internal static FormsCatalog CreateOneFieldDefinitions()
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            GeneratedAt = "2026-01-01T00:00:00Z",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "one-field",
                    DisplayName = new LocalizedText { ["de"] = "Ein Feld" },
                    Description = new LocalizedText { ["de"] = "Ein Feld" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("one-field"),
                            DisplayName = new LocalizedText { ["de"] = "Ein Feld" },
                            Description = new LocalizedText { ["de"] = "Ein Feld" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };
    }

    private static FormContract CreateOneFieldContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "one-field",
            FormId = FormId.From("one-field"),
            DisplayName = new LocalizedText { ["de"] = "Ein Feld" },
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                CreateStringVariable("name")
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" }
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("name") }]
                }
            ]
        };
    }

    // Zwei aufeinanderfolgende Gruppen mit einer Auswahlbedingung im zweiten Feld machen den Erhalt über die Gruppengrenze prüfbar.
    private static FormContract CreateTwoGroupEnumBoundaryContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "boundary-form",
            FormId = FormId.From("boundary-form"),
            DisplayName = new LocalizedText { ["de"] = "Grenzformular" },
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("root"),
            StrictLocalization = true,
            Variables =
            [
                CreateStringVariable("email"),
                CreateStringVariable("productCategory")
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("email"),
                    VariableId = VariableId.From("email"),
                    Label = new LocalizedText { ["de"] = "E-Mail" }
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("productCategory"),
                    VariableId = VariableId.From("productCategory"),
                    Label = new LocalizedText { ["de"] = "Produktkategorie" }
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("root"),
                    Title = new LocalizedText { ["de"] = "Gesamtformular" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("contact") },
                        new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("order") }
                    ]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("contact"),
                    Title = new LocalizedText { ["de"] = "Kontakt" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("email") }]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("order"),
                    Title = new LocalizedText { ["de"] = "Bestellung" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("productCategory") }]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "productCategoryOneOf",
                    Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("productCategory") },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.oneOf.invalid",
                    Message = new LocalizedText { ["de"] = "Diese Kategorie ist nicht zulässig." },
                    Kind = ConstraintKind.OneOf,
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["values"] = JsonDocument.Parse("[\"buch\",\"film\"]").RootElement
                    }
                }
            ]
        };
    }

    private static FormContract CreateDiscoveryDemoContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "discovery-demo",
            FormId = FormId.From("discovery-demo"),
            DisplayName = new LocalizedText { ["de"] = "Discovery Demo" },
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("root"),
            StrictLocalization = true,
            Variables =
            [
                CreateStringVariable("parkingNote"),
                CreateStringVariable("email")
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("parkingNote"),
                    VariableId = VariableId.From("parkingNote"),
                    Label = new LocalizedText { ["de"] = "Parkhinweis" }
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("email"),
                    VariableId = VariableId.From("email"),
                    Label = new LocalizedText { ["de"] = "E-Mail" }
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("root"),
                    Title = new LocalizedText { ["de"] = "Gesamtformular" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("notes") },
                        new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("contact") }
                    ]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("notes"),
                    Title = new LocalizedText { ["de"] = "Notizen" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("parkingNote") }]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("contact"),
                    Title = new LocalizedText { ["de"] = "Kontakt" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("email") }]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "emailRequired",
                    Target = new ConstraintTargetRef
                    {
                        Kind = TargetKinds.Field,
                        FieldId = FieldId.From("email")
                    },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.required",
                    Kind = ConstraintKind.Required,
                    Message = new LocalizedText { ["de"] = "Die E-Mail ist erforderlich." }
                }
            ]
        };
    }

    private static FormContract CreateRequiredOneFieldContract()
    {
        var valueType = CreateValueType("string");

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "one-field",
            FormId = FormId.From("one-field"),
            DisplayName = new LocalizedText { ["de"] = "Ein Feld" },
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = valueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" }
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("name") }]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "name_required",
                    Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("name") },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.required",
                    Message = new LocalizedText { ["de"] = "Name ist erforderlich." },
                    Kind = ConstraintKind.Required
                }
            ]
        };
    }

    private static FormsCatalog CreateCatalogWithSettings(VoiceDialogSettings settings, params ApplicationDescriptor[] applications)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            GeneratedAt = "2026-01-01T00:00:00Z",
            VoiceDialogSettings = settings,
            Applications = applications.ToList()
        };
    }

    private static ApplicationDescriptor CreateOneFieldApplication()
    {
        return new ApplicationDescriptor
        {
            ApplicationId = "one-field",
            DisplayName = new LocalizedText { ["de"] = "Ein Feld" },
            Description = new LocalizedText { ["de"] = "Ein Feld" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("one-field"),
                    DisplayName = new LocalizedText { ["de"] = "Ein Feld" },
                    Description = new LocalizedText { ["de"] = "Ein Feld" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        };
    }

    private static FormsCatalog CreateSharedVariablesCatalog()
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            GeneratedAt = "2026-01-01T00:00:00Z",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.multi",
                    DisplayName = new LocalizedText { ["de"] = "Multi" },
                    Description = new LocalizedText { ["de"] = "Multi" },
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
                            PrerequisiteForms = ["part1"],
                            SharedVariables = ["email"]
                        }
                    ]
                }
            ]
        };
    }

    internal static FormsCatalog CreateCatalogForContract(FormContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        var defaultLanguage = contract.DefaultLanguage
            ?? contract.SupportedLanguages.FirstOrDefault()
            ?? "de";

        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            GeneratedAt = "2026-01-01T00:00:00Z",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = contract.ApplicationId,
                    DisplayName = contract.DisplayName ?? new LocalizedText { ["de"] = contract.FormId },
                    Description = contract.Description ?? new LocalizedText { ["de"] = contract.FormId },
                    SupportedLanguages = contract.SupportedLanguages,
                    DefaultLanguage = defaultLanguage,
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = contract.FormId,
                            DisplayName = contract.DisplayName ?? new LocalizedText { ["de"] = contract.FormId },
                            Description = contract.Description ?? new LocalizedText { ["de"] = contract.FormId },
                            LatestContractVersion = contract.ContractVersion,
                            AvailableContractVersions = [contract.ContractVersion]
                        }
                    ]
                }
            ]
        };
    }

    private static FormContract CreateSharedVariablesTargetContract()
    {
        var valueType = CreateValueType("string");

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "sample.multi",
            FormId = FormId.From("part2"),
            DisplayName = new LocalizedText { ["de"] = "Teil 2" },
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("email"),
                    Name = "email",
                    ValueType = valueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("email"),
                    VariableId = VariableId.From("email"),
                    Label = new LocalizedText { ["de"] = "E-Mail" }
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("email") }]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "email_required",
                    Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("email") },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.required",
                    Message = new LocalizedText { ["de"] = "E-Mail ist erforderlich." },
                    Kind = ConstraintKind.Required
                }
            ]
        };
    }

    private static JsonElement CreateValueType(string kind)
    {
        using var doc = JsonDocument.Parse($"{{\"kind\":\"{kind}\"}}");
        return doc.RootElement.Clone();
    }

    private static JsonElement CreateStringValueTypeWithFormatHint(string formatHint)
    {
        using var doc = JsonDocument.Parse($$"""{"kind":"string","formatHint":"{{formatHint}}"}""");
        return doc.RootElement.Clone();
    }

    private static VariableDefinition CreateStringVariable(string variableId, string? formatHint = null)
    {
        return new VariableDefinition
        {
            VariableId = VariableId.From(variableId),
            Name = variableId,
            ValueType = formatHint is null
                ? CreateValueType("string")
                : CreateStringValueTypeWithFormatHint(formatHint)
        };
    }

    internal static JsonElement GetTaskEndData(IReadOnlyList<StructuredLogEntry> entries)
    {
        var taskEndEntry = Assert.Single(
            entries,
            entry => string.Equals(entry.Values["event"] as string, "task_end", StringComparison.Ordinal));
        var taskEndData = Assert.IsType<string>(taskEndEntry.Values["data"]);
        using var taskEndJson = JsonDocument.Parse(taskEndData);
        return taskEndJson.RootElement.Clone();
    }

    internal static IEvaluationTelemetry CreateTelemetry(ILogger<EvaluationTelemetry> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var context = new StudyTelemetryContext();
        var sessionStateStore = new EvaluationTelemetrySessionStateStore();
        var payloads = new EvaluationSessionTelemetryPayloads();
        var validator = new EvaluationTelemetryCheckpointValidator(
            payloads,
            new EvaluationTelemetryCheckpointEventValidator(
                new TelemetryEnvelopeSchemaValidator()), new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        var outbox = new EvaluationTelemetryTaskOutbox(
            context,
            new EvaluationTelemetryOutboxInfrastructure(
                new InMemoryEvaluationTelemetryOutboxStore(),
                new RecordingEvaluationTelemetryDeliveryTransport(),
                new ImmediateEvaluationTelemetryExclusiveLease(),
                new EvaluationTelemetryRetryDelay(),
                Options.Create(new EvaluationTelemetryOptions { RequireStudyContext = false }),
                logger),
            new EvaluationTelemetryStoredEventFactory(
                new EvaluationTelemetryEnvelopeSerializer(new TelemetryEnvelopeSchemaValidator()),
                payloads),
            validator, new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        var envelopeEmitter = new EvaluationTelemetryEnvelopeEmitter(
            context,
            outbox, new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        var sessionPayloads = new EvaluationSessionTelemetryPayloads();
        var fieldPayloads = new EvaluationFieldTelemetryPayloads();
        var statusPayloads = new EvaluationStatusTelemetryPayloads();
        var commandPayloads = new EvaluationCommandTelemetryPayloads();
        var pacingPayloads = new EvaluationConversationalPacingTelemetryPayloads();
        var pacingTelemetry = new EvaluationConversationalPacingTelemetry(
            sessionStateStore,
            envelopeEmitter,
            pacingPayloads,
            context);
        var detailedStatusChangeDetector = new EvaluationTelemetryDetailedStatusChangeDetector(statusPayloads);

        return new EvaluationTelemetry(
            new EvaluationSessionTelemetry(
                context,
                sessionStateStore,
                envelopeEmitter,
                sessionPayloads,
                pacingTelemetry),
            new EvaluationFormTelemetry(
                sessionStateStore,
                envelopeEmitter,
                fieldPayloads,
                statusPayloads,
                pacingTelemetry,
                detailedStatusChangeDetector),
            new EvaluationCommandTelemetry(
                envelopeEmitter,
                commandPayloads,
                pacingTelemetry));
    }

    private const string AmbiguityPromptLeadIn = "Ich habe zwei Optionen: eins als Eingabe";

    // Eine unverankerte Hilfephrase im Freitext bleibt vollständig Inhalt; sie darf keine Kommandoauswahlfrage oder Hilfe auslösen.
    [Fact]
    public async Task ExecuteAsync_WhenCommandWordEmbeddedInFreeText_CapturesAsContentWithoutRueckfrageOrCommand()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateOneFieldContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 3);
        speechClient.EnqueueTranscript("ich brauche Hilfe");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal("ich brauche Hilfe", result.ValuesByVariableId["name"]);

        Assert.DoesNotContain(tts.Calls, call => call.Text.StartsWith(AmbiguityPromptLeadIn, StringComparison.Ordinal));

        Assert.DoesNotContain(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackGlobalCommand)
                && call.StringArguments.Contains(GlobalCommandIds.Help));

        Assert.DoesNotContain(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackAmbiguityResolved)
                && call.StringArguments.Contains(CommandSignalClass.Ambiguous));
    }

    // Mit Aktivierungswort-Anker wird Hilfe unmittelbar als Kommando behandelt und nicht als Feldwert gespeichert.
    [Fact]
    public async Task ExecuteAsync_WhenAnchoredReadOnlyCommandInTextField_ExecutesCommandWithoutAmbiguity()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateOneFieldContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 4);
        speechClient.EnqueueTranscript("Okay Hilfe");
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal("Max", result.ValuesByVariableId["name"]);
        Assert.DoesNotContain(tts.Calls, call => call.Text.StartsWith(AmbiguityPromptLeadIn, StringComparison.Ordinal));
        Assert.DoesNotContain(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackAmbiguityResolved)
                && call.StringArguments.Contains(CommandSignalClass.Ambiguous));
        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackGlobalCommand)
                && call.StringArguments.Contains(GlobalCommandIds.Help));
    }

    // Eine unverankerte Orientierungsphrase bleibt Freitext; sie löst weder Rückfrage noch Orientierungsbefehl aus.
    [Fact]
    public async Task ExecuteAsync_WhenEmbeddedDiscoveryPhrasingInTextField_CapturesAsContentWithoutRueckfrage()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateOneFieldContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 3);
        speechClient.EnqueueTranscript("ich frage mich was gibt es noch");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal("ich frage mich was gibt es noch", result.ValuesByVariableId["name"]);

        Assert.DoesNotContain(tts.Calls, call => call.Text.StartsWith(AmbiguityPromptLeadIn, StringComparison.Ordinal));

        Assert.DoesNotContain(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackGlobalCommand)
                && call.StringArguments.Contains(GlobalCommandIds.FieldList));

        Assert.DoesNotContain(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackAmbiguityResolved)
                && call.StringArguments.Contains(CommandSignalClass.Ambiguous));
    }

    // eingabe ist kein besonderes Präfix und bleibt zusammen mit dem folgenden Kommandowort im Feldwert.
    [Fact]
    public async Task ExecuteAsync_WhenFormerInputPrefixWordPrecedesCommandWord_CapturesVerbatimAndSuppressesCommand()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateOneFieldContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 3);
        speechClient.EnqueueTranscript("eingabe Hilfe");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal("eingabe Hilfe", result.ValuesByVariableId["name"], ignoreCase: true);

        Assert.DoesNotContain(tts.Calls, call => call.Text.StartsWith(AmbiguityPromptLeadIn, StringComparison.Ordinal));
        Assert.DoesNotContain(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackGlobalCommand)
                && !call.StringArguments.Contains("submit"));

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackFieldAttempt)
                && call.StringArguments.Contains("name")
                && call.StringArguments.Contains(bool.TrueString));
    }

    // Das ausdrückliche Präfix Befehl macht Hilfe zum Kommando; erst der anschließend genannte echte Wert darf übernommen werden.
    [Fact]
    public async Task ExecuteAsync_WhenExplicitCommandPrefixPrecedesCommandWord_ExecutesCommandAndDoesNotApplyValue()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateOneFieldContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 4);
        speechClient.EnqueueTranscript("befehl hilfe");
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal("Max", result.ValuesByVariableId["name"]);

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackGlobalCommand)
                && call.StringArguments.Contains(GlobalCommandIds.Help));

        var successfulFieldAttempts = telemetry.Calls
            .Where(call => call.Method == nameof(RecordingEvaluationTelemetry.TrackFieldAttempt)
                && call.StringArguments.Contains(bool.TrueString))
            .ToArray();
        Assert.Single(successfulFieldAttempts);
    }

    internal static FormContract CreateInputModeContract(params string[] preferredInputModes)
        => CreateInputModeContractCore(formatHint: null, preferredInputModes);

    internal static FormContract CreateDigitFriendlyInputModeContract(params string[] preferredInputModes)
        => CreateInputModeContractCore(formatHint: "phone", preferredInputModes);

    private static FormContract CreateInputModeContractCore(string? formatHint, params string[] preferredInputModes)
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "one-field",
            FormId = FormId.From("one-field"),
            DisplayName = new LocalizedText { ["de"] = "Ein Feld" },
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables = [CreateStringVariable("name", formatHint)],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
                    PreferredInputModes = preferredInputModes.ToList(),
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("name") }]
                }
            ]
        };
    }

    // Die Bedingung ^Marta$ verlangt genaue Großschreibung. Der Test gibt sie beim Buchstabieren ausdrücklich vor.
    private static FormContract CreateCaseSensitiveNameContract()
    {
        var contract = CreateInputModeContract(InputModes.Voice, InputModes.Spelling);
        contract.Constraints =
        [
            new ConstraintDefinition
            {
                ConstraintId = "namePattern",
                Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("name") },
                Severity = ConstraintSeverity.Error,
                Code = "form.pattern.invalid",
                Message = new LocalizedText { ["de"] = "Der Name ist ungültig." },
                Kind = ConstraintKind.Pattern,
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    ["pattern"] = JsonSerializer.SerializeToDocument("^Marta$").RootElement
                }
            }
        ];
        return contract;
    }

    // Moduswechsel, Buchstabierung und Wertbestätigung laufen gemeinsam; ab3 muss als Buchstabierergebnis samt Moduswechsel erfasst werden.
    [Fact]
    public async Task ExecuteAsync_WhenSpellingModeSwitchedOnThenSpelled_CapturesCanonicalSpellingValue()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateInputModeContract(InputModes.Voice, InputModes.Spelling));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 6);
        speechClient.EnqueueTranscript("befehl buchstabieren an");
        speechClient.EnqueueTranscript("a wie anton b wie berta drei");
        // Das erste leere Erkennungsergebnis lässt die Zeichensammlung offen und fragt nach; erst das zweite aufeinanderfolgende leere Ergebnis schließt sie ab.
        speechClient.EnqueueTranscript(string.Empty);
        speechClient.EnqueueTranscript(string.Empty);
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal("ab3", result.ValuesByVariableId["name"]);

        Assert.Contains(tts.Calls, call => call.Text.StartsWith("Buchstabiermodus aktiv.", StringComparison.Ordinal));

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackInputModeChanged)
                && call.StringArguments.SequenceEqual(["name", InputModes.Voice, InputModes.Spelling, TelemetryInputModeTriggers.ExplicitCommand]));

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackFieldAttempt)
                && call.StringArguments.Contains(InputModes.Spelling)
                && call.StringArguments.Contains(bool.TrueString));
    }

    // Der Buchstabierablauf bildet Marta mit großem M. Die angeforderte phonetische Rückmeldung muss diese Großschreibung benennen.
    [Fact]
    public async Task ExecuteAsync_WhenUppercaseSpelled_PassesCaseSensitivePatternGate()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateCaseSensitiveNameContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 6);
        speechClient.EnqueueTranscript("befehl buchstabieren an");
        speechClient.EnqueueTranscript("großes m wie martha a wie anton r wie richard t wie theodor a wie anton");
        speechClient.EnqueueTranscript(string.Empty);
        speechClient.EnqueueTranscript(string.Empty);
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var executionTask = dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var completedTask = await Task.WhenAny(executionTask, Task.Delay(DialogExecutionTimeout));
        if (!ReferenceEquals(executionTask, completedTask))
        {
            var prompts = string.Join(" || ", tts.Calls.Select(call => call.Text));
            throw new Xunit.Sdk.XunitException(
                $"Uppercase-spelling pattern-gate dialog timed out. RecordCallCount={speechClient.RecordCallCount}, Prompts={prompts}");
        }

        var result = await executionTask;

        Assert.True(result.WasSubmitted, $"Prompts={string.Join(" || ", tts.Calls.Select(call => call.Text))}");
        Assert.Equal("Marta", result.ValuesByVariableId["name"]);

        Assert.Contains(
            tts.Calls,
            call => call.Text.Contains("großes M wie Martha", StringComparison.Ordinal));
    }

    // Im wörtlichen Modus bleibt unverankertes weiter Feldinhalt und darf weder ein Kommando noch eine Ablehnungsansage auslösen.
    // Nur vier Aufnahmen sind bereitgestellt: Moduswechsel, Wert, Bestätigung und Absenden. Eine unerwartete weitere Aufnahme würde auf Eingabe warten.
    [Fact]
    public async Task ExecuteAsync_WhenLiteralModeActive_CapturesCommandWordAsValueUntilLiteralOff()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateInputModeContract(InputModes.Voice, InputModes.Literal));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 4);
        speechClient.EnqueueTranscript("befehl wörtlich an");
        speechClient.EnqueueTranscript("weiter");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal(4, speechClient.RecordCallCount);

        Assert.Equal("weiter", result.ValuesByVariableId["name"], ignoreCase: true);

        Assert.DoesNotContain(tts.Calls, call => call.Text.Contains("Wörtlichmodus", StringComparison.Ordinal) && call.Text.Contains("blockiert", StringComparison.Ordinal));

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackInputModeChanged)
                && call.StringArguments.SequenceEqual(["name", InputModes.Voice, InputModes.Literal, TelemetryInputModeTriggers.ExplicitCommand]));

        Assert.DoesNotContain(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackGlobalCommand)
                && call.StringArguments.Contains(GlobalCommandIds.Next));

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackFieldAttempt)
                && call.StringArguments.Contains("name")
                && call.StringArguments.Contains(bool.TrueString));
    }

    // Wörtliche Eingabe ohne Anker bleibt vollständig erhalten, auch wenn sie eingabe enthält. Verankerte Kommandos werden gesondert geprüft.
    [Theory]
    [InlineData("weiter", "weiter")]
    [InlineData("eingabe weiter", "eingabe weiter")]
    public async Task ExecuteAsync_WhenLiteralModeActive_CapturesUtteranceAsValue(
        string spokenUtterance,
        string expectedStoredValue)
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateInputModeContract(InputModes.Voice, InputModes.Literal));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 4);
        speechClient.EnqueueTranscript("befehl wörtlich an");
        speechClient.EnqueueTranscript(spokenUtterance);
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal(4, speechClient.RecordCallCount);
        Assert.Equal(expectedStoredValue, result.ValuesByVariableId["name"], ignoreCase: true);

        Assert.DoesNotContain(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackGlobalCommand)
                && call.StringArguments.Contains(GlobalCommandIds.Next));
    }

    // Auch im wörtlichen Modus führen Okay oder befehl vor weiter zum Kommando statt zur Wertübernahme.
    [Theory]
    [InlineData("okay weiter")]
    [InlineData("befehl weiter")]
    public async Task ExecuteAsync_WhenLiteralModeActive_AnchoredOrPrefixedCommandExecutesInsteadOfCapture(
        string commandUtterance)
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateInputModeContract(InputModes.Voice, InputModes.Literal));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 3);
        speechClient.EnqueueTranscript("befehl wörtlich an");
        speechClient.EnqueueTranscript(commandUtterance);
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal(2, speechClient.RecordCallCount);

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackGlobalCommand)
                && call.StringArguments.Contains(GlobalCommandIds.Next));

        Assert.False(
            result.ValuesByVariableId.TryGetValue("name", out var stored)
                && stored.Contains("weiter", StringComparison.OrdinalIgnoreCase),
            $"The anchored command must not be captured as the literal field value; stored='{stored}'.");
    }

    // Ein ausdrücklich verankerter Ausstieg beendet den wörtlichen Modus. Anschließend wird der normale Sprachwert Max übernommen.
    [Theory]
    [InlineData("befehl wörtlich aus")]
    [InlineData("befehl literal aus")]
    public async Task ExecuteAsync_WhenLiteralModeActive_LiteralOffExitsAndRestoresCommandBehavior(
        string exitUtterance)
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateInputModeContract(InputModes.Voice, InputModes.Literal));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 5);
        speechClient.EnqueueTranscript("befehl wörtlich an");
        speechClient.EnqueueTranscript(exitUtterance);
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal(5, speechClient.RecordCallCount);

        Assert.Equal("Max", result.ValuesByVariableId["name"]);

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackInputModeChanged)
                && call.StringArguments.SequenceEqual(["name", InputModes.Voice, InputModes.Literal, TelemetryInputModeTriggers.ExplicitCommand]));
        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackInputModeChanged)
                && call.StringArguments.SequenceEqual(["name", InputModes.Literal, InputModes.Voice, TelemetryInputModeTriggers.ExplicitCommand]));
    }

    // Die Ziffernblöcke 123 und 45 werden gesammelt; das Löschen des letzten Blocks muss den Wert 123 für die Bestätigung zurücklassen.
    // funf ist hier die normalisierte Fixtureform von fünf.
    [Fact]
    public async Task ExecuteAsync_WhenDigitsModeWithCorrection_CapturesCorrectedDigitValue()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateDigitFriendlyInputModeContract(InputModes.Voice, InputModes.Digits));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        // Sieben Aufnahmen sind vorgesehen; eine unerwartete achte würde auf weitere Eingabe warten.
        await EnqueueUtterancesAsync(speechClient, 7);
        speechClient.EnqueueTranscript("befehl ziffernmodus");
        speechClient.EnqueueTranscript("eins zwei drei");
        speechClient.EnqueueTranscript("vier funf");
        speechClient.EnqueueTranscript("letzten Block löschen");
        speechClient.EnqueueTranscript("weiter");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal(7, speechClient.RecordCallCount);
        Assert.Equal("123", result.ValuesByVariableId["name"]);

        Assert.Contains(tts.Calls, call => call.Text == "Ziffernmodus aktiv.");

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackInputModeChanged)
                && call.StringArguments.SequenceEqual(["name", InputModes.Voice, InputModes.Digits, TelemetryInputModeTriggers.ExplicitCommand]));

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackFieldAttempt)
                && call.StringArguments.Contains(InputModes.Digits)
                && call.StringArguments.Contains(bool.TrueString));
    }

    // Eine bevorzugte Zifferneingabe ist noch keine Zulassung. Ohne passenden Feldtyp muss der Modus unverändert bleiben und die Ablehnung auf Tastatureingabe verweisen.
    [Fact]
    public async Task ExecuteAsync_WhenDigitsCommandIsDeniedForTheFieldType_ModeStaysVoiceAndDialogContinues()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateInputModeContract(InputModes.Voice, InputModes.Digits));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 10);
        speechClient.EnqueueTranscript("befehl ziffernmodus");
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted, $"Prompts={string.Join(" || ", tts.Calls.Select(call => call.Text))}");
        Assert.Equal("Max", result.ValuesByVariableId["name"]);

        Assert.Contains(
            tts.Calls,
            call => call.Text == "Dieser Eingabemodus ist für dieses Feld nicht verfügbar. Sie können die Tastatur nutzen.");
        Assert.DoesNotContain(tts.Calls, call => call.Text == "Ziffernmodus aktiv.");

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackConstraintViolation)
                && call.StringArguments.Contains(InputModeAdmissionErrorCodes.InputModeUnavailable));
        Assert.DoesNotContain(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackInputModeChanged)
                && call.StringArguments.Contains(InputModes.Digits));

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackFieldAttempt)
                && call.StringArguments.Contains(InputModes.Voice)
                && call.StringArguments.Contains(bool.TrueString));
    }

    private const string SensitiveSentinel = "SENSITIVE-E2E-7F3A9C21-4B62-44D0-A913-0E2E5C0FFEE1";

    // Am einzigen Feld ohne Spracheingabe soll die Ablehnung einmal erscheinen. Danach wartet der Dialog neutral auf Kommandos; eingespeiste Stille darf keine Nichtverstehensmeldung auslösen.
    [Fact]
    public async Task ExecuteAsync_WhenOnlyFieldIsVoiceForbidden_DenialOnceThenCommandParkReachesCancel()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateSingleForbiddenFieldContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 8);
        speechClient.EnqueueNoSpeechDetected();
        speechClient.EnqueueTranscript("befehl abbrechen");
        speechClient.EnqueueTranscript("ja");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasCancelled, $"Prompts={string.Join(" || ", tts.Calls.Select(call => call.Text))}");

        Assert.Equal(
            1,
            tts.Calls.Count(call => call.Text.StartsWith(
                "Für dieses Feld ist Spracheingabe deaktiviert.",
                StringComparison.Ordinal)));

        Assert.Contains(
            tts.Calls,
            call => call.Text.StartsWith("Ich warte auf einen Befehl.", StringComparison.Ordinal));
        Assert.DoesNotContain(
            tts.Calls,
            call => call.Text.Contains("nicht verstanden", StringComparison.Ordinal));
        Assert.Equal(
            1,
            telemetry.Calls.Count(call => call.Method == nameof(RecordingEvaluationTelemetry.TrackFieldAttempt)
                && call.StringArguments.Contains("name")));
    }

    private static FormContract CreateSingleForbiddenFieldContract()
    {
        var contract = CreateInputModeContract(InputModes.Voice, InputModes.Keyboard);
        contract.Variables[0].PrivacyClass = PrivacyClass.Secret;
        contract.Variables[0].SensitiveDataPolicy = new SensitiveDataPolicy
        {
            VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputForbidden,
            SilentInputFallback = SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard
        };
        return contract;
    }


    // In diesem sensiblen Feld erlaubt die modellierte Eingaberegel direktes Buchstabieren. Die spätere Zustimmung zum Vorlesen ist davon unabhängig.
    [Fact]
    public async Task ExecuteAsync_WhenSpellingCommandOnSensitiveField_NoConsentQuestionAndOneSpellingTurnOpens()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateConsentGatedVoiceFirstContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 10);
        speechClient.EnqueueTranscript("befehl buchstabieren");
        speechClient.EnqueueTranscript("anton");
        speechClient.EnqueueTranscript("fertig");
        speechClient.EnqueueTranscript("nein");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted, $"Prompts={string.Join(" || ", tts.Calls.Select(call => call.Text))}");
        Assert.Equal("a", result.ValuesByVariableId["secret"]);

        var prompts = tts.Calls.Select(call => call.Text).ToArray();
        Assert.DoesNotContain(
            prompts,
            text => text.StartsWith("Dieses Feld ist vertraulich.", StringComparison.Ordinal));
        Assert.Single(
            prompts,
            text => text.StartsWith("Buchstabiermodus aktiv.", StringComparison.Ordinal));
        Assert.DoesNotContain(
            prompts,
            text => text.StartsWith("Buchstabieren Sie weiter", StringComparison.Ordinal));
        Assert.DoesNotContain(prompts, text => text.Contains("nicht verfügbar", StringComparison.Ordinal));

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackInputModeChanged)
                && call.StringArguments.Contains(InputModes.Spelling));
    }

    private static FormContract CreateConsentGatedVoiceFirstContract()
    {
        var contract = CreateReadbackOptInContract();
        contract.Fields[0].PreferredInputModes = [InputModes.Voice, InputModes.Keyboard];
        return contract;
    }


    // Die Eingaberegel erlaubt die Mikrofonaufnahme dieses sensiblen Feldes; nach abgelehnter Vorlesezustimmung folgt eine maskierte Bestätigung.
    [Fact]
    public async Task ExecuteAsync_WhenSensitiveFieldIsCaptured_NoConsentQuestionSoundsAndValueCommits()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateConsentGatedContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 8);
        speechClient.EnqueueTranscript("g wie gustav");
        speechClient.EnqueueTranscript("fertig");
        speechClient.EnqueueTranscript("nein");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var executionTask = dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var completedTask = await Task.WhenAny(executionTask, Task.Delay(DialogExecutionTimeout));
        if (!ReferenceEquals(executionTask, completedTask))
        {
            var prompts = string.Join(" || ", tts.Calls.Select(call => call.Text));
            throw new Xunit.Sdk.XunitException(
                $"Sensitive-field dialog timed out. RecordCallCount={speechClient.RecordCallCount}, Prompts={prompts}");
        }

        var result = await executionTask;

        Assert.True(result.WasSubmitted, $"Prompts={string.Join(" || ", tts.Calls.Select(call => call.Text))}");
        Assert.False(string.IsNullOrWhiteSpace(result.ValuesByVariableId["secret"]));

        Assert.DoesNotContain(
            tts.Calls,
            call => call.Text.StartsWith("Dieses Feld ist vertraulich.", StringComparison.Ordinal));
        Assert.DoesNotContain(
            tts.Calls,
            call => call.Text.StartsWith("In Ordnung. Sie können den Wert", StringComparison.Ordinal));
        Assert.DoesNotContain(tts.Calls, call => call.Text.Contains("nicht verfügbar", StringComparison.Ordinal));
    }

    private static FormContract CreateConsentGatedContract()
    {
        var contract = CreateReadbackOptInContract();
        contract.Fields[0].PreferredInputModes = [InputModes.Spelling, InputModes.Keyboard];
        return contract;
    }


    // Das erste Feld ohne Spracheingabe wird einmal erklärt und übersprungen. Der nächste gesprochene Wert gehört zum zweiten Feld.
    [Fact]
    public async Task ExecuteAsync_WhenVoiceInputForbiddenFieldIsDeniedOnce_DialogAdvancesWithoutRecording()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateForbiddenThenTextContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 8);
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted, $"Prompts={string.Join(" || ", tts.Calls.Select(call => call.Text))}");
        Assert.Equal("Max", result.ValuesByVariableId["name"]);

        Assert.Equal(
            1,
            tts.Calls.Count(call => call.Text.StartsWith(
                "Für dieses Feld ist Spracheingabe deaktiviert.",
                StringComparison.Ordinal)));
        Assert.Equal(
            1,
            telemetry.Calls.Count(call => call.Method == nameof(RecordingEvaluationTelemetry.TrackFieldAttempt)
                && call.StringArguments.Contains("name")));
    }

    private static FormContract CreateForbiddenThenTextContract()
    {
        var contract = CreateInputModeContract(InputModes.Voice, InputModes.Keyboard);
        contract.Variables.Insert(0, new VariableDefinition
        {
            VariableId = VariableId.From("secret"),
            Name = "secret",
            PrivacyClass = PrivacyClass.Secret,
            SensitiveDataPolicy = new SensitiveDataPolicy
            {
                VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputForbidden,
                SilentInputFallback = SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard
            },
            ValueType = CreateValueType("string")
        });
        contract.Fields.Insert(0, new FieldDefinition
        {
            FieldId = FieldId.From("secretField"),
            VariableId = VariableId.From("secret"),
            Label = new LocalizedText { ["de"] = "Geheimnis" },
            Editability = FieldEditability.UserEditable
        });
        contract.FieldGroups[0].Members.Insert(0, new FieldGroupMember { Kind = "field", FieldId = FieldId.From("secretField") });
        return contract;
    }


    // Die zugelassene Tastaturanforderung beendet den Sprachdialog mit Feldkennung und Übergabewunsch, ohne das Formular abzusenden.
    [Fact]
    public async Task ExecuteAsync_WhenKeyboardModeIsRequested_DialogEndsWithSilentInputRequest()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateInputModeContract(InputModes.Voice, InputModes.Keyboard));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 4);
        speechClient.EnqueueTranscript("befehl tastatureingabe");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.False(result.WasSubmitted, $"Prompts={string.Join(" || ", tts.Calls.Select(call => call.Text))}");
        Assert.True(
            result.IsSilentInputRequested,
            $"Prompts={string.Join(" || ", tts.Calls.Select(call => call.Text))}");
        Assert.Equal("name", result.SilentInputEligibility?.FieldId);

        Assert.DoesNotContain(
            tts.Calls,
            call => call.Text.StartsWith("Für dieses Feld ist Spracheingabe deaktiviert.", StringComparison.Ordinal));
        Assert.DoesNotContain(tts.Calls, call => call.Text.Contains("nicht verfügbar", StringComparison.Ordinal));
    }


    // Nach abgelehnter Vorlesezustimmung darf der Wert weder in den angeforderten Ansagen noch in den hier aufgezeichneten Telemetrie- und Protokollargumenten erscheinen.
    [Fact]
    public async Task ExecuteAsync_WhenSensitiveReadbackDeclined_MasksValueAndNeverLeaksToTelemetryOrLogs()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateReadbackOptInContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 4);
        speechClient.EnqueueTranscript(SensitiveSentinel);
        speechClient.EnqueueTranscript("nein");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        var storedValue = result.ValuesByVariableId["secret"];
        Assert.False(string.IsNullOrWhiteSpace(storedValue));

        Assert.DoesNotContain(tts.Calls, call => ContainsSensitive(call.Text, storedValue));

        AssertSensitiveAbsentFromForbiddenSinks(telemetry, telemetryLogger, storedValue);
    }

    // Ausdrückliche Vorlesezustimmung erlaubt den Wert im TTS-Aufruf. Die hier erfassten Telemetrie- und Protokollargumente müssen weiterhin ohne ihn bleiben.
    [Fact]
    public async Task ExecuteAsync_WhenSensitiveReadbackOptedIn_SpeaksValueButNeverLeaksToTelemetryOrLogs()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateReadbackOptInContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 4);
        speechClient.EnqueueTranscript(SensitiveSentinel);
        speechClient.EnqueueTranscript("ja");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        var storedValue = result.ValuesByVariableId["secret"];
        Assert.False(string.IsNullOrWhiteSpace(storedValue));

        Assert.Contains(tts.Calls, call => ContainsSensitive(call.Text, storedValue));

        AssertSensitiveAbsentFromForbiddenSinks(telemetry, telemetryLogger, storedValue);
    }

    // Die injizierte Uhr bleibt stehen: Beide Abbruchkommandos liegen damit im selben Fünfsekundenfenster. Nach abgelehnter erster Bestätigung muss der zweite Versuch gesperrt bleiben.
    [Fact]
    public async Task ExecuteAsync_WhenCancelTriggeredTwiceWithinRefractory_BlocksSecondAndRecordsSingleResolution()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(
            CreateCatalogWithSettings(new VoiceDialogSettings { CancelRefractoryMs = 5000 }, CreateOneFieldApplication()));
        var mdaFormContracts = new FixedMdaFormContractDataClient(CreateOneFieldContract());
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var clock = new MutableClock(new DateTimeOffset(2026, 2, 8, 21, 0, 0, TimeSpan.Zero));
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            telemetryLogger));

        await EnqueueUtterancesAsync(speechClient, 6);
        speechClient.EnqueueTranscript("befehl abbrechen");
        speechClient.EnqueueTranscript("nein");
        speechClient.EnqueueTranscript("befehl abbrechen");
        speechClient.EnqueueTranscript("Max");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry, clock);

        var result = await dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal("Max", result.ValuesByVariableId["name"]);

        Assert.Single(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackDestructiveCommandResolved)
                && call.StringArguments.SequenceEqual([GlobalCommandIds.Cancel, bool.FalseString, bool.FalseString]));

        Assert.Single(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackDestructiveCommandResolved)
                && call.StringArguments.SequenceEqual([GlobalCommandIds.Cancel, bool.FalseString, bool.TrueString]));

        Assert.DoesNotContain(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackDestructiveCommandResolved)
                && call.StringArguments.SequenceEqual([GlobalCommandIds.Cancel, bool.TrueString, bool.FalseString]));
    }

    private static bool ContainsSensitive(string text, string storedValue)
        => text.Contains(SensitiveSentinel, StringComparison.OrdinalIgnoreCase)
            || text.Contains(storedValue, StringComparison.Ordinal);

    private static void AssertSensitiveAbsentFromForbiddenSinks(
        RecordingEvaluationTelemetry telemetry,
        StructuredCapturingLogger<EvaluationTelemetry> telemetryLogger,
        string storedValue)
    {
        // Prüft alle aufgezeichneten Stringargumente der Telemetrie auf den sensiblen Wert.
        Assert.DoesNotContain(telemetry.StringArguments, argument => ContainsSensitive(argument, storedValue));

        // Prüft außerdem die Stringdarstellung jedes Werts der strukturierten Studienprotokolle, einschließlich der JSON-Nutzlast.
        var logFragments = telemetryLogger.Entries
            .SelectMany(entry => entry.Values.Values.Select(value => value?.ToString() ?? string.Empty))
            .ToArray();
        Assert.DoesNotContain(logFragments, fragment => ContainsSensitive(fragment, storedValue));
    }

    private static FormContract CreateReadbackOptInContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "one-field",
            FormId = FormId.From("one-field"),
            DisplayName = new LocalizedText { ["de"] = "Ein Feld" },
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("secret"),
                    Name = "secret",
                    ValueType = CreateValueType("string"),
                    PrivacyClass = PrivacyClass.Sensitive,
                    SensitiveDataPolicy = new SensitiveDataPolicy
                    {
                        VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputRecommended,
                        ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackOptInRequired
                    }
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("secret"),
                    VariableId = VariableId.From("secret"),
                    Label = new LocalizedText { ["de"] = "Geheim" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("secret") }]
                }
            ]
        };
    }

    // Diese Fälle liefern typgültige Werte, die eine Formularbedingung verletzen. Ein Parserfehler wird gesondert geprüft, damit beide Fehlerklassen unterscheidbar bleiben.

    [Fact]
    public async Task ExecuteAsync_WhenOneOfValueOutOfSet_RejectsAndRepromptsUntilValidValue()
    {
        var contract = CreateSingleConstraintStringContract(
            "oneOfForm",
            "favoriteColor",
            "Lieblingsfarbe",
            new ConstraintDefinition
            {
                ConstraintId = "colorOneOf",
                Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("favoriteColor") },
                Severity = ConstraintSeverity.Error,
                Code = "form.oneOf.invalid",
                Message = new LocalizedText { ["de"] = "Diese Farbe ist nicht zulässig." },
                Remediation = new LocalizedText { ["de"] = "Bitte rotes Auto oder blaues Auto nennen." },
                Kind = ConstraintKind.OneOf,
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    ["values"] = JsonDocument.Parse("[\"rotes Auto\",\"blaues Auto\"]").RootElement
                }
            });

        var result = await RunSingleFieldRejectThenAcceptAsync(
            contract,
            violatingValue: "gruenes Auto",
            validValue: "rotes Auto");

        AssertConstraintViolationTrackedAndReprompted(result, "form.oneOf.invalid", "favoriteColor", "Lieblingsfarbe");
        Assert.Equal("rotes Auto", result.DialogResult.ValuesByVariableId["favoriteColor"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNumericRangeValueOutOfRange_RejectsAndRepromptsUntilValidValue()
    {
        var contract = CreateSingleConstraintIntegerContract(
            "rangeForm",
            "guests",
            "Personenzahl",
            new ConstraintDefinition
            {
                ConstraintId = "guestsRange",
                Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("guests") },
                Severity = ConstraintSeverity.Error,
                Code = "form.range.invalid",
                Message = new LocalizedText { ["de"] = "Die Personenzahl muss zwischen 1 und 10 liegen." },
                Kind = ConstraintKind.Range,
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    [ConstraintPropertyNames.Min] = JsonDocument.Parse("1").RootElement,
                    [ConstraintPropertyNames.Max] = JsonDocument.Parse("10").RootElement
                }
            });

        var result = await RunSingleFieldRejectThenAcceptAsync(
            contract,
            violatingValue: "99",
            validValue: "5");

        AssertConstraintViolationTrackedAndReprompted(result, "form.range.invalid", "guests", "Personenzahl");
        Assert.Equal("5", result.DialogResult.ValuesByVariableId["guests"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNumericMaxExceeded_RejectsAndRepromptsUntilValidValue()
    {
        var contract = CreateSingleConstraintIntegerContract(
            "maxForm",
            "age",
            "Alter",
            new ConstraintDefinition
            {
                ConstraintId = "ageMax",
                Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("age") },
                Severity = ConstraintSeverity.Error,
                Code = "form.max.invalid",
                Message = new LocalizedText { ["de"] = "Das Alter darf höchstens 120 sein." },
                Kind = ConstraintKind.Max,
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    [ConstraintPropertyNames.Max] = JsonDocument.Parse("120").RootElement
                }
            });

        var result = await RunSingleFieldRejectThenAcceptAsync(
            contract,
            violatingValue: "200",
            validValue: "30");

        AssertConstraintViolationTrackedAndReprompted(result, "form.max.invalid", "age", "Alter");
        Assert.Equal("30", result.DialogResult.ValuesByVariableId["age"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMinLengthTooShort_RejectsAndRepromptsUntilValidValue()
    {
        var contract = CreateSingleConstraintStringContract(
            "minLengthForm",
            "comment",
            "Kommentar",
            new ConstraintDefinition
            {
                ConstraintId = "commentMinLength",
                Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("comment") },
                Severity = ConstraintSeverity.Error,
                Code = "form.minLength.invalid",
                Message = new LocalizedText { ["de"] = "Der Kommentar ist zu kurz." },
                Kind = ConstraintKind.MinLength,
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    ["minLength"] = JsonDocument.Parse("10").RootElement
                }
            });

        var result = await RunSingleFieldRejectThenAcceptAsync(
            contract,
            violatingValue: "kurz",
            validValue: "ausreichend langer Kommentar");

        AssertConstraintViolationTrackedAndReprompted(result, "form.minLength.invalid", "comment", "Kommentar");
        Assert.Equal("ausreichend langer Kommentar", result.DialogResult.ValuesByVariableId["comment"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMaxLengthExceeded_RejectsAndRepromptsUntilValidValue()
    {
        var contract = CreateSingleConstraintStringContract(
            "maxLengthForm",
            "nickname",
            "Spitzname",
            new ConstraintDefinition
            {
                ConstraintId = "nicknameMaxLength",
                Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("nickname") },
                Severity = ConstraintSeverity.Error,
                Code = "form.maxLength.invalid",
                Message = new LocalizedText { ["de"] = "Der Spitzname ist zu lang." },
                Kind = ConstraintKind.MaxLength,
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    ["maxLength"] = JsonDocument.Parse("8").RootElement
                }
            });

        var result = await RunSingleFieldRejectThenAcceptAsync(
            contract,
            violatingValue: "viel zu langer spitzname",
            validValue: "kurz");

        AssertConstraintViolationTrackedAndReprompted(result, "form.maxLength.invalid", "nickname", "Spitzname");
        Assert.Equal("kurz", result.DialogResult.ValuesByVariableId["nickname"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPatternViolated_RejectsAndRepromptsUntilValidValue()
    {
        var contract = CreateSingleConstraintStringContract(
            "patternForm",
            "postalCode",
            "Postleitzahl",
            new ConstraintDefinition
            {
                ConstraintId = "postalCodePattern",
                Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("postalCode") },
                Severity = ConstraintSeverity.Error,
                Code = "form.pattern.invalid",
                Message = new LocalizedText { ["de"] = "Die Postleitzahl ist ungültig." },
                Kind = ConstraintKind.Pattern,
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    ["pattern"] = JsonSerializer.SerializeToDocument("^[0-9]{5}$").RootElement
                }
            });

        var result = await RunSingleFieldRejectThenAcceptAsync(
            contract,
            violatingValue: "abcde",
            validValue: "10115");

        AssertConstraintViolationTrackedAndReprompted(result, "form.pattern.invalid", "postalCode", "Postleitzahl");
        Assert.Equal("10115", result.DialogResult.ValuesByVariableId["postalCode"]);
    }

    // Ziffer 3 und Zahlwort drei müssen beide die exakte Bereichsbedingung [3, 3] erfüllen.
    [Theory]
    [InlineData("3")]
    [InlineData("drei")]
    public async Task ExecuteAsync_WhenIntegerRangeExact_SpokenValuePasses(string validValue)
    {
        var contract = CreateSingleConstraintIntegerContract(
            "rangeForm",
            "quantity",
            "Anzahl",
            new ConstraintDefinition
            {
                ConstraintId = "quantityRange",
                Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("quantity") },
                Severity = ConstraintSeverity.Error,
                Code = "form.range.invalid",
                Message = new LocalizedText { ["de"] = "Die Anzahl ist ungültig." },
                Kind = ConstraintKind.Range,
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    ["min"] = JsonDocument.Parse("3").RootElement,
                    ["max"] = JsonDocument.Parse("3").RootElement
                }
            });

        var result = await RunSingleFieldRejectThenAcceptAsync(
            contract,
            violatingValue: "5",
            validValue: validValue);

        AssertConstraintViolationTrackedAndReprompted(result, "form.range.invalid", "quantity", "Anzahl");
        Assert.Equal("3", result.DialogResult.ValuesByVariableId["quantity"]);
    }

    // Optionslabels werden auf Codes abgebildet; erst der Code wird gegen oneOf geprüft.
    [Theory]
    [InlineData("e-mail")]
    [InlineData("e-mail bitte")]
    public async Task ExecuteAsync_WhenEnumOptionNamedByLabel_ResolvesTheOptionCode(string validValue)
    {
        var enumValueType = JsonDocument.Parse(
            "{\"kind\":\"enum\",\"values\":[" +
            "{\"code\":\"email\",\"displayName\":{\"de\":\"E-Mail\"}}," +
            "{\"code\":\"phone\",\"displayName\":{\"de\":\"Telefon\"}}," +
            "{\"code\":\"letter\",\"displayName\":{\"de\":\"Brief\"}}]}").RootElement;
        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "enum-form",
            FormId = FormId.From("enum-form"),
            DisplayName = new LocalizedText { ["de"] = "Kontaktkanal" },
            DefaultLanguage = "de",
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("contactChannel"),
                    Name = "contactChannel",
                    ValueType = enumValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("contactChannel"),
                    VariableId = VariableId.From("contactChannel"),
                    Label = new LocalizedText { ["de"] = "Kontaktkanal" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("contactChannel") }]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "channelOneOf",
                    Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("contactChannel") },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.oneOf.invalid",
                    Message = new LocalizedText { ["de"] = "Der Kontaktkanal ist ungültig." },
                    Kind = ConstraintKind.OneOf,
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["values"] = JsonDocument.Parse("[\"email\"]").RootElement
                    }
                }
            ]
        };

        var result = await RunSingleFieldRejectThenAcceptAsync(
            contract,
            violatingValue: "brief",
            validValue: validValue);

        Assert.Equal("email", result.DialogResult.ValuesByVariableId["contactChannel"]);
    }

    // Gesprochene deutsche Uhrzeiten werden vor oneOf nach HH:mm normalisiert.
    [Theory]
    [InlineData("vierzehn uhr dreißig")]
    [InlineData("14 uhr 30")]
    public async Task ExecuteAsync_WhenTimeOneOf_SpokenTime_EvaluatesAgainstTheGate(string validValue)
    {
        var contract = CreateSingleConstraintContract(
            "timeForm",
            "callbackTime",
            "Uhrzeit",
            "time",
            new ConstraintDefinition
            {
                ConstraintId = "timeOneOf",
                Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("callbackTime") },
                Severity = ConstraintSeverity.Error,
                Code = "form.oneOf.invalid",
                Message = new LocalizedText { ["de"] = "Die Uhrzeit ist ungültig." },
                Kind = ConstraintKind.OneOf,
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    ["values"] = JsonSerializer.SerializeToDocument(new[] { "14:30" }).RootElement
                }
            });

        var result = await RunSingleFieldRejectThenAcceptAsync(
            contract,
            violatingValue: "neun uhr",
            validValue: validValue);

        Assert.Equal("14:30", result.DialogResult.ValuesByVariableId["callbackTime"]);
    }

    // Das jahrlose Datum wird vor oneOf ergänzt. Die Fixture erwartet fünfzehnter märz als 2026-03-15; eine entsprechende Datumsuhr wird hier nicht injiziert.
    [Fact]
    public async Task ExecuteAsync_WhenDateOneOf_YearlessSpokenDate_EvaluatesAgainstTheGate()
    {
        var contract = CreateSingleConstraintContract(
            "dateForm",
            "deliveryDate",
            "Datum",
            "date",
            new ConstraintDefinition
            {
                ConstraintId = "dateOneOf",
                Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("deliveryDate") },
                Severity = ConstraintSeverity.Error,
                Code = "form.oneOf.invalid",
                Message = new LocalizedText { ["de"] = "Das Datum ist ungültig." },
                Kind = ConstraintKind.OneOf,
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    ["values"] = JsonSerializer.SerializeToDocument(new[] { "2026-03-15" }).RootElement
                }
            });

        var result = await RunSingleFieldRejectThenAcceptAsync(
            contract,
            violatingValue: "zwölfter märz",
            validValue: "fünfzehnter märz");

        Assert.Equal("2026-03-15", result.DialogResult.ValuesByVariableId["deliveryDate"]);
    }


    [Fact]
    public async Task ExecuteAsync_WhenCrossFieldCompareVariablesViolated_RejectsAndRepromptsFocusedField()
    {
        var contract = CreateCrossFieldDateOrderContract();

        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateCatalogForContract(contract));
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            new StructuredCapturingLogger<EvaluationTelemetry>()));

        // Ein Enddatum vor dem Beginn verletzt die feldübergreifende Bedingung; die Korrektur muss nur das Endfeld erneut abfragen.
        await EnqueueUtterancesAsync(speechClient, 7);
        speechClient.EnqueueTranscript("2026-04-10");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("2026-04-01");
        speechClient.EnqueueTranscript("2026-04-20");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var executionTask = dialog.ExecuteAsync(
            "de",
            contract.ApplicationId,
            formId: contract.FormId,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var completedTask = await Task.WhenAny(executionTask, Task.Delay(DialogExecutionTimeout));
        if (!ReferenceEquals(executionTask, completedTask))
        {
            var prompts = string.Join(" || ", tts.Calls.Select(call => call.Text));
            throw new Xunit.Sdk.XunitException(
                $"Cross-field dialog timed out. Prompts={prompts}");
        }

        var result = await executionTask;

        Assert.True(result.WasSubmitted);
        Assert.Equal("2026-04-20", result.ValuesByVariableId["endDate"]);

        var violationCall = Assert.Single(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackConstraintViolation)
                && call.StringArguments.Contains("form.dateOrder"));
        Assert.Contains("endDateField", violationCall.StringArguments);

        // Die Feldbezeichnung steht in der Orientierungszeile bei heißt Enddatum, nicht am Anfang der Ansage.
        var endDatePrompts = tts.Calls
            .Where(call => call.Text.Contains("heißt Enddatum.", StringComparison.Ordinal))
            .ToArray();
        Assert.True(
            endDatePrompts.Length >= 2,
            $"Expected the end-date field to be re-prompted after the cross-field violation. Prompts={string.Join(" || ", tts.Calls.Select(call => call.Text))}");
    }

    // Ein nicht als Ganzzahl lesbarer Wert muss unrecognized_input auslösen; eine Bereichsverletzung darf dafür nicht gemeldet werden.
    [Fact]
    public async Task ExecuteAsync_WhenUnparseableNumber_RepromptsViaUnrecognizedInput_NotConstraintViolation()
    {
        var contract = CreateSingleConstraintIntegerContract(
            "parserNegativeForm",
            "guests",
            "Personenzahl",
            new ConstraintDefinition
            {
                ConstraintId = "guestsRange",
                Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("guests") },
                Severity = ConstraintSeverity.Error,
                Code = "form.range.invalid",
                Message = new LocalizedText { ["de"] = "Die Personenzahl muss zwischen 1 und 10 liegen." },
                Kind = ConstraintKind.Range,
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    [ConstraintPropertyNames.Min] = JsonDocument.Parse("1").RootElement,
                    [ConstraintPropertyNames.Max] = JsonDocument.Parse("10").RootElement
                }
            });

        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateCatalogForContract(contract));
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            new StructuredCapturingLogger<EvaluationTelemetry>()));

        await EnqueueUtterancesAsync(speechClient, 7);
        speechClient.EnqueueTranscript("keine Zahl");
        speechClient.EnqueueTranscript("5");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var executionTask = dialog.ExecuteAsync(
            "de",
            contract.ApplicationId,
            formId: contract.FormId,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var completedTask = await Task.WhenAny(executionTask, Task.Delay(DialogExecutionTimeout));
        if (!ReferenceEquals(executionTask, completedTask))
        {
            var prompts = string.Join(" || ", tts.Calls.Select(call => call.Text));
            throw new Xunit.Sdk.XunitException($"Parser-negative dialog timed out. Prompts={prompts}");
        }

        var result = await executionTask;

        Assert.True(result.WasSubmitted);
        Assert.Equal("5", result.ValuesByVariableId["guests"]);

        Assert.Contains(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackRepairAttempt)
                && call.StringArguments.Contains(FormVoiceCommandFieldErrorCodes.UnrecognizedInput));
        Assert.DoesNotContain(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackConstraintViolation)
                && call.StringArguments.Contains("form.range.invalid"));
    }

    // Dieselbe Bereichsbedingung weist nicht jede Eingabe zurück.
    [Fact]
    public async Task ExecuteAsync_WhenNumericValueInRange_AcceptsWithoutConstraintViolation()
    {
        var contract = CreateSingleConstraintIntegerContract(
            "rangeControlForm",
            "guests",
            "Personenzahl",
            new ConstraintDefinition
            {
                ConstraintId = "guestsRange",
                Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("guests") },
                Severity = ConstraintSeverity.Error,
                Code = "form.range.invalid",
                Message = new LocalizedText { ["de"] = "Die Personenzahl muss zwischen 1 und 10 liegen." },
                Kind = ConstraintKind.Range,
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    [ConstraintPropertyNames.Min] = JsonDocument.Parse("1").RootElement,
                    [ConstraintPropertyNames.Max] = JsonDocument.Parse("10").RootElement
                }
            });

        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateCatalogForContract(contract));
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            new StructuredCapturingLogger<EvaluationTelemetry>()));

        await EnqueueUtterancesAsync(speechClient, 5);
        speechClient.EnqueueTranscript("5");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await dialog.ExecuteAsync(
            "de",
            contract.ApplicationId,
            formId: contract.FormId,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.True(result.WasSubmitted);
        Assert.Equal("5", result.ValuesByVariableId["guests"]);
        Assert.DoesNotContain(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackConstraintViolation)
                && call.StringArguments.Contains("form.range.invalid"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenMultiSelectFirstTurnNamesOptionsAndDoneInOneBreath_SelectsBothAndCompletesWithoutASecondConfirmation()
    {
        // Zwei Optionen und fertig in einer Äußerung müssen direkt zur kanonischen Auswahl führen. fertig bestätigt die Auswahl bereits; eine zusätzliche generische Wertbestätigung entfällt.
        var contract = CreateMultiSelectContract("channels-form", "contactChannels", "Kontaktkanäle", minSelected: 1);

        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateCatalogForContract(contract));
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            new StructuredCapturingLogger<EvaluationTelemetry>()));

        await EnqueueUtterancesAsync(speechClient, 5);
        speechClient.EnqueueTranscript("E-mail, sms, fertig.", 86);
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await RunDialogWithTimeoutAsync(dialog, contract, speechClient, tts);

        Assert.True(result.WasSubmitted);
        Assert.Equal("email|sms", result.ValuesByVariableId["contactChannels"]);

        Assert.DoesNotContain(
            telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackConstraintViolation)
                && call.StringArguments.Contains("unrecognized_input"));

        // Weder die zusätzliche Frage Übernehmen oder ändern noch die rohe Auswahlform mit | darf in TTS-Anfragen erscheinen.
        Assert.DoesNotContain(tts.Calls, call => call.Text.Contains("Übernehmen oder ändern", StringComparison.Ordinal));
        Assert.DoesNotContain(tts.Calls, call => call.Text.Contains("email|sms", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenOptionalMultiSelectCompletedEmpty_SpeaksKeineAndCommitsWithoutAbort()
    {
        // Eine leere optionale Auswahl muss übernommen werden und das Formular absendbar bleiben. Die angeforderte Rückmeldung nennt dafür das lokalisierte Wort keine.
        var contract = CreateMultiSelectContract("topics-form", "newsletterTopics", "Interessen", minSelected: 0);

        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateCatalogForContract(contract));
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            new StructuredCapturingLogger<EvaluationTelemetry>()));

        await EnqueueUtterancesAsync(speechClient, 5);
        speechClient.EnqueueTranscript("Fertig", 97);
        speechClient.EnqueueTranscript("ja", 90);
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await RunDialogWithTimeoutAsync(dialog, contract, speechClient, tts);

        Assert.True(result.WasSubmitted);
        Assert.Equal(string.Empty, result.ValuesByVariableId["newsletterTopics"]);

        Assert.Contains(tts.Calls, call => call.Text.Contains("keine", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_WhenMandatoryMultiSelectDoneOnEmpty_RefusesAndCompletesOnlyAfterASelection()
    {
        // Bei minSelected 1 darf fertig ohne Auswahl nicht genügen; erst nach einer tatsächlich genannten Option kann das Feld enden.
        var contract = CreateMultiSelectContract("channels-form", "contactChannels", "Kontaktkanäle", minSelected: 1);

        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateCatalogForContract(contract));
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            new StructuredCapturingLogger<EvaluationTelemetry>()));

        await EnqueueUtterancesAsync(speechClient, 7);
        speechClient.EnqueueTranscript("Fertig", 96);
        speechClient.EnqueueTranscript("E-Mail", 88);
        speechClient.EnqueueTranscript("fertig", 95);
        speechClient.EnqueueTranscript("absenden");
        speechClient.EnqueueTranscript("ja", 90);
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await RunDialogWithTimeoutAsync(dialog, contract, speechClient, tts);

        Assert.True(result.WasSubmitted);
        Assert.Equal("email", result.ValuesByVariableId["contactChannels"]);
        Assert.Contains(tts.Calls, call => call.Text.Contains("Noch nichts gewählt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenMultiSelectNamesOptionsThenAccepts_SelectionConfirmNamesBothAndUebernehmenCommitsOnce()
    {
        // Ohne Abschlusswort fragt die gemeinsame Auswahlbestätigung nach und nennt beide Optionen. Einmal übernehmen genügt; Absenden bleibt eine eigene Aktion.
        var contract = CreateMultiSelectContract("channels-form", "contactChannels", "Kontaktkanäle", minSelected: 1);

        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateCatalogForContract(contract));
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            new StructuredCapturingLogger<EvaluationTelemetry>()));

        await EnqueueUtterancesAsync(speechClient, 8);
        speechClient.EnqueueTranscript("E-Mail, SMS", 90);
        speechClient.EnqueueTranscript("übernehmen", 95);
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await RunDialogWithTimeoutAsync(dialog, contract, speechClient, tts);

        Assert.True(result.WasSubmitted);
        Assert.Equal("email|sms", result.ValuesByVariableId["contactChannels"]);

        Assert.Contains(tts.Calls, call => call.Text.Contains("E-Mail, SMS", StringComparison.Ordinal)
            && call.Text.Contains("Übernehmen", StringComparison.Ordinal));
        Assert.DoesNotContain(tts.Calls, call => call.Text.Contains("Übernehmen oder ändern", StringComparison.Ordinal));
        Assert.DoesNotContain(tts.Calls, call => call.Text.Contains("email|sms", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenMultiSelectExtendedAtConfirmTurn_ReConfirmsBothThenAcceptCommits()
    {
        // Eine weitere Option an der Bestätigungsfrage erweitert die Menge und muss in der erneuten Bestätigung zusammen mit der bisherigen Auswahl erscheinen.
        var contract = CreateMultiSelectContract("channels-form", "contactChannels", "Kontaktkanäle", minSelected: 1);

        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateCatalogForContract(contract));
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            new StructuredCapturingLogger<EvaluationTelemetry>()));

        await EnqueueUtterancesAsync(speechClient, 8);
        speechClient.EnqueueTranscript("E-Mail", 88);
        speechClient.EnqueueTranscript("SMS", 90);
        speechClient.EnqueueTranscript("übernehmen", 95);
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await RunDialogWithTimeoutAsync(dialog, contract, speechClient, tts);

        Assert.True(result.WasSubmitted);
        Assert.Equal("email|sms", result.ValuesByVariableId["contactChannels"]);
        Assert.Contains(tts.Calls, call => call.Text.Contains("E-Mail, SMS", StringComparison.Ordinal));
        Assert.DoesNotContain(tts.Calls, call => call.Text.Contains("Übernehmen oder ändern", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenMultiSelectFinalizedWithDoneSynonym_StillCommitsAndSkipsTheGenericConfirmation()
    {
        // fertig bestätigt die Auswahl auch in einer eigenen späteren Äußerung. Danach ist keine generische Bestätigung nötig; absenden beendet das Formular.
        var contract = CreateMultiSelectContract("channels-form", "contactChannels", "Kontaktkanäle", minSelected: 1);

        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateCatalogForContract(contract));
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            new StructuredCapturingLogger<EvaluationTelemetry>()));

        await EnqueueUtterancesAsync(speechClient, 8);
        speechClient.EnqueueTranscript("E-Mail", 88);
        speechClient.EnqueueTranscript("fertig", 95);
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await RunDialogWithTimeoutAsync(dialog, contract, speechClient, tts);

        Assert.True(result.WasSubmitted);
        Assert.Equal("email", result.ValuesByVariableId["contactChannels"]);
        Assert.DoesNotContain(tts.Calls, call => call.Text.Contains("Übernehmen oder ändern", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenMultiSelectFinalizedInlineAndAnotherFieldFollows_CommitAcknowledgmentNamesTheCommittedItems()
    {
        // Wenn nach der Auswahl ein weiteres Feld folgt, muss die Übernahmerückmeldung beide tatsächlich übernommenen Optionen benennen.
        var contract = CreateTwoMultiSelectContract("channels-form", "contactChannels", "Kontaktkanäle", "secondChannels", "Zweitkanäle");

        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateCatalogForContract(contract));
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            new StructuredCapturingLogger<EvaluationTelemetry>()));

        await EnqueueUtterancesAsync(speechClient, 10);
        speechClient.EnqueueTranscript("E-mail, sms, fertig.", 86);
        speechClient.EnqueueTranscript("E-Mail, fertig.", 90);
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await RunDialogWithTimeoutAsync(dialog, contract, speechClient, tts);

        Assert.True(result.WasSubmitted);
        Assert.Equal("email|sms", result.ValuesByVariableId["contactChannels"]);

        Assert.Contains(
            tts.Calls,
            call => call.Text.Contains("E-Mail, SMS", StringComparison.Ordinal)
                && call.Text.Contains("wurde übernommen", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenMultiSelectFinalizedPausedAndAnotherFieldFollows_CommitAcknowledgmentNamesTheCommittedItem()
    {
        // Auch ein getrennt gesprochenes fertig muss eine Rückmeldung mit dem übernommenen Wert erzeugen. wurde übernommen unterscheidet sie von der vorherigen Einladung Übernehmen.
        var contract = CreateTwoMultiSelectContract("channels-form", "contactChannels", "Kontaktkanäle", "secondChannels", "Zweitkanäle");

        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateCatalogForContract(contract));
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            new StructuredCapturingLogger<EvaluationTelemetry>()));

        await EnqueueUtterancesAsync(speechClient, 10);
        speechClient.EnqueueTranscript("E-Mail", 88);
        speechClient.EnqueueTranscript("fertig", 95);
        speechClient.EnqueueTranscript("E-Mail, fertig.", 90);
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var result = await RunDialogWithTimeoutAsync(dialog, contract, speechClient, tts);

        Assert.True(result.WasSubmitted);
        Assert.Equal("email", result.ValuesByVariableId["contactChannels"]);

        Assert.Contains(
            tts.Calls,
            call => call.Text.Contains("wurde übernommen", StringComparison.Ordinal)
                && call.Text.Contains("E-Mail", StringComparison.Ordinal));
    }

    private static async Task<VoiceCommandDialogResult> RunDialogWithTimeoutAsync(
        FormVoiceCommandDialog dialog,
        FormContract contract,
        QueuedSpeechInputClient speechClient,
        RecordingTextToSpeechPlayer tts)
    {
        var executionTask = dialog.ExecuteAsync(
            "de",
            contract.ApplicationId,
            formId: contract.FormId,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var completedTask = await Task.WhenAny(executionTask, Task.Delay(DialogExecutionTimeout));
        if (!ReferenceEquals(executionTask, completedTask))
        {
            var prompts = string.Join(" || ", tts.Calls.Select(call => call.Text));
            throw new Xunit.Sdk.XunitException(
                $"Multi-select dialog timed out. RecordCallCount={speechClient.RecordCallCount}, Prompts={prompts}");
        }

        return await executionTask;
    }

    // Ein Auswahlfeld mit drei Optionen, ohne ausdrücklich gesetzte Höchstzahl und mit der angegebenen Mindestzahl.
    private static FormContract CreateMultiSelectContract(
        string applicationId,
        string variableId,
        string fieldLabel,
        int minSelected)
    {
        using var setValueType = JsonDocument.Parse(
            "{\"kind\":\"set\",\"values\":[" +
            "{\"code\":\"email\",\"displayName\":{\"de\":\"E-Mail\"}}," +
            "{\"code\":\"phone\",\"displayName\":{\"de\":\"Telefon\"}}," +
            "{\"code\":\"sms\",\"displayName\":{\"de\":\"SMS\"}}]," +
            $"\"minSelected\":{minSelected}}}");

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = applicationId,
            FormId = FormId.From(applicationId),
            DisplayName = new LocalizedText { ["de"] = "Mehrfachauswahl" },
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From(variableId),
                    Name = variableId,
                    ValueType = setValueType.RootElement.Clone()
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From(variableId),
                    VariableId = VariableId.From(variableId),
                    Label = new LocalizedText { ["de"] = fieldLabel }
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Präferenzen" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From(variableId) }]
                }
            ]
        };
    }

    // Zwei Pflichtauswahlfelder machen nach dem ersten Abschluss die Übernahmerückmeldung beobachtbar; ein einzelnes Feld würde direkt zum Abschlussangebot führen.
    private static FormContract CreateTwoMultiSelectContract(
        string applicationId,
        string firstVariableId,
        string firstLabel,
        string secondVariableId,
        string secondLabel)
    {
        static JsonElement BuildSetValueType()
        {
            using var doc = JsonDocument.Parse(
                "{\"kind\":\"set\",\"values\":[" +
                "{\"code\":\"email\",\"displayName\":{\"de\":\"E-Mail\"}}," +
                "{\"code\":\"phone\",\"displayName\":{\"de\":\"Telefon\"}}," +
                "{\"code\":\"sms\",\"displayName\":{\"de\":\"SMS\"}}]," +
                "\"minSelected\":1}");
            return doc.RootElement.Clone();
        }

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = applicationId,
            FormId = FormId.From(applicationId),
            DisplayName = new LocalizedText { ["de"] = "Mehrfachauswahl" },
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From(firstVariableId),
                    Name = firstVariableId,
                    ValueType = BuildSetValueType()
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From(secondVariableId),
                    Name = secondVariableId,
                    ValueType = BuildSetValueType()
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From(firstVariableId),
                    VariableId = VariableId.From(firstVariableId),
                    Label = new LocalizedText { ["de"] = firstLabel }
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From(secondVariableId),
                    VariableId = VariableId.From(secondVariableId),
                    Label = new LocalizedText { ["de"] = secondLabel }
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Präferenzen" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From(firstVariableId) },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From(secondVariableId) }
                    ]
                }
            ]
        };
    }

    private async Task<FormVoiceCommandRejectThenAcceptResult> RunSingleFieldRejectThenAcceptAsync(
        FormContract contract,
        string violatingValue,
        string validValue)
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new QueuedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(CreateCatalogForContract(contract));
        var mdaFormContracts = new MapMdaFormContractDataClient([contract]);
        var telemetry = new RecordingEvaluationTelemetry(CreateTelemetry(
            new StructuredCapturingLogger<EvaluationTelemetry>()));

        await EnqueueUtterancesAsync(speechClient, 7);
        speechClient.EnqueueTranscript(violatingValue);
        speechClient.EnqueueTranscript(validValue);
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var dialog = CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var executionTask = dialog.ExecuteAsync(
            "de",
            contract.ApplicationId,
            formId: contract.FormId,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var completedTask = await Task.WhenAny(executionTask, Task.Delay(DialogExecutionTimeout));
        if (!ReferenceEquals(executionTask, completedTask))
        {
            var prompts = string.Join(" || ", tts.Calls.Select(call => call.Text));
            throw new Xunit.Sdk.XunitException(
                $"Reject-then-accept dialog timed out. RecordCallCount={speechClient.RecordCallCount}, Prompts={prompts}");
        }

        var result = await executionTask;
        return new FormVoiceCommandRejectThenAcceptResult(result, telemetry, tts);
    }

    private static void AssertConstraintViolationTrackedAndReprompted(
        FormVoiceCommandRejectThenAcceptResult result,
        string expectedErrorCode,
        string fieldId,
        string fieldLabel)
    {
        Assert.True(result.DialogResult.WasSubmitted);

        // error_code enthält die stabile Bedeutung der verletzten Bedingung, nicht die Kennung ihrer konkreten Regelinstanz.
        var violationCall = Assert.Single(
            result.Telemetry.Calls,
            call => call.Method == nameof(RecordingEvaluationTelemetry.TrackConstraintViolation)
                && call.StringArguments.Contains(expectedErrorCode));
        Assert.Contains(fieldId, violationCall.StringArguments);

        // Der erneute Feldaufruf wird an der Bezeichnung in der Orientierungszeile erkannt, nicht an einem Ansagepräfix.
        var fieldPrompts = result.TextToSpeech.Calls
            .Where(call => call.Text.Contains($"heißt {fieldLabel}.", StringComparison.Ordinal))
            .ToArray();
        Assert.True(
            fieldPrompts.Length >= 2,
            $"Expected the field to be re-prompted after the violation. Prompts={string.Join(" || ", result.TextToSpeech.Calls.Select(call => call.Text))}");
    }

    private static FormContract CreateSingleConstraintStringContract(
        string formId,
        string fieldId,
        string label,
        ConstraintDefinition constraint)
        => CreateSingleConstraintContract(formId, fieldId, label, "string", constraint);

    private static FormContract CreateSingleConstraintIntegerContract(
        string formId,
        string fieldId,
        string label,
        ConstraintDefinition constraint)
        => CreateSingleConstraintContract(formId, fieldId, label, "integer", constraint);

    private static FormContract CreateSingleConstraintContract(
        string formId,
        string fieldId,
        string label,
        string valueTypeKind,
        ConstraintDefinition constraint)
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            // Die Anwendungskennung ist hier fest zweisegmentig, weil der Vertrag sie so verlangt und
            // diese Fälle verschiedene Formularregeln innerhalb derselben Anwendung prüfen.
            ApplicationId = "sample.constraint",
            FormId = FormId.From(formId),
            DisplayName = new LocalizedText { ["de"] = label },
            DefaultLanguage = "de",
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From(fieldId),
                    Name = fieldId,
                    ValueType = CreateValueType(valueTypeKind)
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From(fieldId),
                    VariableId = VariableId.From(fieldId),
                    Label = new LocalizedText { ["de"] = label },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From(fieldId) }]
                }
            ],
            Constraints = [constraint]
        };
    }

    private static FormContract CreateCrossFieldDateOrderContract()
    {
        var dateValueType = CreateValueType("date");
        var comparison = JsonDocument.Parse(
            "{\"leftVariableId\":\"endDate\",\"rightVariableId\":\"startDate\",\"operator\":\"gte\"}").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "sample.date-order",
            FormId = FormId.From("dateOrderForm"),
            DisplayName = new LocalizedText { ["de"] = "Zeitraum" },
            DefaultLanguage = "de",
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition { VariableId = VariableId.From("startDate"), Name = "startDate", ValueType = dateValueType },
                new VariableDefinition { VariableId = VariableId.From("endDate"), Name = "endDate", ValueType = dateValueType }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("startDateField"),
                    VariableId = VariableId.From("startDate"),
                    Label = new LocalizedText { ["de"] = "Startdatum" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("endDateField"),
                    VariableId = VariableId.From("endDate"),
                    Label = new LocalizedText { ["de"] = "Enddatum" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Zeitraum" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("startDateField") },
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("endDateField") }
                    ]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "endAfterStart",
                    Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("endDateField") },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.dateOrder",
                    Message = new LocalizedText { ["de"] = "Das Enddatum muss nach dem Startdatum liegen." },
                    Remediation = new LocalizedText { ["de"] = "Bitte ein Enddatum am oder nach dem Startdatum angeben." },
                    Kind = ConstraintKind.CompareVariables,
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["leftVariableId"] = comparison.GetProperty("leftVariableId"),
                        ["rightVariableId"] = comparison.GetProperty("rightVariableId"),
                        ["operator"] = comparison.GetProperty("operator")
                    }
                }
            ]
        };
    }

}
