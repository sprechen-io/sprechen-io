using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Vorbereitung und Übernahme zusätzlicher Feldwerte: erlaubte Ziele, Lesbarkeit,
/// Schutz beim Vorlesen und Ablehnung ungültiger oder veralteter Vorschläge.
/// Die Zustandsprüfungen betreffen Werte, Fokus und Revision; sie vergleichen keine vollständige Byteabbildung.
/// </summary>
public sealed class FormAdditionalFieldProposalPolicyTests
{
    [Fact]
    public async Task Prepare_WhenTargetIsDeclaredReadOnly_ReturnsUnavailableWithoutOfferOrToken()
    {
        var interpreter = CreateInterpreter(CreateContract(cityEditability: FieldEditability.ReadOnly));

        var preparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());

        Assert.Equal(FormAdditionalFieldProposalPreparationStatus.Unavailable, preparation.Status);
        Assert.Null(preparation.SpokenOffer);
        Assert.Null(preparation.CommitToken);
    }

    [Fact]
    public async Task Prepare_WhenTargetIsComputed_ReturnsUnavailable()
    {
        var interpreter = CreateInterpreter(CreateContract(cityEditability: FieldEditability.Computed));

        var preparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());

        Assert.Equal(FormAdditionalFieldProposalPreparationStatus.Unavailable, preparation.Status);
    }

    [Fact]
    public async Task Prepare_WhenVoiceInputIsForbidden_ReturnsUnavailable()
    {
        var interpreter = CreateInterpreter(CreateContract(forbidCityVoiceInput: true));

        var preparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());

        Assert.Equal(FormAdditionalFieldProposalPreparationStatus.Unavailable, preparation.Status);
    }

    [Fact]
    public async Task Prepare_WhenValueIsNotReadableForTheBoundType_ReturnsInvalid()
    {
        var interpreter = CreateInterpreter(CreateContract(cityIsInteger: true));

        var preparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());

        Assert.Equal(FormAdditionalFieldProposalPreparationStatus.Invalid, preparation.Status);
        Assert.Null(preparation.CommitToken);
    }

    [Fact]
    public async Task Prepare_WhenReadbackIsForbidden_OffersMaskedWordingInsteadOfTheValue()
    {
        var interpreter = CreateInterpreter(CreateContract(protectCityReadback: true));

        var preparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());

        Assert.Equal(FormAdditionalFieldProposalPreparationStatus.Prepared, preparation.Status);
        Assert.NotNull(preparation.SpokenOffer);
        Assert.DoesNotContain("Berlin", preparation.SpokenOffer, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Prepare_LeavesValuesAndStateRevisionUnchanged()
    {
        var interpreter = CreateInterpreter(CreateContract());
        var revisionBefore = interpreter.MutableState.StateRevision;
        var valuesBefore = await interpreter.GetValuesSnapshotAsync();

        var preparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());

        Assert.Equal(FormAdditionalFieldProposalPreparationStatus.Prepared, preparation.Status);
        Assert.Equal(revisionBefore, interpreter.MutableState.StateRevision);
        Assert.Equal(valuesBefore, await interpreter.GetValuesSnapshotAsync());
    }

    [Fact]
    public async Task Commit_WhenProposalIsUnknown_ReturnsUnavailableWithoutRevision()
    {
        var interpreter = CreateInterpreter(CreateContract());
        var revisionBefore = interpreter.MutableState.StateRevision;

        var adoption = await interpreter.CommitAdditionalFieldProposalAsync(
            new FormAdditionalFieldProposalCommitToken("unknown-proposal"));

        Assert.Equal(FormAdditionalFieldValueAdoptionStatus.Unavailable, adoption.Status);
        Assert.Equal(0, adoption.CommitRevision);
        Assert.Equal(revisionBefore, interpreter.MutableState.StateRevision);
    }

    [Fact]
    public async Task Commit_WhenApplied_PublishesExactlyOneRevisionAndReportsIt()
    {
        var interpreter = CreateInterpreter(CreateContract());
        var preparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());
        var revisionBefore = interpreter.MutableState.StateRevision;

        var adoption = await interpreter.CommitAdditionalFieldProposalAsync(preparation.CommitToken!);

        Assert.Equal(FormAdditionalFieldValueAdoptionStatus.Applied, adoption.Status);
        Assert.Equal(revisionBefore + 1, interpreter.MutableState.StateRevision);
        Assert.Equal(interpreter.MutableState.StateRevision, adoption.CommitRevision);
        Assert.Equal("Berlin", (await interpreter.GetValuesSnapshotAsync())["city"]);
    }

    [Fact]
    public async Task Commit_WhenReplayed_SecondAttemptReturnsUnavailableAndWritesNothingMore()
    {
        var interpreter = CreateInterpreter(CreateContract());
        var preparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());
        var firstAdoption = await interpreter.CommitAdditionalFieldProposalAsync(preparation.CommitToken!);
        var revisionAfterFirst = interpreter.MutableState.StateRevision;

        var secondAdoption = await interpreter.CommitAdditionalFieldProposalAsync(preparation.CommitToken!);

        Assert.Equal(FormAdditionalFieldValueAdoptionStatus.Applied, firstAdoption.Status);
        Assert.Equal(FormAdditionalFieldValueAdoptionStatus.Unavailable, secondAdoption.Status);
        Assert.Equal(revisionAfterFirst, interpreter.MutableState.StateRevision);
    }

    [Fact]
    public async Task Commit_AfterAbandon_ReturnsUnavailableAndLeavesTheFormUnchanged()
    {
        var interpreter = CreateInterpreter(CreateContract());
        var preparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());
        var revisionBefore = interpreter.MutableState.StateRevision;

        await interpreter.AbandonAdditionalFieldProposalAsync(preparation.CommitToken!);
        var adoption = await interpreter.CommitAdditionalFieldProposalAsync(preparation.CommitToken!);

        Assert.Equal(FormAdditionalFieldValueAdoptionStatus.Unavailable, adoption.Status);
        Assert.Equal(revisionBefore, interpreter.MutableState.StateRevision);
        Assert.False((await interpreter.GetValuesSnapshotAsync()).ContainsKey("city"));
    }

    [Fact]
    public async Task Abandon_IsRepeatableAndLeavesStateRevisionUnchanged()
    {
        var interpreter = CreateInterpreter(CreateContract());
        var preparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());
        var revisionBefore = interpreter.MutableState.StateRevision;

        await interpreter.AbandonAdditionalFieldProposalAsync(preparation.CommitToken!);
        await interpreter.AbandonAdditionalFieldProposalAsync(preparation.CommitToken!);
        await interpreter.AbandonAdditionalFieldProposalAsync(
            new FormAdditionalFieldProposalCommitToken("never-issued"));

        Assert.Equal(revisionBefore, interpreter.MutableState.StateRevision);
    }

    [Fact]
    public async Task Commit_WhenTheFormChangedAfterThePreparation_ReturnsUnavailableAndWritesNothing()
    {
        var interpreter = CreateInterpreter(CreateContract());
        var preparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());

        // Die Eingabe im aktuellen Feld erhöht die Zustandsrevision.
        // Der zuvor vorbereitete Vorschlag bezieht sich damit auf einen veralteten Stand.
        Assert.True((await interpreter.TryApplyTranscriptAsync("Max Mustermann")).ValueCaptured);
        var revisionBefore = interpreter.MutableState.StateRevision;

        var adoption = await interpreter.CommitAdditionalFieldProposalAsync(preparation.CommitToken!);

        Assert.Equal(FormAdditionalFieldValueAdoptionStatus.Unavailable, adoption.Status);
        Assert.Equal(revisionBefore, interpreter.MutableState.StateRevision);
        Assert.False((await interpreter.GetValuesSnapshotAsync()).ContainsKey("city"));
    }

    [Fact]
    public async Task Commit_WhenTwoCandidatesAreAppliedInSequence_SecondUsesTheFirstRevisionAsBaseline()
    {
        var interpreter = CreateInterpreter(CreateContract());
        var cityPreparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());
        var cityAdoption = await interpreter.CommitAdditionalFieldProposalAsync(cityPreparation.CommitToken!);
        var revisionAfterCity = interpreter.MutableState.StateRevision;

        var notePreparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateNoteCandidate());
        var noteAdoption = await interpreter.CommitAdditionalFieldProposalAsync(notePreparation.CommitToken!);

        Assert.Equal(FormAdditionalFieldValueAdoptionStatus.Applied, cityAdoption.Status);
        Assert.Equal(FormAdditionalFieldValueAdoptionStatus.Applied, noteAdoption.Status);
        Assert.Equal(revisionAfterCity + 1, interpreter.MutableState.StateRevision);
        Assert.Equal(interpreter.MutableState.StateRevision, noteAdoption.CommitRevision);
        var values = await interpreter.GetValuesSnapshotAsync();
        Assert.Equal("Berlin", values["city"]);
        Assert.Equal("Park", values["note"]);
        Assert.Equal("nameField", await interpreter.GetFocusedFieldIdAsync());
    }

    [Fact]
    public async Task Commit_WhenSecondCandidateWasPreparedAgainstTheSameRevision_ReturnsUnavailableAfterTheFirstApply()
    {
        var interpreter = CreateInterpreter(CreateContract());
        var cityPreparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());
        var notePreparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateNoteCandidate());
        var revisionBefore = interpreter.MutableState.StateRevision;

        var cityAdoption = await interpreter.CommitAdditionalFieldProposalAsync(cityPreparation.CommitToken!);
        var noteAdoption = await interpreter.CommitAdditionalFieldProposalAsync(notePreparation.CommitToken!);

        Assert.Equal(FormAdditionalFieldValueAdoptionStatus.Applied, cityAdoption.Status);
        Assert.Equal(FormAdditionalFieldValueAdoptionStatus.Unavailable, noteAdoption.Status);
        Assert.Equal(revisionBefore + 1, interpreter.MutableState.StateRevision);
        var values = await interpreter.GetValuesSnapshotAsync();
        Assert.Equal("Berlin", values["city"]);
        Assert.False(values.ContainsKey("note"));
        Assert.Equal("nameField", await interpreter.GetFocusedFieldIdAsync());
    }

    [Fact]
    public async Task Commit_WhenValidationRejectsTheValue_LeavesValuesFocusAndStateRevisionUnchanged()
    {
        var interpreter = CreateInterpreter(CreateContract(constrainCity: true));
        var preparation = await interpreter.PrepareAdditionalFieldProposalAsync(CreateCityCandidate());
        var revisionBefore = interpreter.MutableState.StateRevision;
        var valuesBefore = await interpreter.GetValuesSnapshotAsync();
        var focusedBefore = await interpreter.GetFocusedFieldIdAsync();

        var adoption = await interpreter.CommitAdditionalFieldProposalAsync(preparation.CommitToken!);

        Assert.Equal(FormAdditionalFieldValueAdoptionStatus.BlockedByValidation, adoption.Status);
        Assert.Equal(0, adoption.CommitRevision);
        Assert.Equal(revisionBefore, interpreter.MutableState.StateRevision);
        Assert.Equal(valuesBefore, await interpreter.GetValuesSnapshotAsync());
        Assert.Equal(focusedBefore, await interpreter.GetFocusedFieldIdAsync());
        Assert.Empty(await interpreter.ValidateAllAsync());
    }

    private static FormInterpreterRuntimeOwner CreateInterpreter(FormContract contract)
        => new(contract, "de", ImmutableDictionary<string, string>.Empty);

    private static FormAdditionalFieldValueCandidate CreateCityCandidate()
        => new(
            FieldId.From("cityField"),
            "Stadt",
            "Berlin",
            "Berlin",
            ValueTypeKinds.String,
            []);

    private static FormAdditionalFieldValueCandidate CreateNoteCandidate()
        => new(
            FieldId.From("noteField"),
            "Notiz",
            "Park",
            "Park",
            ValueTypeKinds.String,
            []);

    private static FormContract CreateContract(
        FieldEditability cityEditability = FieldEditability.UserEditable,
        bool constrainCity = false,
        bool cityIsInteger = false,
        bool forbidCityVoiceInput = false,
        bool protectCityReadback = false)
    {
        var stringValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement;
        var integerValueType = JsonDocument.Parse("{\"kind\":\"integer\"}").RootElement;

        var cityVariable = new VariableDefinition
        {
            VariableId = VariableId.From("city"),
            Name = "city",
            ValueType = cityIsInteger ? integerValueType : stringValueType,
            PrivacyClass = protectCityReadback ? PrivacyClass.Sensitive : null,
            SensitiveDataPolicy = forbidCityVoiceInput
                ? new SensitiveDataPolicy
                {
                    VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputForbidden,
                    SilentInputFallback = SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard
                }
                : protectCityReadback
                    ? new SensitiveDataPolicy { ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackNever }
                    : null
        };

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("additionalField"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition { VariableId = VariableId.From("name"), Name = "name", ValueType = stringValueType },
                cityVariable,
                new VariableDefinition { VariableId = VariableId.From("note"), Name = "note", ValueType = stringValueType }
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
