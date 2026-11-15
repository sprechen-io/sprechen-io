using Moq;
using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandAmbiguityResolverTests
{
    // Prüft, dass die gezeigten Eingaben keine zusätzliche Rückfrage zur
    // Unterscheidung von Befehl und Feldwert auslösen sollen. Der Resolver
    // liefert dafür None. Die weitere Befehlsausführung wird hier nicht geprüft.

    [Fact]
    public async Task ResolveAsync_WhenNoParsedCommand_ReturnsNone()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var resolver = new FormVoiceCommandAmbiguityResolver();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var stateTracker = CreateStateTracker();

        var result = await ResolveAsync(resolver,
            context, CreateSession(), stateTracker,
            hasParsedCommand: false,
            parsedCommandId: GlobalCommandIds.Next,
            isTextField: true,
            hasExplicitCommandPrefix: false,
            trimmedTranscript: "weiter bitte",
            inputTranscript: "weiter bitte",
            CancellationToken.None);

        AssertNone(result);
        speechService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WhenNotTextField_ReturnsNone()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var resolver = new FormVoiceCommandAmbiguityResolver();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var stateTracker = CreateStateTracker();

        var result = await ResolveAsync(resolver,
            context, CreateSession(), stateTracker,
            hasParsedCommand: true,
            parsedCommandId: GlobalCommandIds.Next,
            isTextField: false,
            hasExplicitCommandPrefix: false,
            trimmedTranscript: "weiter bitte",
            inputTranscript: "weiter bitte",
            CancellationToken.None);

        AssertNone(result);
        speechService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WhenHasExplicitCommandPrefix_ReturnsNone()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var resolver = new FormVoiceCommandAmbiguityResolver();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var stateTracker = CreateStateTracker();

        var result = await ResolveAsync(resolver,
            context, CreateSession(), stateTracker,
            hasParsedCommand: true,
            parsedCommandId: GlobalCommandIds.Next,
            isTextField: true,
            hasExplicitCommandPrefix: true,
            trimmedTranscript: "kommando weiter",
            inputTranscript: "kommando weiter",
            CancellationToken.None);

        AssertNone(result);
        speechService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(GlobalCommandIds.Cancel)]
    [InlineData(GlobalCommandIds.BackToFormSelection)]
    [InlineData(GlobalCommandIds.BackToAppSelection)]
    [InlineData(GlobalCommandIds.OpenApplications)]
    public async Task ResolveAsync_WhenAlwaysGlobalCommandWithoutAnchor_ReturnsNone(string commandId)
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var resolver = new FormVoiceCommandAmbiguityResolver();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var stateTracker = CreateStateTracker();

        var result = await ResolveAsync(resolver,
            context, CreateSession(), stateTracker,
            hasParsedCommand: true,
            parsedCommandId: commandId,
            isTextField: true,
            hasExplicitCommandPrefix: false,
            trimmedTranscript: "abbrechen",
            inputTranscript: "abbrechen",
            CancellationToken.None);

        AssertNone(result);
        speechService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(GlobalCommandIds.Next, "weiter")]
    [InlineData(GlobalCommandIds.Help, "hilfe")]
    [InlineData(GlobalCommandIds.Cancel, "abbrechen")]
    [InlineData(GlobalCommandIds.Cancel, "ich moechte das hier abbrechen und neu")]
    [InlineData(GlobalCommandIds.Repeat, "wiederholen")]
    [InlineData(GlobalCommandIds.Repeat, "meine adresse ist wiederholen strasse 1")]
    [InlineData(GlobalCommandIds.ChangeLanguage, "auf englisch wechseln")]
    [InlineData(GlobalCommandIds.ChangePoliteness, "sei locker")]
    public async Task ResolveAsync_ForFreeTextCommandCandidate_ReturnsNoneWithoutAskingRueckfrage(
        string parsedCommandId,
        string transcript)
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var resolver = new FormVoiceCommandAmbiguityResolver();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var stateTracker = CreateStateTracker();

        var result = await ResolveAsync(resolver,
            context, CreateSession(), stateTracker,
            hasParsedCommand: true,
            parsedCommandId: parsedCommandId,
            isTextField: true,
            hasExplicitCommandPrefix: false,
            trimmedTranscript: transcript,
            inputTranscript: transcript,
            CancellationToken.None);

        AssertNone(result);
        speechService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WhenOkayLeadIn_ReturnsNone()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var resolver = new FormVoiceCommandAmbiguityResolver();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var stateTracker = CreateStateTracker();

        var result = await ResolveAsync(resolver,
            context, CreateSession(), stateTracker,
            hasParsedCommand: true,
            parsedCommandId: GlobalCommandIds.Next,
            isTextField: true,
            hasExplicitCommandPrefix: false,
            trimmedTranscript: "okay weiter",
            inputTranscript: "okay weiter",
            CancellationToken.None);

        AssertNone(result);
        speechService.VerifyNoOtherCalls();
    }

    private static void AssertNone(AmbiguityResolutionResult result)
    {
        var none = AmbiguityResolutionResult.None();
        Assert.Equal(none.ExecuteCommand, result.ExecuteCommand);
        Assert.Equal(none.EmitAcceptedTelemetry, result.EmitAcceptedTelemetry);
        Assert.Equal(none.ResolutionStrategy, result.ResolutionStrategy);
    }

    // Ein unerwartet aktiviertes Mehrdeutigkeitssignal muss einen Fehler melden,
    // solange die zugehörige Rückfrage nicht bereitsteht.
    [Fact]
    public async Task ResolveAsync_WhenASignalFires_ThrowsBecauseNoClarificationDialogExists()
    {
        var resolver = new FormVoiceCommandAmbiguityResolver(new FiringAmbiguitySignalTestDouble());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ResolveAsync(
            resolver,
            CreateContext(CreateMinimalContract()),
            CreateSession(),
            CreateStateTracker(),
            hasParsedCommand: true,
            parsedCommandId: GlobalCommandIds.Next,
            isTextField: true,
            hasExplicitCommandPrefix: false,
            trimmedTranscript: "weiter",
            inputTranscript: "weiter",
            CancellationToken.None));
        Assert.Contains("no clarification dialog", exception.Message, StringComparison.Ordinal);
    }

    private static FormVoiceCommandStateTracker CreateStateTracker()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var session = CreateSession();
        return new FormVoiceCommandStateTracker(telemetry.Object, session);
    }

    private static EvaluationSession CreateSession()
        => new("session-test", EvaluationChannel.Voice, "app", "app/current");

    private static Task<AmbiguityResolutionResult> ResolveAsync(
        FormVoiceCommandAmbiguityResolver resolver,
        FormVoiceCommandContext context,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        bool hasParsedCommand,
        string parsedCommandId,
        bool isTextField,
        bool hasExplicitCommandPrefix,
        string trimmedTranscript,
        string inputTranscript,
        CancellationToken cancellationToken)
    {
        var interpretation = new FormVoiceCommandTranscriptInterpretation(
            new FormVoiceCommandTranscriptSource(
                trimmedTranscript,
                inputTranscript,
                hasExplicitCommandPrefix),
            new FormVoiceCommandParsedCommand(
                hasParsedCommand,
                parsedCommandId,
                ParsedCommandArgument: null,
                CommandSignalClass.NoCommand),
            new FormVoiceCommandInterpretationMode(
                isTextField,
                IsLiteralMode: false,
                IsDigitsMode: false,
                ShouldExecuteParsedCommand: false,
                ShouldExecuteParsedCommandInDigitsMode: false));

        return resolver.ResolveAsync(context, session, stateTracker, interpretation, cancellationToken);
    }

    private static FormContract CreateMinimalContract(string isoCode = "de")
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { [isoCode] = "Form 1" },
            SupportedLanguages = [isoCode],
            DefaultLanguage = isoCode,
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
                    Label = new LocalizedText { [isoCode] = "Name" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { [isoCode] = "Main" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("name") }
                    ]
                }
            ]
        };
    }

    private static FormVoiceCommandContext CreateContext(FormContract contract, string isoCode = "de")
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { [isoCode] = "Form 1" },
            Description = new LocalizedText { [isoCode] = "Form 1" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { [isoCode] = "App" },
            Description = new LocalizedText { [isoCode] = "App" },
            SupportedLanguages = [isoCode],
            DefaultLanguage = isoCode,
            Forms = [form]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: isoCode,
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: []),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }
}
