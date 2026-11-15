using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Bestätigungsnachfrage über den echten Sprachdienst mit Aufnahme-
/// und Sprachausgabedoubles. Nach simulierter Stille soll genau eine Nachfrage
/// mit dem Wert folgen; die anschließende Antwort nimmt ihn an. Eine Meldung
/// über nicht verstandene Eingabe darf dabei nicht ausgegeben werden.
/// </summary>
public sealed class FormVoiceCommandConfirmationReaskDialogTests
{
    [Fact]
    public async Task SilenceAtTheGate_SpeaksOneTrueReask_ThenAcceptsTheValue()
    {
        var spoken = new List<string>();
        var speechClient = new ScriptedSpeechInputClient();
        // Erste Antwort: Stille (kein Sprachbeginn im Fenster); zweite Antwort: das Annahmewort.
        await speechClient.EnqueueRecordStepAsync(_ => throw new NoSpeechDetectedException());
        await speechClient.EnqueueRecordStepAsync(
            _ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()])));
        speechClient.EnqueueTranscript("übernehmen", confidencePercent: 99);

        await using var recognitionWorker = new SpeechRecognitionWorker(speechClient);
        var speechService = new FormVoiceCommandSpeechService(
            Mock.Of<IEvaluationSessionTelemetry>(),
            Mock.Of<ISpeechCaptureRecordingIndicator>(),
            Mock.Of<ISpeechBargeInSignals>(),
            recognitionWorker,
            new RecordingTextToSpeechPlayer(spoken.Add),
            new FormVoiceCommandRecordingService(speechClient, speechClient),
            NullLogger<FormVoiceCommandSpeechService>.Instance);

        var contract = CreateContract();
        var context = CreateContext(contract);
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);
        var parser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());
        var commandExecutionService = new FormVoiceCommandGlobalCommandExecutionService(
            telemetry.Object,
            new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict).Object);
        var handler = new FormVoiceCommandConfirmationHandler(
            new FormVoiceCommandConfirmationValuePresenter(speechService, renderer),
            new FormVoiceCommandConfirmationDecisionService(
                new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                new FormVoiceCommandConfirmationTranscriptInterpreter(new FormVoiceCommandConfirmationCommandDecisionMapper(), parser),
                new FormVoiceCommandConfirmationMissingDecisionPolicy()),
            new FormVoiceCommandConfirmationRepairActionFlow(telemetry.Object, speechService, commandExecutionService),
            new FormVoiceCommandConfirmedFieldCompletionFlow(
                speechService,
                Mock.Of<IEarconPlayer>(),
                new FormClosingReviewTurnFlow(speechService, commandExecutionService)),
            new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy()));

        var session = new EvaluationSession("reask-session", EvaluationChannel.Voice, "app", "app/current");
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            context.IsoCode,
            ImmutableDictionary.CreateRange(StringComparer.OrdinalIgnoreCase, [new KeyValuePair<string, string>("name", "max")]));
        var field = await interpreter.GetFocusedFieldAsync();
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();
        stateTracker.BeginInterpretation();
        stateTracker.BeginValidation();
        var privacy = new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Loose);
        privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync(It.IsAny<string>())).ReturnsAsync(true);
        var turnContext = new FormVoiceCommandFieldTurnContext(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            new FormVoiceCommandAutoHelpTracker(
                context.VoiceSettings,
                Mock.Of<IFormVoiceCommandPromptService>(),
                Mock.Of<IFormVoiceCommandDialogStateTracker>(),
                interpreter,
                context.IsoCode),
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            privacy.Object);

        var result = await handler.HandleAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("max"),
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        var prompts = spoken.Where(line => line.StartsWith("tts:de:", StringComparison.Ordinal)).Select(line => line["tts:de:".Length..]).ToList();
        var firstQuestion = prompts.FindIndex(p => p.Contains("Name ist", StringComparison.Ordinal) && p.Contains("übernehmen", StringComparison.Ordinal));
        var reask = prompts.FindIndex(p => p.StartsWith("Ich habe nichts gehört.", StringComparison.Ordinal));
        Assert.True(firstQuestion >= 0, string.Join(" | ", prompts));
        Assert.True(reask > firstQuestion, string.Join(" | ", prompts));
        Assert.Contains("Name ist max. Übernehmen oder ändern?", prompts[reask], StringComparison.Ordinal);
        Assert.Equal(1, prompts.Count(p => p.StartsWith("Ich habe nichts gehört.", StringComparison.Ordinal)));
        Assert.DoesNotContain(prompts, p => p.Contains("nicht verstanden", StringComparison.Ordinal));
        Assert.Equal(2, speechClient.RecordCallCount);
    }

    private static FormVoiceCommandContext CreateContext(FormContract contract)
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
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            Forms = [form]
        };
        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: ["okay alex"]),
            Selection: new FormVoiceCommandSelectionContext(
                // Ohne automatisches Fortschreiten folgt die Fortsetzungsansage; die
                // kurze Stillefrist begrenzt zusätzlich das Aufnahmefenster im Test.
                VoiceSettings: new VoiceDialogSettings { MaxRetries = 0, NoInputTimeoutMs = 100, AutoAdvanceAfterCommit = false },
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }

    private static FormContract CreateContract()
        => new()
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            StrictLocalization = false,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
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
