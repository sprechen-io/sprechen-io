using Sprechen.Speech.TestDoubles;
using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Vorschläge für zusätzliche Felder mit echtem Formularinterpreter und
/// simulierten Sprach- und Telemetriediensten. Unterscheidet Zustimmung von
/// erfolgreicher validierter Übernahme und prüft unveränderte Werte bei
/// Ablehnung, unzulässigem Ziel oder ausbleibender Entscheidung.
/// </summary>
public sealed class FormVoiceCommandAdditionalFieldProposalHandlerTests
{
    [Fact]
    public async Task ProposeAsync_WhenCandidateAccepted_AppliesValueAndRecordsAccepted()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), "de", ImmutableDictionary<string, string>.Empty);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);
        var (handler, speechService, spokenTexts) = CreateHandler(commandTelemetry.Object, "ja");
        var (turnContext, session) = CreateTurnContext(interpreter);

        await handler.ProposeAsync(turnContext, "Stadt: Berlin", CancellationToken.None);

        var values = await interpreter.GetValuesSnapshotAsync();
        Assert.Equal("Berlin", values["city"]);
        Assert.Equal("nameField", (await interpreter.GetFocusedFieldAsync()).FieldId);

        VerifyOutcome(commandTelemetry, session, TelemetryAmbiguityResults.Accepted);
        Assert.Contains(
            FormAdditionalFieldProposalPromptBuilder.GetAppliedPrompt(
                CreateContract(), "de", "Stadt", "Berlin", PolitenessLevel.Formal),
            spokenTexts);
        speechService.Verify(
            s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProposeAsync_WhenValidationRejectsAcceptedValue_LeavesFormUnchangedAndRecordsCorrected()
    {
        var contract = CreateContract(constrainCity: true);
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);
        var (handler, _, spokenTexts) = CreateHandler(commandTelemetry.Object, "ja");
        var (turnContext, session) = CreateTurnContext(interpreter, contract);

        await handler.ProposeAsync(turnContext, "Stadt: Berlin", CancellationToken.None);

        var values = await interpreter.GetValuesSnapshotAsync();
        Assert.False(values.ContainsKey("city"));
        VerifyOutcome(commandTelemetry, session, TelemetryAmbiguityResults.Corrected);

        // Die Rückmeldung soll den modellierten Grund der verletzten Bedingung
        // enthalten und nicht nur den allgemeinen Fehlertext.
        var blockedText = Assert.Single(
            spokenTexts,
            text => text.Contains("Diese Stadt ist nicht zulässig.", StringComparison.Ordinal));
        Assert.DoesNotContain(
            FormAdditionalFieldProposalPromptBuilder.GetInvalidPrompt(
                contract, "de", "Stadt", "Berlin", PolitenessLevel.Formal),
            spokenTexts);

        // Da für dieses zusätzliche Feld keine weitere Aufnahme folgt, darf der
        // Text keinen neuen Wert anfordern. Er soll die unterbliebene Übernahme benennen.
        Assert.DoesNotContain("Bitte nennen Sie einen neuen Wert.", blockedText, StringComparison.Ordinal);
        Assert.EndsWith(
            FormAdditionalFieldProposalPromptBuilder.GetUnchangedAssurance(contract, "de", PolitenessLevel.Formal),
            blockedText,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProposeAsync_WhenTargetIsProtectedAndUnavailable_NeverSpeaksTheHeardValue()
    {
        var contract = CreateContract(cityEditability: FieldEditability.ReadOnly, protectCityReadback: true);
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);
        var (handler, _, spokenTexts) = CreateHandler(commandTelemetry.Object, "ja");
        var (turnContext, session) = CreateTurnContext(interpreter, contract);

        await handler.ProposeAsync(turnContext, "Stadt: Berlin", CancellationToken.None);

        Assert.NotEmpty(spokenTexts);
        Assert.All(spokenTexts, text => Assert.DoesNotContain("Berlin", text, StringComparison.Ordinal));
        VerifyOutcome(commandTelemetry, session, TelemetryAmbiguityResults.Corrected);
    }

    [Fact]
    public async Task ProposeAsync_WhenARuleLoopBreaksInADiscardedTransaction_DoesNotSignalTheBreach()
    {
        var contract = CreateContract(constrainCity: true, addNonConvergingRule: true);
        var loopGuardFeedback = new Mock<IFormRuleLoopGuardFeedback>(MockBehavior.Loose);
        var interpreter = CreateInterpreter(contract, loopGuardFeedback.Object);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);
        var (handler, _, _) = CreateHandler(commandTelemetry.Object, "ja");
        var (turnContext, session) = CreateTurnContext(interpreter, contract);

        await handler.ProposeAsync(turnContext, "Stadt: Berlin", CancellationToken.None);

        // Die verworfene Transaktion darf ihren vorläufigen Regelzustand nicht
        // als Fehler des übernommenen Formularzustands melden.
        loopGuardFeedback.Verify(feedback => feedback.OnRuleLoopGuardBreach(), Times.Never);
        Assert.False((await interpreter.GetValuesSnapshotAsync()).ContainsKey("city"));
        VerifyOutcome(commandTelemetry, session, TelemetryAmbiguityResults.Corrected);
    }

    [Fact]
    public async Task ProposeAsync_WhenARuleLoopBreaksInAPublishedTransaction_SignalsTheBreachOnce()
    {
        var contract = CreateContract(addNonConvergingRule: true);
        var loopGuardFeedback = new Mock<IFormRuleLoopGuardFeedback>(MockBehavior.Loose);
        var interpreter = CreateInterpreter(contract, loopGuardFeedback.Object);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);
        var (handler, _, _) = CreateHandler(commandTelemetry.Object, "ja");
        var (turnContext, session) = CreateTurnContext(interpreter, contract);

        await handler.ProposeAsync(turnContext, "Stadt: Berlin", CancellationToken.None);

        Assert.Equal("Berlin", (await interpreter.GetValuesSnapshotAsync())["city"]);
        loopGuardFeedback.Verify(feedback => feedback.OnRuleLoopGuardBreach(), Times.Once);
        VerifyOutcome(commandTelemetry, session, TelemetryAmbiguityResults.Accepted);
    }

    [Fact]
    public async Task ProposeAsync_WhenTargetIsDeclaredReadOnly_DoesNotOfferAndRecordsCorrected()
    {
        var contract = CreateContract(cityEditability: FieldEditability.ReadOnly);
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);
        var (handler, speechService, spokenTexts) = CreateHandler(commandTelemetry.Object, "ja");
        var (turnContext, session) = CreateTurnContext(interpreter, contract);

        await handler.ProposeAsync(turnContext, "Stadt: Berlin", CancellationToken.None);

        var values = await interpreter.GetValuesSnapshotAsync();
        Assert.False(values.ContainsKey("city"));
        VerifyOutcome(commandTelemetry, session, TelemetryAmbiguityResults.Corrected);
        Assert.Contains(
            FormAdditionalFieldProposalPromptBuilder.GetUnavailablePrompt(
                contract, "de", "Stadt", "Berlin", PolitenessLevel.Formal),
            spokenTexts);
        speechService.Verify(
            s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProposeAsync_WhenCandidateDeclined_DiscardsValueAndRecordsCorrected()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), "de", ImmutableDictionary<string, string>.Empty);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);
        var (handler, _, _) = CreateHandler(commandTelemetry.Object, "nein");
        var (turnContext, session) = CreateTurnContext(interpreter);

        await handler.ProposeAsync(turnContext, "Stadt: Berlin", CancellationToken.None);

        var values = await interpreter.GetValuesSnapshotAsync();
        Assert.False(values.TryGetValue("city", out var cityValue) && cityValue == "Berlin");
        VerifyOutcome(commandTelemetry, session, TelemetryAmbiguityResults.Corrected);
    }

    [Fact]
    public async Task ProposeAsync_WhenNoDecisionIsHeard_ReasksOnceThenSkipsAudibly()
    {
        var contract = CreateContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);
        var (handler, speechService, spokenTexts) = CreateHandler(commandTelemetry.Object, decisionTranscript: null);
        var (turnContext, session) = CreateTurnContext(interpreter, contract);

        await handler.ProposeAsync(turnContext, "Stadt: Berlin", CancellationToken.None);

        var values = await interpreter.GetValuesSnapshotAsync();
        Assert.False(values.ContainsKey("city"));
        VerifyOutcome(commandTelemetry, session, TelemetryAmbiguityResults.Corrected);
        Assert.Contains(
            FormAdditionalFieldProposalPromptBuilder.GetSkippedPrompt(
                contract, "de", "Stadt", "Berlin", PolitenessLevel.Formal),
            spokenTexts);
        speechService.Verify(
            s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ProposeAsync_WhenTwoCandidatesAreAcceptedInSequence_AppliesBothAndKeepsFocus()
    {
        var contract = CreateContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);
        var (handler, _, spokenTexts) = CreateHandler(commandTelemetry.Object, "ja");
        var (turnContext, _) = CreateTurnContext(interpreter, contract);

        await handler.ProposeAsync(turnContext, "Stadt: Berlin", CancellationToken.None);
        await handler.ProposeAsync(turnContext, "Notiz: Park", CancellationToken.None);

        var values = await interpreter.GetValuesSnapshotAsync();
        Assert.Equal("Berlin", values["city"]);
        Assert.Equal("Park", values["note"]);
        Assert.Equal("nameField", (await interpreter.GetFocusedFieldAsync()).FieldId);
        Assert.Contains(
            FormAdditionalFieldProposalPromptBuilder.GetAppliedPrompt(
                contract, "de", "Stadt", "Berlin", PolitenessLevel.Formal),
            spokenTexts);
        Assert.Contains(
            FormAdditionalFieldProposalPromptBuilder.GetAppliedPrompt(
                contract, "de", "Notiz", "Park", PolitenessLevel.Formal),
            spokenTexts);
    }

    [Fact]
    public async Task ProposeAsync_WhenTwoCandidatesArriveInOneUtterance_FirstAcceptedSecondDeclined_AppliesOnlyTheFirst()
    {
        var contract = CreateContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);
        var (handler, _, spokenTexts) = CreateHandler(commandTelemetry.Object, ["ja", "nein"]);
        var (turnContext, session) = CreateTurnContext(interpreter, contract);

        await handler.ProposeAsync(turnContext, "Stadt: Berlin; Notiz: Park", CancellationToken.None);

        var values = await interpreter.GetValuesSnapshotAsync();
        Assert.Equal("Berlin", values["city"]);
        Assert.False(values.ContainsKey("note"));
        Assert.Equal("nameField", (await interpreter.GetFocusedFieldAsync()).FieldId);
        Assert.Contains(
            FormAdditionalFieldProposalPromptBuilder.GetAppliedPrompt(
                contract, "de", "Stadt", "Berlin", PolitenessLevel.Formal),
            spokenTexts);
        Assert.DoesNotContain(
            FormAdditionalFieldProposalPromptBuilder.GetAppliedPrompt(
                contract, "de", "Notiz", "Park", PolitenessLevel.Formal),
            spokenTexts);
        commandTelemetry.Verify(
            t => t.TrackAmbiguityResolved(
                session,
                TelemetryAmbiguityTypes.FieldNameVsContent,
                TelemetryAmbiguityResolutionStrategies.OptInProposal,
                TelemetryAmbiguitySignalClasses.Clear,
                TelemetryAmbiguityResults.Accepted,
                "cityField",
                null),
            Times.Once);
        commandTelemetry.Verify(
            t => t.TrackAmbiguityResolved(
                session,
                TelemetryAmbiguityTypes.FieldNameVsContent,
                TelemetryAmbiguityResolutionStrategies.OptInProposal,
                TelemetryAmbiguitySignalClasses.Clear,
                TelemetryAmbiguityResults.Corrected,
                "noteField",
                null),
            Times.Once);
    }

    [Fact]
    public async Task ProposeAsync_WhenOkayAlexCancelIsHeard_DoesNotApplyAndStopsRemainingCandidates()
    {
        var contract = CreateContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);
        var (handler, speechService, spokenTexts) = CreateHandler(commandTelemetry.Object, "Okay Alex abbrechen");
        var (turnContext, session) = CreateTurnContext(interpreter, contract);

        await handler.ProposeAsync(turnContext, "Stadt: Berlin; Notiz: Park", CancellationToken.None);

        var values = await interpreter.GetValuesSnapshotAsync();
        Assert.False(values.ContainsKey("city"));
        Assert.False(values.ContainsKey("note"));
        Assert.Equal("nameField", (await interpreter.GetFocusedFieldAsync()).FieldId);
        VerifyOutcome(commandTelemetry, session, TelemetryAmbiguityResults.Corrected);
        Assert.Contains(
            FormAdditionalFieldProposalPromptBuilder.GetSkippedPrompt(
                contract, "de", "Stadt", "Berlin", PolitenessLevel.Formal),
            spokenTexts);
        speechService.Verify(
            s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProposeAsync_WhenOkayLeadInCancelIsHeard_DoesNotApplyAndStopsRemainingCandidates()
    {
        var contract = CreateContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);
        var (handler, speechService, spokenTexts) = CreateHandler(commandTelemetry.Object, "Okay abbrechen");
        var (turnContext, session) = CreateTurnContext(interpreter, contract);

        await handler.ProposeAsync(turnContext, "Stadt: Berlin; Notiz: Park", CancellationToken.None);

        var values = await interpreter.GetValuesSnapshotAsync();
        Assert.False(values.ContainsKey("city"));
        Assert.False(values.ContainsKey("note"));
        Assert.Equal("nameField", (await interpreter.GetFocusedFieldAsync()).FieldId);
        VerifyOutcome(commandTelemetry, session, TelemetryAmbiguityResults.Corrected);
        Assert.Contains(
            FormAdditionalFieldProposalPromptBuilder.GetSkippedPrompt(
                contract, "de", "Stadt", "Berlin", PolitenessLevel.Formal),
            spokenTexts);
        speechService.Verify(
            s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProposeAsync_WhenTwoCandidatesArriveInOneUtterance_BothAccepted_AppliesBothAgainstSuccessiveRevisions()
    {
        var contract = CreateContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);
        var (handler, _, spokenTexts) = CreateHandler(commandTelemetry.Object, "ja");
        var (turnContext, session) = CreateTurnContext(interpreter, contract);
        var revisionBefore = interpreter.MutableState.StateRevision;

        await handler.ProposeAsync(turnContext, "Stadt: Berlin; Notiz: Park", CancellationToken.None);

        var values = await interpreter.GetValuesSnapshotAsync();
        Assert.Equal("Berlin", values["city"]);
        Assert.Equal("Park", values["note"]);
        Assert.Equal(revisionBefore + 2, interpreter.MutableState.StateRevision);
        Assert.Equal("nameField", (await interpreter.GetFocusedFieldAsync()).FieldId);
        Assert.Contains(
            FormAdditionalFieldProposalPromptBuilder.GetAppliedPrompt(
                contract, "de", "Stadt", "Berlin", PolitenessLevel.Formal),
            spokenTexts);
        Assert.Contains(
            FormAdditionalFieldProposalPromptBuilder.GetAppliedPrompt(
                contract, "de", "Notiz", "Park", PolitenessLevel.Formal),
            spokenTexts);
        commandTelemetry.Verify(
            t => t.TrackAmbiguityResolved(
                session,
                TelemetryAmbiguityTypes.FieldNameVsContent,
                TelemetryAmbiguityResolutionStrategies.OptInProposal,
                TelemetryAmbiguitySignalClasses.Clear,
                TelemetryAmbiguityResults.Accepted,
                "cityField",
                null),
            Times.Once);
        commandTelemetry.Verify(
            t => t.TrackAmbiguityResolved(
                session,
                TelemetryAmbiguityTypes.FieldNameVsContent,
                TelemetryAmbiguityResolutionStrategies.OptInProposal,
                TelemetryAmbiguitySignalClasses.Clear,
                TelemetryAmbiguityResults.Accepted,
                "noteField",
                null),
            Times.Once);
    }

    [Fact]
    public async Task ProposeAsync_WhenNoExtraFieldMention_IsNoOpWithoutProposalOrTelemetry()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), "de", ImmutableDictionary<string, string>.Empty);
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Strict);
        var (handler, speechService, _) = CreateHandler(commandTelemetry.Object, "ja");
        var (turnContext, _) = CreateTurnContext(interpreter);

        await handler.ProposeAsync(turnContext, "Max Mustermann", CancellationToken.None);

        speechService.Verify(
            s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        commandTelemetry.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProposeAsync_WhenFocusedValueContainsLabelLikeWord_IsNoOp()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), "de", ImmutableDictionary<string, string>.Empty);
        Assert.True(await interpreter.TrySetFocusToFieldIdAsync("noteField"));
        var commandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Strict);
        var (handler, speechService, _) = CreateHandler(commandTelemetry.Object, "ja");
        var (turnContext, _) = CreateTurnContext(interpreter, focusedFieldId: "noteField");

        await handler.ProposeAsync(turnContext, "Stadt Park ist schoen", CancellationToken.None);

        speechService.Verify(
            s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        commandTelemetry.VerifyNoOtherCalls();
    }

    private static FormInterpreterRuntimeOwner CreateInterpreter(
        FormContract contract,
        IFormRuleLoopGuardFeedback loopGuardFeedback)
        => new(
            contract,
            "de",
            ImmutableDictionary<string, string>.Empty,
            new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()),
            PolitenessLevel.Formal,
            sessionId: null,
            loopGuardFeedback);

    private static void VerifyOutcome(
        Mock<IEvaluationCommandTelemetry> commandTelemetry,
        EvaluationSession session,
        string expectedResult)
        => commandTelemetry.Verify(
            t => t.TrackAmbiguityResolved(
                session,
                TelemetryAmbiguityTypes.FieldNameVsContent,
                TelemetryAmbiguityResolutionStrategies.OptInProposal,
                TelemetryAmbiguitySignalClasses.Clear,
                expectedResult,
                "cityField",
                null),
            Times.Once);

    private static (
        FormVoiceCommandAdditionalFieldProposalHandler Handler,
        Mock<IFormVoiceCommandSpeechService> SpeechService,
        List<string> SpokenTexts) CreateHandler(
        IEvaluationCommandTelemetry commandTelemetry,
        string? decisionTranscript)
        => CreateHandler(commandTelemetry, [decisionTranscript]);

    private static (
        FormVoiceCommandAdditionalFieldProposalHandler Handler,
        Mock<IFormVoiceCommandSpeechService> SpeechService,
        List<string> SpokenTexts) CreateHandler(
        IEvaluationCommandTelemetry commandTelemetry,
        IReadOnlyList<string?> decisionTranscripts)
    {
        var remaining = new Queue<string?>(decisionTranscripts);
        var spokenTexts = new List<string>();
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
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
                    onStartListening();
                    var transcript = remaining.Count > 1
                        ? remaining.Dequeue()
                        : remaining.Count == 1
                            ? remaining.Peek()
                            : null;
                    return Task.FromResult(new SpeechCaptureResult(transcript, null));
                });
        speechService
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>(
                (text, _, _, _) =>
                {
                    spokenTexts.Add(text);
                    return Task.CompletedTask;
                });

        var handler = new FormVoiceCommandAdditionalFieldProposalHandler(
            commandTelemetry,
            new FormVoiceCommandConfirmationTranscriptCapture(speechService.Object),
            new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()),
            speechService.Object);

        return (handler, speechService, spokenTexts);
    }

    private static (FormVoiceCommandFieldTurnContext TurnContext, EvaluationSession Session) CreateTurnContext(
        IFormDialogRuntime interpreter,
        FormContract? contract = null,
        string focusedFieldId = "nameField")
    {
        var resolvedContract = contract ?? CreateContract();
        var sessionTelemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var session = new EvaluationSession("additional-field-session", EvaluationChannel.Voice, "app", "app/additionalField");
        var stateTracker = new FormVoiceCommandStateTracker(sessionTelemetry.Object, session);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings { MaxRetries = 0 },
            Mock.Of<IFormVoiceCommandPromptService>(),
            stateTracker,
            interpreter,
            isoCode: "de");

        var field = new FormFieldSnapshot(
            Identity: new FormFieldIdentitySnapshot(
                FieldId: focusedFieldId,
                VariableId: focusedFieldId == "noteField" ? "note" : "name",
                FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Name",
                HelpText: null,
                Examples: [],
                Options: []),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));

        var turnContext = new FormVoiceCommandFieldTurnContext(
            CreateContext(resolvedContract),
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            interpreter as IFormDialogPrivacyReadbackPolicy ?? Mock.Of<IFormDialogPrivacyReadbackPolicy>());

        return (turnContext, session);
    }

    private static FormVoiceCommandContext CreateContext(FormContract contract)
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("additionalField"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            Description = new LocalizedText { ["de"] = "Formular" },
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

        return new FormVoiceCommandContext(
            new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "additionalField",
                WakeWordActivationForms: ["okay", "okay alex"]),
            new FormVoiceCommandSelectionContext(
                new VoiceDialogSettings(),
                application,
                ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                form,
                contract),
            new FormVoiceCommandValueSnapshot(
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                []));
    }

    private static FormContract CreateContract(
        FieldEditability cityEditability = FieldEditability.UserEditable,
        bool constrainCity = false,
        bool protectCityReadback = false,
        bool addNonConvergingRule = false)
    {
        var stringValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement;
        var booleanValueType = JsonDocument.Parse("{\"kind\":\"boolean\"}").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("additionalField"),
            Rules = addNonConvergingRule
                ?
                [
                    new RuleDefinition
                    {
                        RuleId = "toggleFlag",
                        Scope = RuleScopes.OnValueChanged,
                        When = JsonDocument.Parse("{\"kind\":\"equals\",\"left\":{\"kind\":\"var\",\"variableId\":\"flag\"},\"right\":{\"kind\":\"const\",\"value\":true}}").RootElement,
                        Then = [JsonDocument.Parse("{\"kind\":\"setValue\",\"variableId\":\"flag\",\"value\":false}").RootElement],
                        Else = [JsonDocument.Parse("{\"kind\":\"setValue\",\"variableId\":\"flag\",\"value\":true}").RootElement]
                    }
                ]
                : [],
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition { VariableId = VariableId.From("name"), Name = "name", ValueType = stringValueType },
                new VariableDefinition
                {
                    VariableId = VariableId.From("city"),
                    Name = "city",
                    ValueType = stringValueType,
                    PrivacyClass = protectCityReadback ? PrivacyClass.Sensitive : null,
                    SensitiveDataPolicy = protectCityReadback
                        ? new SensitiveDataPolicy { ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackNever }
                        : null
                },
                new VariableDefinition { VariableId = VariableId.From("note"), Name = "note", ValueType = stringValueType },
                new VariableDefinition { VariableId = VariableId.From("flag"), Name = "flag", ValueType = booleanValueType }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("nameField"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("cityField"),
                    VariableId = VariableId.From("city"),
                    Label = new LocalizedText { ["de"] = "Stadt" },
                    Editability = cityEditability
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("noteField"),
                    VariableId = VariableId.From("note"),
                    Label = new LocalizedText { ["de"] = "Notiz" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("nameField") },
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("cityField") },
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("noteField") }
                    ]
                }
            ],
            Constraints = constrainCity
                ?
                [
                    new ConstraintDefinition
                    {
                        ConstraintId = "cityOneOf",
                        Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("cityField") },
                        Severity = ConstraintSeverity.Error,
                        Code = "form.oneOf.invalid",
                        Message = new LocalizedText { ["de"] = "Diese Stadt ist nicht zulässig." },
                        Kind = ConstraintKind.OneOf,
                        AdditionalProperties = new Dictionary<string, JsonElement>
                        {
                            ["values"] = JsonDocument.Parse("[\"Hamburg\"]").RootElement
                        }
                    }
                ]
                : []
        };
    }
}
