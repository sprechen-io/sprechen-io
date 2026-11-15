using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class ParsedCommandBranchDecisionResolverTests
{
    private readonly ParsedCommandBranchDecisionResolver _resolver = new();
    private readonly FormVoiceCommandParsedCommandFailureDecisionResolver _failureResolver = new();
    private readonly FormVoiceCommandParsedCommandExecutionDecisionResolver _executionResolver = new();

    // Im Wörtlich-Modus behandelt der übergeordnete Ablauf gewöhnliche Befehlswörter
    // als Werte. Ohne Ausführungsfreigabe ergibt sich hier deshalb kein Befehlszweig.
    [Fact]
    public void Resolve_WhenLiteralModeNonOffCommand_ReturnsNone()
    {
        var decision = _resolver.Resolve(
            CreateContext(),
            CreateInterpretation(
                hasParsedCommand: true,
                parsedCommandId: GlobalCommandIds.Next,
                hasExplicitCommandPrefix: false,
                isLiteralMode: true,
                isDigitsMode: false,
                shouldExecuteParsedCommand: false,
                shouldExecuteParsedCommandInDigitsMode: false));

        Assert.Equal(ParsedCommandBranchKind.None, decision.Kind);
    }

    [Fact]
    public void Resolve_WhenExplicitCommandIsUnknown_ReturnsPromptedFailure()
    {
        var decision = _resolver.Resolve(
            CreateContext(),
            CreateInterpretation(
                hasParsedCommand: false,
                parsedCommandId: string.Empty,
                hasExplicitCommandPrefix: true,
                isLiteralMode: false,
                isDigitsMode: false,
                shouldExecuteParsedCommand: false,
                shouldExecuteParsedCommandInDigitsMode: false));

        Assert.Equal(ParsedCommandBranchKind.PromptedFailure, decision.Kind);
        Assert.Equal(FormVoiceCommandFieldErrorCodes.UnknownCommand, decision.IssueCode);
    }

    [Fact]
    public void Resolve_WhenEditValueCommandIsParsed_ReturnsManualCorrection()
    {
        var decision = _resolver.Resolve(
            CreateContext(),
            CreateInterpretation(
                hasParsedCommand: true,
                parsedCommandId: GlobalCommandIds.EditValue,
                hasExplicitCommandPrefix: false,
                isLiteralMode: false,
                isDigitsMode: false,
                shouldExecuteParsedCommand: false,
                shouldExecuteParsedCommandInDigitsMode: false));

        Assert.Equal(ParsedCommandBranchKind.ManualCorrection, decision.Kind);
    }

    [Fact]
    public void Resolve_WhenParsedCommandShouldExecute_ReturnsExecuteCommand()
    {
        var decision = _resolver.Resolve(
            CreateContext(),
            CreateInterpretation(
                hasParsedCommand: true,
                parsedCommandId: GlobalCommandIds.Next,
                hasExplicitCommandPrefix: false,
                isLiteralMode: false,
                isDigitsMode: false,
                shouldExecuteParsedCommand: true,
                shouldExecuteParsedCommandInDigitsMode: false));

        Assert.Equal(ParsedCommandBranchKind.Execution, decision.Kind);
    }

    [Fact]
    public void Resolve_WhenNoParsedCommandAndNoExplicitCommandPrefix_ReturnsNone()
    {
        var decision = _resolver.Resolve(
            CreateContext(),
            CreateInterpretation(
                hasParsedCommand: false,
                parsedCommandId: string.Empty,
                hasExplicitCommandPrefix: false,
                isLiteralMode: false,
                isDigitsMode: false,
                shouldExecuteParsedCommand: false,
                shouldExecuteParsedCommandInDigitsMode: false));

        Assert.Equal(ParsedCommandBranchKind.None, decision.Kind);
    }

    // Ein gewöhnliches Befehlswort im Wörtlich-Modus begründet hier keinen Fehler;
    // die Wertbehandlung liegt im übergeordneten Ablauf.
    [Fact]
    public void TryResolveFailure_WhenLiteralModeNonOffCommand_ResolvesNoFailure()
    {
        var resolved = _failureResolver.TryResolve(
            CreateContext(),
            CreateInterpretation(
                hasParsedCommand: true,
                parsedCommandId: GlobalCommandIds.Next,
                hasExplicitCommandPrefix: false,
                isLiteralMode: true,
                isDigitsMode: false,
                shouldExecuteParsedCommand: false,
                shouldExecuteParsedCommandInDigitsMode: false),
            out var decision);

        Assert.False(resolved);
        Assert.Equal(ParsedCommandBranchKind.None, decision.Kind);
    }

    [Fact]
    public void ResolveExecution_WhenEditValueCommandIsParsed_ReturnsManualCorrection()
    {
        var decision = _executionResolver.Resolve(
            CreateInterpretation(
                hasParsedCommand: true,
                parsedCommandId: GlobalCommandIds.EditValue,
                hasExplicitCommandPrefix: false,
                isLiteralMode: false,
                isDigitsMode: false,
                shouldExecuteParsedCommand: false,
                shouldExecuteParsedCommandInDigitsMode: false));

        Assert.Equal(ParsedCommandBranchKind.ManualCorrection, decision.Kind);
    }

    [Fact]
    public void ResolveExecution_WhenEditValueOnNonTextFieldSuppressedByOptionMatch_ReturnsNone()
    {
        var decision = _executionResolver.Resolve(
            CreateInterpretation(
                hasParsedCommand: true,
                parsedCommandId: GlobalCommandIds.EditValue,
                hasExplicitCommandPrefix: false,
                isLiteralMode: false,
                isDigitsMode: false,
                shouldExecuteParsedCommand: false,
                shouldExecuteParsedCommandInDigitsMode: false,
                isTextField: false));

        Assert.Equal(ParsedCommandBranchKind.None, decision.Kind);
    }

    [Fact]
    public void ResolveExecution_WhenEditValueOnNonTextFieldWithoutOptionMatch_ReturnsManualCorrection()
    {
        var decision = _executionResolver.Resolve(
            CreateInterpretation(
                hasParsedCommand: true,
                parsedCommandId: GlobalCommandIds.EditValue,
                hasExplicitCommandPrefix: false,
                isLiteralMode: false,
                isDigitsMode: false,
                shouldExecuteParsedCommand: true,
                shouldExecuteParsedCommandInDigitsMode: false,
                isTextField: false));

        Assert.Equal(ParsedCommandBranchKind.ManualCorrection, decision.Kind);
    }

    private static FormVoiceCommandTranscriptInterpretation CreateInterpretation(
        bool hasParsedCommand,
        string parsedCommandId,
        bool hasExplicitCommandPrefix,
        bool isLiteralMode,
        bool isDigitsMode,
        bool shouldExecuteParsedCommand,
        bool shouldExecuteParsedCommandInDigitsMode,
        bool isTextField = true)
        => new(
            new FormVoiceCommandTranscriptSource(
                parsedCommandId.Length == 0 ? "unbekannt" : parsedCommandId,
                parsedCommandId.Length == 0 ? "unbekannt" : parsedCommandId,
                hasExplicitCommandPrefix),
            new FormVoiceCommandParsedCommand(
                hasParsedCommand,
                parsedCommandId,
                null,
                hasParsedCommand ? CommandSignalClass.Clear : CommandSignalClass.NoCommand),
            new FormVoiceCommandInterpretationMode(
                isTextField,
                isLiteralMode,
                isDigitsMode,
                shouldExecuteParsedCommand,
                shouldExecuteParsedCommandInDigitsMode));

    private static FormVoiceCommandContext CreateContext()
    {
        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
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
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("name") }
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
                VoiceSettings: new VoiceDialogSettings(),
                Application: new ApplicationDescriptor
                {
                    ApplicationId = "app",
                    DisplayName = new LocalizedText { ["de"] = "App" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("current"),
                            DisplayName = new LocalizedText { ["de"] = "Formular" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                },
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: new FormDescriptor
                {
                    FormId = FormId.From("current"),
                    DisplayName = new LocalizedText { ["de"] = "Formular" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                },
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }
}
