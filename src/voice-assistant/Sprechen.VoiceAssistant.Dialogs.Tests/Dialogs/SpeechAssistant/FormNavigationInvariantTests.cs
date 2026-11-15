using Moq;
using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die Navigationsregeln fest, die das Navigationsmodell als feste Regeln des Interpreters
/// beschreibt: „weiter“ auf dem letzten Feld ist ein Absendeversuch, und trifft eine
/// Sprungäußerung mehrere Stufen zugleich, gewinnt die höhere Stufe.
/// </summary>
public sealed class FormNavigationInvariantTests
{
    [Fact]
    public async Task Next_WhenNoFieldFollows_AttemptsSubmissionOnce()
    {
        var submission = CreateSubmission();
        var interpreter = new Mock<IFormDialogRuntime>();
        interpreter.Setup(i => i.TryMoveFocusNextAsync()).ReturnsAsync(false);

        var handler = new FormVoiceCommandNavigationHandler(
            Mock.Of<IEvaluationFormTelemetry>(),
            submission.Object,
            Mock.Of<IFormVoiceCommandSpeechService>());
        await handler.HandleAsync(CreateNextContext(interpreter.Object), CancellationToken.None);

        submission.Verify(
            s => s.TrySubmitAsync(
                interpreter.Object,
                It.IsAny<FormContract>(),
                It.IsAny<EvaluationSession>(),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<IFormVoiceCommandDialogStateTracker>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Next_WhenAFieldFollows_MovesWithoutSubmitting()
    {
        var submission = CreateSubmission();
        var interpreter = new Mock<IFormDialogRuntime>();
        interpreter.Setup(i => i.TryMoveFocusNextAsync()).ReturnsAsync(true);

        var handler = new FormVoiceCommandNavigationHandler(
            Mock.Of<IEvaluationFormTelemetry>(),
            submission.Object,
            Mock.Of<IFormVoiceCommandSpeechService>());
        await handler.HandleAsync(CreateNextContext(interpreter.Object), CancellationToken.None);

        submission.VerifyNoOtherCalls();
    }

    [Fact]
    public void Jump_WhenSpokenNameAndLabelCollide_SpokenNameWinsEvenAgainstDocumentOrder()
    {
        var resolver = CreateCollidingResolver();

        Assert.True(resolver.TryResolve(FormJumpTarget.From("kontakt"), out var target));

        Assert.Equal(FormJumpTargetKind.Field, target!.Kind);
        Assert.Equal("mailField", target.TargetId);
    }

    [Fact]
    public void Jump_WhenLabelAndFieldGroupTitleCollide_FieldLabelWins()
    {
        var resolver = CreateCollidingResolver();

        Assert.True(resolver.TryResolve(FormJumpTarget.From("adresse"), out var target));

        Assert.Equal(FormJumpTargetKind.Field, target!.Kind);
        Assert.Equal("cityField", target.TargetId);
    }

    [Fact]
    public void Jump_WhenFieldGroupTitleAndSynonymCollide_FieldGroupTitleWins()
    {
        var resolver = CreateCollidingResolver();

        Assert.True(resolver.TryResolve(FormJumpTarget.From("einstieg"), out var target));

        Assert.Equal(FormJumpTargetKind.FieldGroup, target!.Kind);
        Assert.Equal("entry", target.TargetId);
    }

    private static Mock<IFormVoiceCommandSubmissionService> CreateSubmission()
    {
        var submission = new Mock<IFormVoiceCommandSubmissionService>();
        submission
            .Setup(s => s.TrySubmitAsync(
                It.IsAny<IFormDialogRuntime>(),
                It.IsAny<FormContract>(),
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<IFormVoiceCommandDialogStateTracker>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormSubmissionResult.BlockedAwaitingCorrection);
        return submission;
    }

    private static FormVoiceCommandGlobalCommandContext CreateNextContext(IFormDialogRuntime interpreter)
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("form-1"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            Description = new LocalizedText { ["de"] = "Teil 1" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };
        var application = new ApplicationDescriptor
        {
            ApplicationId = "test-app",
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
            ApplicationId = "test-app",
            FormId = FormId.From("form-1"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
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
                    Members = [new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("name") }]
                }
            ]
        };

        return new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: GlobalCommandIds.Next,
                IsoCode: "de",
                Argument: null),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Runtime: new FormVoiceCommandGlobalCommandRuntime(
                Interpreter: interpreter,
                Session: new EvaluationSession("test", EvaluationChannel.Voice, "test-app", "form-1"),
                StateTracker: new TestFormVoiceCommandDialogStateTracker()));
    }

    // Jede Äußerung trifft zwei Stufen zugleich: „kontakt“ ist Bezeichnung des ersten und
    // gesprochener Name des zweiten Felds, „adresse“ Bezeichnung eines Felds und Titel seiner
    // Feldgruppe, „einstieg“ Titel einer Feldgruppe und Synonym eines Felds.
    private static FormJumpTargetResolver CreateCollidingResolver()
    {
        var orderedFieldIds = ImmutableArray.Create("streetField", "mailField", "cityField");
        var fieldsById = new Dictionary<string, FieldDefinition>(StringComparer.Ordinal)
        {
            ["streetField"] = Field("streetField", "street", "Kontakt"),
            ["mailField"] = Field("mailField", "mail", "E-Mail", spokenName: "Kontakt"),
            ["cityField"] = Field("cityField", "city", "Adresse")
        }.ToImmutableDictionary(kvp => FieldId.From(kvp.Key), kvp => kvp.Value);

        var fieldGroupsById = new Dictionary<string, FieldGroupDefinition>(StringComparer.Ordinal)
        {
            ["entry"] = new()
            {
                FieldGroupId = FieldGroupId.From("entry"),
                Title = new LocalizedText { ["de"] = "Einstieg" },
                Members =
                [
                    new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("streetField") },
                    new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("mailField") },
                    new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("address") }
                ]
            },
            ["address"] = new()
            {
                FieldGroupId = FieldGroupId.From("address"),
                Title = new LocalizedText { ["de"] = "Adresse" },
                Members = [new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("cityField") }]
            }
        };

        var localizationResolver = new FormLocalizationResolver(
            requestedIsoCode: "de",
            defaultLanguage: "de",
            supportedLanguages: ["de"],
            strictLocalization: false,
            politenessLevel: PolitenessLevel.Formal,
            variablesById: ImmutableDictionary<VariableId, VariableDefinition>.Empty,
            fieldGroupsById: fieldGroupsById.ToImmutableDictionary(kvp => FieldGroupId.From(kvp.Key), kvp => kvp.Value));

        var navigatorState = new TestNavigatorState(
            orderedFieldIds,
            orderedFieldIds
                .Select((fieldId, index) => (fieldId, index))
                .ToImmutableDictionary(pair => pair.fieldId, pair => pair.index, StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["streetField"] = "entry",
                ["mailField"] = "entry",
                ["cityField"] = "address"
            }.ToImmutableDictionary(StringComparer.Ordinal),
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["entry"] = null,
                ["address"] = "entry"
            }.ToImmutableDictionary(StringComparer.Ordinal));

        var fieldNameSynonyms = new Dictionary<string, SynonymSet>(StringComparer.Ordinal)
        {
            ["mailField"] = new()
            {
                PrimaryName = new LocalizedText { ["de"] = "mail" },
                Synonyms = [new LocalizedText { ["de"] = "einstieg" }]
            }
        };

        return new FormJumpTargetResolver(
            orderedFieldIds,
            fieldsById,
            ImmutableArray.Create("entry", "address"),
            fieldNameSynonyms,
            localizationResolver,
            navigatorState);
    }

    private static FieldDefinition Field(string fieldId, string variableId, string label, string? spokenName = null)
        => new()
        {
            FieldId = FieldId.From(fieldId),
            VariableId = VariableId.From(variableId),
            Label = new LocalizedText { ["de"] = label },
            Editability = FieldEditability.UserEditable,
            Projections = spokenName is null
                ? null
                : new FieldProjections
                {
                    Voice = new FieldVoiceProjection { Names = new LocalizedText { ["de"] = spokenName } }
                }
        };
}
