using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Texte und Wiederholungszustand für schreibgeschützte Hinweise einer Feldgruppe.
/// Leere Hinweise bleiben unberücksichtigt; der sensible Beispielwert fehlt in den angeforderten Ansagen.
/// </summary>
public sealed class FormVoiceCommandReadOnlyFieldSpeakerTests
{
    private const string InstructionText = "Die Referenznummer steht oben rechts auf der Rechnung.";
    private const string SensitiveText = "Interner Vorgang 12345678";

    [Fact]
    public async Task Announce_SpeaksValuedReadOnlyFieldsOnce_AndNeverAgainOnLaterIntros()
    {
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandReadOnlyFieldSpeaker(speechService, new FormVoiceCommandSpokenOutputReplayBuffer());
        var turnContext = CreateTurnContext(includeSensitiveField: true, includeValuedField: true);

        await speaker.AnnounceFieldGroupReadOnlyFieldsAsync(turnContext, true, CancellationToken.None);

        // Der ungeschützte Hinweis enthält Bezeichnung und Wert, der sensible Hinweis nur die Bezeichnung mit Datenschutzhinweis.
        // Das leere Hinweisfeld fehlt in den angeforderten Texten.
        Assert.Equal(2, speechService.Calls.Count);
        Assert.Equal($"Zur Information. Hinweis ist {InstructionText}", speechService.Calls[0].Text);
        Assert.Equal("Zur Information. Das Feld Vertraulich wird nicht automatisch vorgelesen.", speechService.Calls[1].Text);
        Assert.DoesNotContain(speechService.Calls, call => call.Text.Contains(SensitiveText, StringComparison.Ordinal));
        Assert.DoesNotContain(speechService.Calls, call => call.Text.Contains("Leerhinweis", StringComparison.Ordinal));

        await speaker.AnnounceFieldGroupReadOnlyFieldsAsync(turnContext, true, CancellationToken.None);

        Assert.Equal(2, speechService.Calls.Count);
    }

    [Fact]
    public async Task Announce_WithNothingToConvey_StaysSilent_AndDoesNotConsumeTheLatch()
    {
        // Die Gruppe enthält als schreibgeschützten Hinweis nur ein Feld ohne Wert.
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandReadOnlyFieldSpeaker(speechService, new FormVoiceCommandSpokenOutputReplayBuffer());
        var turnContext = CreateTurnContext(includeSensitiveField: false, includeValuedField: false);

        await speaker.AnnounceFieldGroupReadOnlyFieldsAsync(turnContext, true, CancellationToken.None);

        // Ohne Ansage bleibt der einmalige Gruppenhinweis unverbraucht.
        Assert.Empty(speechService.Calls);
        Assert.False(turnContext.StateTracker.HasAlreadyTaught($"{DialogTeachOnceTopics.ReadOnlyFieldGroupContent}:main"));
    }

    [Fact]
    public async Task Compose_PreservesPrivacyAndOnce_WithoutReplacingReplayWithFieldQuestion()
    {
        var speechService = new RecordingSpeechService();
        var replayBuffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var speaker = new FormVoiceCommandReadOnlyFieldSpeaker(speechService, replayBuffer);
        var context = CreateTurnContext(true, true) with { FieldEntryPrompt = "Feldfrage." };

        var prompt = await speaker.ComposeFieldEntryPromptAsync(context, true, CancellationToken.None);

        Assert.Equal($"Feldfrage. Zur Information. Hinweis ist {InstructionText} Zur Information. Das Feld Vertraulich wird nicht automatisch vorgelesen.", prompt);
        Assert.DoesNotContain(SensitiveText, prompt, StringComparison.Ordinal);
        Assert.Empty(speechService.Calls);
        var expectedReplay = "Zur Information. Das Feld Vertraulich wird nicht automatisch vorgelesen.";
        Assert.Equal(expectedReplay, replayBuffer.LastSpokenOutput);
        Assert.Equal("Feldfrage.", await speaker.ComposeFieldEntryPromptAsync(context, true, CancellationToken.None));
        await speaker.AnnounceFieldGroupReadOnlyFieldsAsync(context, true, CancellationToken.None);
        Assert.Empty(speechService.Calls);
        Assert.Equal(expectedReplay, replayBuffer.LastSpokenOutput);
    }

    [Fact]
    public async Task Compose_EmptyProjection_DoesNotConsumeGroupLatch()
    {
        var speaker = new FormVoiceCommandReadOnlyFieldSpeaker(new RecordingSpeechService(), new FormVoiceCommandSpokenOutputReplayBuffer());
        var context = CreateTurnContext(false, false);

        Assert.Equal(context.Field.Label, await speaker.ComposeFieldEntryPromptAsync(context, true, CancellationToken.None));
        Assert.False(context.StateTracker.HasAlreadyTaught($"{DialogTeachOnceTopics.ReadOnlyFieldGroupContent}:main"));
    }

    [Fact]
    public async Task Compose_OutsideGroupEntry_DoesNotConsumeFutureAnnouncement()
    {
        var replayBuffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var speaker = new FormVoiceCommandReadOnlyFieldSpeaker(new RecordingSpeechService(), replayBuffer);
        var context = CreateTurnContext(false, true);

        Assert.Equal(context.Field.Label, await speaker.ComposeFieldEntryPromptAsync(context, false, CancellationToken.None));
        Assert.Null(replayBuffer.LastSpokenOutput);
        Assert.EndsWith(InstructionText, await speaker.ComposeFieldEntryPromptAsync(context, true, CancellationToken.None), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Announcement_CancelledBeforeRequest_DoesNotConsumeLatch(bool separateOutput)
    {
        var speechService = new RecordingSpeechService();
        var replayBuffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var speaker = new FormVoiceCommandReadOnlyFieldSpeaker(speechService, replayBuffer);
        var context = CreateTurnContext(false, true);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            if (separateOutput)
                await speaker.AnnounceFieldGroupReadOnlyFieldsAsync(context, true, cancellation.Token);
            else
                await speaker.ComposeFieldEntryPromptAsync(context, true, cancellation.Token);
        });

        Assert.Empty(speechService.Calls);
        Assert.Null(replayBuffer.LastSpokenOutput);
        Assert.False(context.StateTracker.HasAlreadyTaught($"{DialogTeachOnceTopics.ReadOnlyFieldGroupContent}:main"));
    }

    [Fact]
    public async Task Compose_ProvidesSameGuidanceToAutomaticAndManualReplay()
    {
        const string fieldQuestion = "Waehlen Sie eine Option.";
        var speechService = new RecordingSpeechService();
        var replayBuffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var speaker = new FormVoiceCommandReadOnlyFieldSpeaker(speechService, replayBuffer);
        var context = CreateTurnContext(false, true) with { FieldEntryPrompt = fieldQuestion };
        replayBuffer.BeginFieldTurn(context.Field.FieldId);
        await speaker.ComposeFieldEntryPromptAsync(context, true, CancellationToken.None);
        var guidance = $"Zur Information. Hinweis ist {InstructionText}";
        Assert.Equal(guidance, replayBuffer.LastSpokenOutput);
        var errorHandling = new FormVoiceCommandFieldErrorHandlingService(
            Mock.Of<IEvaluationTelemetry>(), speechService, replayBuffer, new CaptureReplayPolicy());

        await errorHandling.HandleCaptureFailureAsync(context, InputModes.Voice, FormVoiceCommandFieldErrorCodes.NoInput, true, CancellationToken.None);
        await errorHandling.HandleCaptureFailureAsync(context, InputModes.Voice, FormVoiceCommandFieldErrorCodes.NoInput, true, CancellationToken.None);

        var autoReplay = Assert.Single(speechService.ReplayCalls);
        Assert.EndsWith(guidance, autoReplay.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(fieldQuestion, autoReplay.Text, StringComparison.Ordinal);
        var repeatHandler = new FormVoiceCommandRepeatHandler(replayBuffer, speechService, Mock.Of<IFormVoiceCommandPromptService>());
        var commandContext = new FormVoiceCommandGlobalCommandContext(
            new FormVoiceCommandGlobalCommandRequest(GlobalCommandIds.Repeat, "de", null),
            context.Context.Selection,
            new FormVoiceCommandGlobalCommandRuntime(context.Interpreter, context.Session, context.StateTracker));
        await repeatHandler.HandleAsync(commandContext, CancellationToken.None);
        Assert.Equal(guidance, Assert.Single(speechService.Calls).Text);
        Assert.Equal(fieldQuestion, await speaker.ComposeFieldEntryPromptAsync(context, false, CancellationToken.None));
        Assert.Equal(guidance, replayBuffer.LastSpokenOutput);
    }

    private static FormVoiceCommandFieldTurnContext CreateTurnContext(bool includeSensitiveField, bool includeValuedField)
    {
        var contract = CreateContract(includeSensitiveField, includeValuedField);
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var telemetry = new Mock<IEvaluationTelemetry>();
        var session = new EvaluationSession("readonly-speaker-session", EvaluationChannel.Voice, "app", "app/form");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt("main");
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings { MaxRetries = 0 },
            Mock.Of<IFormVoiceCommandPromptService>(),
            stateTracker,
            interpreter,
            isoCode: "de");

        return new FormVoiceCommandFieldTurnContext(
            CreateContext(contract),
            interpreter,
            CreateInputFieldSnapshot(),
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            interpreter);
    }

    private static FormFieldSnapshot CreateInputFieldSnapshot()
        => new(
            Identity: new FormFieldIdentitySnapshot(
                FieldId: "refField",
                VariableId: "ref",
                FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Hauptteil",
                Label: "Referenznummer",
                HelpText: null,
                Examples: [],
                Options: []),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));

    private static FormVoiceCommandContext CreateContext(FormContract contract)
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            Description = new LocalizedText { ["de"] = "Formular" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };
        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "Anwendung" },
            Description = new LocalizedText { ["de"] = "Anwendung" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "form",
                WakeWordActivationForms: []),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings { MaxRetries = 0 },
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }

    private static FormContract CreateContract(bool includeSensitiveField, bool includeValuedField)
    {
        var stringValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement;

        var variables = new List<VariableDefinition>
        {
            new()
            {
                VariableId = VariableId.From("ref"),
                Name = "ref",
                ValueType = stringValueType
            },
            new()
            {
                VariableId = VariableId.From("emptyInstruction"),
                Name = "emptyInstruction",
                ValueType = stringValueType
            }
        };
        var fields = new List<FieldDefinition>
        {
            new()
            {
                FieldId = FieldId.From("refField"),
                VariableId = VariableId.From("ref"),
                Label = new LocalizedText { ["de"] = "Referenznummer" },
                Editability = FieldEditability.UserEditable
            },
            new()
            {
                FieldId = FieldId.From("emptyInstructionField"),
                VariableId = VariableId.From("emptyInstruction"),
                Label = new LocalizedText { ["de"] = "Leerhinweis" },
                Editability = FieldEditability.ReadOnly
            }
        };
        var members = new List<FieldGroupMember>
        {
            new() { Kind = "field", FieldId = FieldId.From("refField") },
            new() { Kind = "field", FieldId = FieldId.From("emptyInstructionField") }
        };

        if (includeValuedField)
        {
            variables.Add(new VariableDefinition
            {
                VariableId = VariableId.From("instruction"),
                Name = "instruction",
                ValueType = stringValueType,
                InitialValue = JsonDocument.Parse($"\"{InstructionText}\"").RootElement
            });
            fields.Add(new FieldDefinition
            {
                FieldId = FieldId.From("instructionField"),
                VariableId = VariableId.From("instruction"),
                Label = new LocalizedText { ["de"] = "Hinweis" },
                Editability = FieldEditability.ReadOnly
            });
            members.Add(new FieldGroupMember { Kind = "field", FieldId = FieldId.From("instructionField") });
        }

        if (includeSensitiveField)
        {
            variables.Add(new VariableDefinition
            {
                VariableId = VariableId.From("sensitiveNote"),
                Name = "sensitiveNote",
                ValueType = stringValueType,
                PrivacyClass = PrivacyClass.Sensitive,
                InitialValue = JsonDocument.Parse($"\"{SensitiveText}\"").RootElement
            });
            fields.Add(new FieldDefinition
            {
                FieldId = FieldId.From("sensitiveField"),
                VariableId = VariableId.From("sensitiveNote"),
                Label = new LocalizedText { ["de"] = "Vertraulich" },
                Editability = FieldEditability.ReadOnly
            });
            members.Add(new FieldGroupMember { Kind = "field", FieldId = FieldId.From("sensitiveField") });
        }

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables = variables,
            Fields = fields,
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Hauptteil" },
                    Members = members
                }
            ]
        };
    }
}
