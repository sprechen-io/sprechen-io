using Moq;
using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Gemeinsamer Testaufbau für die Erfassung eines Feldwerts mit Fehlerbehandlung und Kontext.
/// Wiederholungs- und Telemetrietests verwenden dadurch dieselbe Zusammenstellung der Dienste.
/// </summary>
internal static class CaptureTurnFixture
{
    internal static (
        FormVoiceCommandFieldTranscriptCapture Capture,
        RecordingSpeechService RecordingSpeech,
        FormVoiceCommandSpokenOutputReplayBuffer Buffer,
        FormFieldSnapshot Field,
        FormVoiceCommandFieldTurnContext TurnContext) Create(
        IFormVoiceCommandSpeechService captureSpeech,
        IEvaluationTelemetry? evaluationTelemetry = null,
        EvaluationSession? evaluationSession = null)
    {
        var telemetry = evaluationTelemetry ?? Mock.Of<IEvaluationTelemetry>();
        var recordingSpeech = new RecordingSpeechService();
        var buffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var errorHandling = new FormVoiceCommandFieldErrorHandlingService(
            telemetry, recordingSpeech, buffer, new CaptureReplayPolicy());
        var capture = new FormVoiceCommandFieldTranscriptCapture(captureSpeech, errorHandling);

        var context = CreateContext();
        var field = CreateFieldSnapshot();
        var session = evaluationSession
            ?? new EvaluationSession("session-routing", EvaluationChannel.Voice, "app", "app/form");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings { MaxRetries = 0 },
            Mock.Of<IFormVoiceCommandPromptService>(),
            stateTracker,
            Mock.Of<IFormDialogRuntime>(),
            isoCode: "de");

        var turnContext = new FormVoiceCommandFieldTurnContext(
            context,
            Mock.Of<IFormDialogRuntime>(),
            field,
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            Mock.Of<IFormDialogPrivacyReadbackPolicy>());

        return (capture, recordingSpeech, buffer, field, turnContext);
    }

    private static FormFieldSnapshot CreateFieldSnapshot()
        => new(
            Identity: new FormFieldIdentitySnapshot(
                FieldId: "date_of_birth",
                VariableId: "date_of_birth",
                FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Geburtsdatum",
                HelpText: null,
                Examples: [],
                Options: []),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));

    private static FormVoiceCommandContext CreateContext()
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
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("date_of_birth"),
                    Name = "date_of_birth",
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("date_of_birth"),
                    VariableId = VariableId.From("date_of_birth"),
                    Label = new LocalizedText { ["de"] = "Geburtsdatum" },
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
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("date_of_birth") }
                    ]
                }
            ]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "current",
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
}
