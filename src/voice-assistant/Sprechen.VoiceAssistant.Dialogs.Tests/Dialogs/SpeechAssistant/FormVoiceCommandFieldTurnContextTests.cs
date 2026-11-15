using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandFieldTurnContextTests
{
    [Fact]
    public void Constructor_WhenContextIsNull_Throws()
    {
        var parts = CreateParts();

        Assert.Throws<ArgumentNullException>(() => new FormVoiceCommandFieldTurnContext(
            null!,
            parts.Interpreter,
            parts.Field,
            parts.Session,
            parts.StateTracker,
            parts.AutoHelp,
            parts.Escalation,
            parts.PrivacyReadbackPolicy));
    }

    [Fact]
    public void Constructor_WhenFieldIdentityIsMalformed_Throws()
    {
        var parts = CreateParts(fieldId: " ");

        Assert.Throws<ArgumentException>(() => new FormVoiceCommandFieldTurnContext(
            parts.Context,
            parts.Interpreter,
            parts.Field,
            parts.Session,
            parts.StateTracker,
            parts.AutoHelp,
            parts.Escalation,
            parts.PrivacyReadbackPolicy));
    }

    [Fact]
    public void Constructor_WhenValid_PreservesTurnCollaborators()
    {
        var parts = CreateParts();

        var turnContext = new FormVoiceCommandFieldTurnContext(
            parts.Context,
            parts.Interpreter,
            parts.Field,
            parts.Session,
            parts.StateTracker,
            parts.AutoHelp,
            parts.Escalation,
            parts.PrivacyReadbackPolicy);

        Assert.Same(parts.Context, turnContext.Context);
        Assert.Same(parts.Interpreter, turnContext.Interpreter);
        Assert.Same(parts.Field, turnContext.Field);
        Assert.Same(parts.Session, turnContext.Session);
        Assert.Same(parts.StateTracker, turnContext.StateTracker);
        Assert.Same(parts.AutoHelp, turnContext.AutoHelp);
        Assert.Same(parts.Escalation, turnContext.Escalation);
        Assert.Same(parts.PrivacyReadbackPolicy, turnContext.PrivacyReadbackPolicy);
    }

    private static (
        FormVoiceCommandContext Context,
        IFormDialogRuntime Interpreter,
        FormFieldSnapshot Field,
        EvaluationSession Session,
        FormVoiceCommandStateTracker StateTracker,
        FormVoiceCommandAutoHelpTracker AutoHelp,
        FormVoiceCommandUnrecognizedInputEscalationPolicy Escalation,
        IFormDialogPrivacyReadbackPolicy PrivacyReadbackPolicy) CreateParts(string fieldId = "field")
    {
        var telemetry = Mock.Of<IEvaluationSessionTelemetry>();
        var session = new EvaluationSession("session", EvaluationChannel.Voice, "app", "app/form");
        var interpreter = Mock.Of<IFormDialogRuntime>();
        var context = CreateContext();

        return (
            context,
            interpreter,
            CreateField(fieldId),
            session,
            new FormVoiceCommandStateTracker(telemetry, session),
            new FormVoiceCommandAutoHelpTracker(
                context.VoiceSettings,
                Mock.Of<IFormVoiceCommandPromptService>(),
                Mock.Of<IFormVoiceCommandDialogStateTracker>(),
                interpreter,
                context.IsoCode),
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            Mock.Of<IFormDialogPrivacyReadbackPolicy>());
    }

    private static FormFieldSnapshot CreateField(string fieldId)
        => new(
            new FormFieldIdentitySnapshot(fieldId, "variable", "group"),
            new FormFieldPresentationSnapshot("Group", "Field", HelpText: null, [], []),
            new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));

    private static FormVoiceCommandContext CreateContext()
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Form" },
            Description = new LocalizedText { ["de"] = "Form" },
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
            FormId = FormId.From("form"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("group"),
            StrictLocalization = true,
            Variables = [],
            Fields = [],
            FieldGroups = []
        };

        return new FormVoiceCommandContext(
            new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "form",
                WakeWordActivationForms: []),
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
}
