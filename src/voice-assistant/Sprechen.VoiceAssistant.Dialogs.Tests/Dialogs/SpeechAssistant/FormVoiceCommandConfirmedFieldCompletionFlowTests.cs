using System.Collections.Immutable;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.TestDoubles;
using Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandConfirmedFieldCompletionFlowTests
{
    [Fact]
    public async Task AutoAdvanceOn_Commit_SpeaksCommitAccepted_AndPlaysValidationSuccess()
    {
        // Prüft den gemeinsamen Abschlussaufruf bei standardmäßig aktivem
        // Weiterrücken. Die vorgelagerte Zuordnung verschiedener Antworten wird hier nicht geprüft.
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        string? spokenText = null;
        var speechService = CreateSpeakingSpeechService(text => spokenText = text);
        var earconPlayer = CreateEarconPlayer();

        var field = CreateFieldSnapshot();
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter.Setup(i => i.TryMoveFocusNextAsync()).ReturnsAsync(true);
        interpreter.Setup(i => i.GetFocusedFieldAsync()).ReturnsAsync(field);

        var flow = new FormVoiceCommandConfirmedFieldCompletionFlow(
                speechService.Object,
                earconPlayer.Object,
                CreateNoopClosingFlow());
        var turnContext = CreateTurnContext(new VoiceDialogSettings(), interpreter.Object, field, out _);

        var result = await flow.CompleteAsync(turnContext, CancellationToken.None);

        Assert.True(result.ShouldContinue);
        interpreter.Verify(i => i.TryMoveFocusNextAsync(), Times.Once);
        speechService.Verify(
            s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(FormDialogFlowPromptTextResolver.GetCommitAcceptedPrompt("de"), spokenText);
        Assert.NotEqual(FormDialogFlowPromptTextResolver.GetAutoAdvanceManualContinuationPrompt("de"), spokenText);
        earconPlayer.Verify(e => e.PlayAsync(EarconId.ValidationSuccess, It.IsAny<CancellationToken>()), Times.Once);
        submissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AutoAdvanceOn_WhenAnswerableTotalWalksFromSevenToEight_SpeaksChangeAfterCommit_NotASecondCountSentence()
    {
        // Die Feldanzahl wird von sieben auf acht simuliert. Der Abschluss soll
        // die Änderung melden; die nächste Positionsansage gehört zum Feldeinstieg.
        var spoken = new List<string>();
        var speechService = CreateSpeakingSpeechService(text => spoken.Add(text));
        var earconPlayer = CreateEarconPlayer();
        var field = CreateFieldSnapshot();
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter.Setup(i => i.TryMoveFocusNextAsync()).ReturnsAsync(true);
        interpreter.Setup(i => i.GetFocusedFieldAsync()).ReturnsAsync(field);
        interpreter.Setup(i => i.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(allTotal: 8, fieldIndex: 8));

        var flow = new FormVoiceCommandConfirmedFieldCompletionFlow(
            speechService.Object,
            earconPlayer.Object,
            CreateNoopClosingFlow());
        var turnContext = CreateTurnContext(new VoiceDialogSettings(), interpreter.Object, field, out var stateTracker);
        stateTracker.RecordFieldEntryAnswerableTotal(7);

        var result = await flow.CompleteAsync(turnContext, CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.Equal(
            [
                FormDialogFlowPromptTextResolver.GetCommitAcceptedPrompt("de"),
                FormDialogFlowPromptTextResolver.GetProgressAnswerableTotalChangedPrompt("de", 7, 8)
            ],
            spoken);
        Assert.DoesNotContain("Eingabefelder abgeschlossen", string.Join(" ", spoken), StringComparison.Ordinal);
        Assert.DoesNotContain("Feld 8 von 8", string.Join(" ", spoken), StringComparison.Ordinal);
        interpreter.Verify(i => i.GetStatusSnapshotAsync(), Times.Once);
    }

    [Fact]
    public async Task AutoAdvanceOn_WhenAnswerableTotalWalksFromEightToSeven_WithoutNextField_SpeaksChangeWithBothNumbers()
    {
        var (closingFlow, _, router, earconPlayer) = CreateClosingFlow("absenden");
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var spoken = new List<string>();
        var completionSpeech = CreateSpeakingSpeechService(text => spoken.Add(text));
        var field = CreateFieldSnapshot();
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter.Setup(i => i.TryMoveFocusNextAsync()).ReturnsAsync(false);
        interpreter.Setup(i => i.GetFocusedFieldIdAsync()).ReturnsAsync(field.FieldId);
        interpreter.Setup(i => i.GetFocusedFieldAsync()).ReturnsAsync(field);
        interpreter.Setup(i => i.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(allTotal: 7, fieldIndex: 7));

        var flow = new FormVoiceCommandConfirmedFieldCompletionFlow(
            completionSpeech.Object,
            earconPlayer.Object,
            closingFlow);
        var turnContext = CreateTurnContext(new VoiceDialogSettings(), interpreter.Object, field, out var stateTracker);
        stateTracker.RecordFieldEntryAnswerableTotal(8);

        await flow.CompleteAsync(turnContext, CancellationToken.None);

        Assert.Equal(
            [FormDialogFlowPromptTextResolver.GetProgressAnswerableTotalChangedPrompt("de", 8, 7)],
            spoken);
    }

    [Fact]
    public async Task AutoAdvanceOn_WhenAnswerableTotalStays_DoesNotSpeakAChange()
    {
        var spoken = new List<string>();
        var speechService = CreateSpeakingSpeechService(text => spoken.Add(text));
        var earconPlayer = CreateEarconPlayer();
        var field = CreateFieldSnapshot();
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter.Setup(i => i.TryMoveFocusNextAsync()).ReturnsAsync(true);
        interpreter.Setup(i => i.GetFocusedFieldAsync()).ReturnsAsync(field);
        interpreter.Setup(i => i.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(allTotal: 7, fieldIndex: 3));

        var flow = new FormVoiceCommandConfirmedFieldCompletionFlow(
            speechService.Object,
            earconPlayer.Object,
            CreateNoopClosingFlow());
        var turnContext = CreateTurnContext(new VoiceDialogSettings(), interpreter.Object, field, out var stateTracker);
        stateTracker.RecordFieldEntryAnswerableTotal(7);

        await flow.CompleteAsync(turnContext, CancellationToken.None);

        Assert.Equal([FormDialogFlowPromptTextResolver.GetCommitAcceptedPrompt("de")], spoken);
    }

    [Fact]
    public async Task AutoAdvanceOn_CommitWithCommittedValueReadback_SpeaksAcknowledgmentThatNamesTheValue()
    {
        // Wird ein freigegebener Wert zum Vorlesen übergeben, soll die
        // Übernahmebestätigung diesen Wert nennen.
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        string? spokenText = null;
        var speechService = CreateSpeakingSpeechService(text => spokenText = text);
        var earconPlayer = CreateEarconPlayer();

        var field = CreateFieldSnapshot();
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter.Setup(i => i.TryMoveFocusNextAsync()).ReturnsAsync(true);
        interpreter.Setup(i => i.GetFocusedFieldAsync()).ReturnsAsync(field);

        var flow = new FormVoiceCommandConfirmedFieldCompletionFlow(
                speechService.Object,
                earconPlayer.Object,
                CreateNoopClosingFlow());
        var turnContext = CreateTurnContext(new VoiceDialogSettings(), interpreter.Object, field, out _);

        var committedValueReadback = new FormVoiceCommandUnderstoodValue(
            "E-Mail, SMS",
            IncludesValue: true,
            FormSpokenValueProjectionStatus.FullValue);
        var result = await flow.CompleteAsync(turnContext, committedValueReadback, CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.Equal(
            FormConfirmationPromptBuilder.GetCommitAcceptedNamingValuePrompt("de", field.Label, "E-Mail, SMS"),
            spokenText);
        Assert.Contains("E-Mail, SMS", spokenText!, StringComparison.Ordinal);
        Assert.NotEqual(FormDialogFlowPromptTextResolver.GetCommitAcceptedPrompt("de"), spokenText);
        earconPlayer.Verify(e => e.PlayAsync(EarconId.ValidationSuccess, It.IsAny<CancellationToken>()), Times.Once);
        submissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AutoAdvanceOn_CommitWithoutValueReadback_SpeaksThePlainAcknowledgment()
    {
        // Ohne übergebenen Wert zum Vorlesen genügt die allgemeine
        // Übernahmebestätigung. Vorangegangene Ansagen werden hier nicht geprüft.
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        string? spokenText = null;
        var speechService = CreateSpeakingSpeechService(text => spokenText = text);
        var earconPlayer = CreateEarconPlayer();

        var field = CreateFieldSnapshot();
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter.Setup(i => i.TryMoveFocusNextAsync()).ReturnsAsync(true);
        interpreter.Setup(i => i.GetFocusedFieldAsync()).ReturnsAsync(field);

        var flow = new FormVoiceCommandConfirmedFieldCompletionFlow(
                speechService.Object,
                earconPlayer.Object,
                CreateNoopClosingFlow());
        var turnContext = CreateTurnContext(new VoiceDialogSettings(), interpreter.Object, field, out _);

        var result = await flow.CompleteAsync(turnContext, FormVoiceCommandUnderstoodValue.WithoutValue, CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.Equal(FormDialogFlowPromptTextResolver.GetCommitAcceptedPrompt("de"), spokenText);
        submissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AutoAdvance_PlaysNoStructureEarcon()
    {
        // Automatisches Weiterrücken darf weder einen Strukturwechselton
        // noch einen Gruppengrenzton anfordern.

        // Wechsel innerhalb derselben Feldgruppe.
        {
            var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
            var speechService = CreateSpeakingSpeechService(_ => { });
            var earconPlayer = CreateEarconPlayer();

            var currentField = CreateFieldSnapshot();
            var nextSameGroupField = CreateFieldSnapshot();
            var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
            interpreter.Setup(i => i.TryMoveFocusNextAsync()).ReturnsAsync(true);
            interpreter.Setup(i => i.GetFocusedFieldAsync()).ReturnsAsync(nextSameGroupField);

            var flow = new FormVoiceCommandConfirmedFieldCompletionFlow(
                speechService.Object,
                earconPlayer.Object,
                CreateNoopClosingFlow());
            var turnContext = CreateTurnContext(new VoiceDialogSettings(), interpreter.Object, currentField, out _);

            await flow.CompleteAsync(turnContext, CancellationToken.None);

            earconPlayer.Verify(e => e.PlayAsync(EarconId.StructureStepTransition, It.IsAny<CancellationToken>()), Times.Never);
            earconPlayer.Verify(e => e.PlayAsync(EarconId.StructureBoundary, It.IsAny<CancellationToken>()), Times.Never);
        }

        // Wechsel in eine andere Feldgruppe.
        {
            var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
            var speechService = CreateSpeakingSpeechService(_ => { });
            var earconPlayer = CreateEarconPlayer();

            var currentField = CreateFieldSnapshot();
            var nextOtherGroupField = CreateFieldSnapshot(fieldGroupId: "contact");
            var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
            interpreter.Setup(i => i.TryMoveFocusNextAsync()).ReturnsAsync(true);
            interpreter.Setup(i => i.GetFocusedFieldAsync()).ReturnsAsync(nextOtherGroupField);

            var flow = new FormVoiceCommandConfirmedFieldCompletionFlow(
                speechService.Object,
                earconPlayer.Object,
                CreateNoopClosingFlow());
            var turnContext = CreateTurnContext(new VoiceDialogSettings(), interpreter.Object, currentField, out _);

            await flow.CompleteAsync(turnContext, CancellationToken.None);

            earconPlayer.Verify(e => e.PlayAsync(EarconId.StructureBoundary, It.IsAny<CancellationToken>()), Times.Never);
            earconPlayer.Verify(e => e.PlayAsync(EarconId.StructureStepTransition, It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task LastFieldComplete_DoesNotAutoSubmit_RunsClosingTurn_AndSubmitsOnlyOnExplicitSubmit()
    {
        // Nach dem letzten Feld den Abschlussdialog aufrufen. Die simulierte
        // Antwort Absenden wird an den Router übergeben; echte Speicherung
        // ist nicht Bestandteil dieses Tests.
        var (closingFlow, closingSpeech, router, earconPlayer) = CreateClosingFlow("absenden");
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var completionSpeech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var field = CreateFieldSnapshot();
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter.Setup(i => i.TryMoveFocusNextAsync()).ReturnsAsync(false);
        // Der Abschlussdialog benötigt die Kennung des zuletzt bearbeiteten Felds.
        interpreter.Setup(i => i.GetFocusedFieldIdAsync()).ReturnsAsync(field.FieldId);
        interpreter.Setup(i => i.GetFocusedFieldAsync()).ReturnsAsync(field);

        var flow = new FormVoiceCommandConfirmedFieldCompletionFlow(
            completionSpeech.Object,
            earconPlayer.Object,
            closingFlow);
        var turnContext = CreateTurnContext(new VoiceDialogSettings(), interpreter.Object, field, out _);

        var result = await flow.CompleteAsync(turnContext, CancellationToken.None);

        Assert.Equal(EvaluationOutcome.Submitted, result.DialogResult?.Outcome);
        interpreter.Verify(i => i.TryMoveFocusNextAsync(), Times.Once);
        // Die Abschlussfrage muss an die Aufnahme übergeben werden.
        closingSpeech.Verify(
            s => s.CaptureTranscriptAsync(
                FormDialogFlowPromptTextResolver.GetFormClosingReviewPrompt("de", PolitenessLevel.Formal),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                RecognitionProfile.Conversational,
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        router.Verify(
            r => r.TryHandleAsync(It.Is<CommandId>(c => c.Value == "submit"), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
        // Hier noch keinen endgültigen Abschlusston anfordern. Die dauerhafte
        // Annahme der Telemetrie wird im übergeordneten Sitzungsabschluss behandelt.
        earconPlayer.Verify(e => e.PlayAsync(EarconId.FlowDone, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LastFieldComplete_WithCommittedValueReadback_PrefixesFirstClosingOffer()
    {
        var (closingFlow, closingSpeech, router, earconPlayer) = CreateClosingFlow("absenden");
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var completionSpeech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var field = CreateFieldSnapshot(label: "Kontaktkanäle");
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter.Setup(i => i.TryMoveFocusNextAsync()).ReturnsAsync(false);
        interpreter.Setup(i => i.GetFocusedFieldIdAsync()).ReturnsAsync(field.FieldId);
        interpreter.Setup(i => i.GetFocusedFieldAsync()).ReturnsAsync(field);

        var flow = new FormVoiceCommandConfirmedFieldCompletionFlow(
            completionSpeech.Object,
            earconPlayer.Object,
            closingFlow);
        var turnContext = CreateTurnContext(new VoiceDialogSettings(), interpreter.Object, field, out _);
        var committedValueReadback = new FormVoiceCommandUnderstoodValue(
            "E-Mail, SMS",
            IncludesValue: true,
            FormSpokenValueProjectionStatus.FullValue);
        var expectedReadbackPrompt = FormConfirmationPromptBuilder.GetCommitAcceptedNamingValuePrompt(
            "de",
            field.Label,
            "E-Mail, SMS");
        var closingPrompt = FormDialogFlowPromptTextResolver.GetFormClosingReviewPrompt("de", PolitenessLevel.Formal);

        var result = await flow.CompleteAsync(turnContext, committedValueReadback, CancellationToken.None);

        Assert.Equal(EvaluationOutcome.Submitted, result.DialogResult?.Outcome);
        closingSpeech.Verify(
            s => s.CaptureTranscriptAsync(
                string.Concat(expectedReadbackPrompt, " ", closingPrompt),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                RecognitionProfile.Conversational,
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        closingSpeech.Verify(
            s => s.CaptureTranscriptAsync(
                closingPrompt,
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                RecognitionProfile.Conversational,
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        earconPlayer.Verify(e => e.PlayAsync(EarconId.FlowDone, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AutoAdvanceOff_SpeaksManualContinuationOnly_NoCommitAck_NoChime()
    {
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        string? spokenText = null;
        var speechService = CreateSpeakingSpeechService(text => spokenText = text);
        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);

        var field = CreateFieldSnapshot();
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);

        var flow = new FormVoiceCommandConfirmedFieldCompletionFlow(
                speechService.Object,
                earconPlayer.Object,
                CreateNoopClosingFlow());
        var turnContext = CreateTurnContext(
            new VoiceDialogSettings { AutoAdvanceAfterCommit = false },
            interpreter.Object,
            field,
            out _);

        var result = await flow.CompleteAsync(turnContext, CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        interpreter.Verify(i => i.TryMoveFocusNextAsync(), Times.Never);
        submissionService.VerifyNoOtherCalls();
        speechService.Verify(
            s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(FormDialogFlowPromptTextResolver.GetAutoAdvanceManualContinuationPrompt("de"), spokenText);
        Assert.NotEqual(FormDialogFlowPromptTextResolver.GetCommitAcceptedPrompt("de"), spokenText);
        earconPlayer.Verify(e => e.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadedDeviceOverride_WhenAutoAdvanceOff_FlowDoesNotAutoAdvance()
    {
        // Nur der simulierte Einstellungsspeicher liefert die geräteweite
        // Abschaltung; im Katalog bleibt automatisches Weiterrücken ungesetzt.
        // Die geladenen Einstellungen müssen diese Vorgabe übernehmen.
        var overrideStore = new Mock<IVoiceDialogSettingsOverrideStore>(MockBehavior.Loose);
        overrideStore
            .Setup(s => s.GetApplicationAllowBargeInOverrideAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)null);
        overrideStore
            .Setup(s => s.GetDeviceAllowBargeInOverrideAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)null);
        overrideStore
            .Setup(s => s.GetApplicationAutoAdvanceAfterCommitOverrideAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)null);
        overrideStore
            .Setup(s => s.GetDeviceAutoAdvanceAfterCommitOverrideAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)false);

        var loader = new FormVoiceCommandCatalogSnapshotLoader(
            new FixedFormsCatalogDataClient(CreateCatalog()),
            overrideStore.Object,
            NullLogger<FormVoiceCommandCatalogSnapshotLoader>.Instance);

        var snapshot = await loader.LoadAsync("app", "current", CancellationToken.None);

        Assert.NotNull(snapshot);
        Assert.False(snapshot!.BaseVoiceSettings.GetAutoAdvanceAfterCommit());

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var speechService = CreateSpeakingSpeechService(_ => { });
        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);

        var field = CreateFieldSnapshot();
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        var flow = new FormVoiceCommandConfirmedFieldCompletionFlow(
                speechService.Object,
                earconPlayer.Object,
                CreateNoopClosingFlow());
        var turnContext = CreateTurnContext(snapshot.BaseVoiceSettings, interpreter.Object, field, out _);

        var result = await flow.CompleteAsync(turnContext, CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        interpreter.Verify(i => i.TryMoveFocusNextAsync(), Times.Never);
        speechService.Verify(
            s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()),
            Times.Once);
        earconPlayer.Verify(e => e.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()), Times.Never);
        submissionService.VerifyNoOtherCalls();
    }

    private static FormClosingReviewTurnFlow CreateNoopClosingFlow()
        => new(
            new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose).Object,
            new FormVoiceCommandGlobalCommandExecutionService(
                Mock.Of<IEvaluationCommandTelemetry>(),
                Mock.Of<IFormVoiceCommandGlobalCommandRouter>()));

    private static (
        FormClosingReviewTurnFlow Flow,
        Mock<IFormVoiceCommandSpeechService> Speech,
        Mock<IFormVoiceCommandGlobalCommandRouter> Router,
        Mock<IEarconPlayer> Earcon) CreateClosingFlow(params string?[] transcripts)
    {
        var queue = new Queue<string?>(transcripts);
        var speech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speech
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                (_, _, _, onStartListening, _, _, _, _) =>
                {
                    onStartListening?.Invoke();
                    var transcript = queue.Count > 0 ? queue.Dequeue() : null;
                    if (string.Equals(transcript, "<silence>", StringComparison.Ordinal))
                    {
                        throw new NoSpeechDetectedException();
                    }

                    return Task.FromResult(new SpeechCaptureResult(transcript, null));
                });
        speech
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Loose);
        var earcon = CreateEarconPlayer();
        var flow = new FormClosingReviewTurnFlow(
            speech.Object,
            new FormVoiceCommandGlobalCommandExecutionService(Mock.Of<IEvaluationCommandTelemetry>(), router.Object));
        return (flow, speech, router, earcon);
    }

    private static Mock<IFormVoiceCommandSpeechService> CreateSpeakingSpeechService(Action<string> onSpoken)
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) =>
            {
                onSpoken(text);
                return Task.CompletedTask;
            });
        return speechService;
    }

    private static Mock<IEarconPlayer> CreateEarconPlayer()
    {
        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(e => e.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        return earconPlayer;
    }

    private static FormVoiceCommandFieldTurnContext CreateTurnContext(
        VoiceDialogSettings voiceSettings,
        IFormDialogRuntime interpreter,
        FormFieldSnapshot field,
        out FormVoiceCommandStateTracker stateTracker)
    {
        var session = new EvaluationSession("session-completion", EvaluationChannel.Voice, "app", "app/current");
        var context = CreateContext(voiceSettings);
        stateTracker = new FormVoiceCommandStateTracker(Mock.Of<IEvaluationTelemetry>(), session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();
        stateTracker.BeginInterpretation();
        stateTracker.BeginValidation();
        stateTracker.BeginFieldConfirmation();

        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            voiceSettings,
            Mock.Of<IFormVoiceCommandPromptService>(),
            Mock.Of<IFormVoiceCommandDialogStateTracker>(),
            interpreter,
            isoCode: "de");

        return new FormVoiceCommandFieldTurnContext(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            interpreter as IFormDialogPrivacyReadbackPolicy ?? Mock.Of<IFormDialogPrivacyReadbackPolicy>());
    }

    private static FormStatusSnapshot CreateStatusSnapshot(int allTotal, int fieldIndex)
        => new(
            Navigation: new FormStatusNavigationPosition(
                CurrentFormId: null,
                CurrentFormIndex: null,
                TotalForms: null,
                CurrentFieldGroupId: "main",
                CurrentFieldGroupIndex: 1,
                TotalFieldGroups: 8,
                CurrentFieldId: "name"),
            Progress: new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(0, allTotal),
                RequiredFieldsOnly: new FormStatusProgressDimension(0, 0),
                FieldGroupBased: new FormStatusProgressDimension(0, 0)),
            CurrentFieldLabel: "Name",
            CurrentFieldIndex: fieldIndex,
            CurrentFieldGroupTitle: "Main",
            OpenRequiredFieldCount: 0,
            ValidationErrorCount: 0);

    private static FormFieldSnapshot CreateFieldSnapshot(string fieldGroupId = "main", string label = "Name")
        => new(
            Identity: new FormFieldIdentitySnapshot(
                FieldId: "name",
                VariableId: "name",
                FieldGroupId: fieldGroupId),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: label,
                HelpText: null,
                Examples: [],
                Options: []),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));

    private static FormVoiceCommandContext CreateContext(VoiceDialogSettings voiceSettings)
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            Description = new LocalizedText { ["de"] = "Teil 1" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };

        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables = [],
            Fields = [],
            FieldGroups = []
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: []),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: voiceSettings,
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }

    private static FormsCatalog CreateCatalog()
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            Description = new LocalizedText { ["de"] = "Teil 1" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "app",
                    DisplayName = new LocalizedText { ["de"] = "App" },
                    Description = new LocalizedText { ["de"] = "App" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms = [form]
                }
            ]
        };
    }
}
