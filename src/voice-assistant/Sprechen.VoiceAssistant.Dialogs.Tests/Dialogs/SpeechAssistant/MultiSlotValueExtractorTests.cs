using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft zusätzliche Feldzuordnungen aus abgegrenzten Abschnitten mit Feldname und Wert.
/// Das aktuelle Feld, unpassende Namen und nicht auswertbare Werte werden ausgeschlossen.
/// </summary>
public sealed class MultiSlotValueExtractorTests
{
    [Fact]
    public void Extract_WhenDelimitedNonFocusedFieldNamed_ReturnsSingleCandidate()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), "de", ImmutableDictionary<string, string>.Empty);

        var candidates = interpreter.ExtractAdditionalFieldCandidates("Stadt: Berlin");

        var candidate = Assert.Single(candidates);
        Assert.Equal(FieldId.From("cityField"), candidate.FieldId);
        Assert.Equal("Stadt", candidate.FieldLabel);
        Assert.Equal("Berlin", candidate.ValueSegment);
        Assert.Equal("Berlin", candidate.CanonicalValue);
    }

    [Fact]
    public void Extract_WhenFocusedOnlyUtterance_ReturnsEmpty()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), "de", ImmutableDictionary<string, string>.Empty);

        Assert.Empty(interpreter.ExtractAdditionalFieldCandidates("Max Mustermann"));
    }

    [Fact]
    public void Extract_WhenFocusedValueContainsLabelLikeWord_ReturnsEmpty()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), "de", ImmutableDictionary<string, string>.Empty);
        Assert.True(interpreter.TrySetFocusToFieldIdAsync("noteField").GetAwaiter().GetResult());

        // Ein Feldname innerhalb eines freien Textes begründet noch keine zusätzliche Zuordnung.
        Assert.Empty(interpreter.ExtractAdditionalFieldCandidates("Stadt Park ist schoen"));
        // Auch vor einem Doppelpunkt muss der ganze Abschnitt dem Feldnamen entsprechen.
        Assert.Empty(interpreter.ExtractAdditionalFieldCandidates("Mein Stadt Park: schoen"));
    }

    [Fact]
    public void Extract_WhenMultipleDelimitedFieldsNamed_ReturnsCandidatePerField()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), "de", ImmutableDictionary<string, string>.Empty);

        var candidates = interpreter.ExtractAdditionalFieldCandidates("Stadt: Berlin; PLZ: 10115");

        Assert.Equal(2, candidates.Length);
        Assert.Equal(FieldId.From("cityField"), candidates[0].FieldId);
        Assert.Equal("Berlin", candidates[0].ValueSegment);
        Assert.Equal(FieldId.From("postalCodeField"), candidates[1].FieldId);
        Assert.Equal("10115", candidates[1].ValueSegment);
    }

    [Fact]
    public void Extract_WhenNamedFieldIsTheFocusedField_ReturnsEmpty()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), "de", ImmutableDictionary<string, string>.Empty);
        Assert.True(interpreter.TrySetFocusToFieldIdAsync("cityField").GetAwaiter().GetResult());

        Assert.Empty(interpreter.ExtractAdditionalFieldCandidates("Stadt: Berlin"));
    }

    [Fact]
    public void Extract_WhenValueDoesNotParseForFieldType_ReturnsEmpty()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), "de", ImmutableDictionary<string, string>.Empty);

        Assert.Empty(interpreter.ExtractAdditionalFieldCandidates("Geburtstag: kein datum"));
    }

    private static FormContract CreateContract()
    {
        var stringValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement;
        var dateValueType = JsonDocument.Parse("{\"kind\":\"date\"}").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test",
            FormId = FormId.From("multislot"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition { VariableId = VariableId.From("name"), Name = "name", ValueType = stringValueType },
                new VariableDefinition { VariableId = VariableId.From("city"), Name = "city", ValueType = stringValueType },
                new VariableDefinition { VariableId = VariableId.From("postalCode"), Name = "postalCode", ValueType = stringValueType },
                new VariableDefinition { VariableId = VariableId.From("note"), Name = "note", ValueType = stringValueType },
                new VariableDefinition { VariableId = VariableId.From("birthday"), Name = "birthday", ValueType = dateValueType }
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
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("postalCodeField"),
                    VariableId = VariableId.From("postalCode"),
                    Label = new LocalizedText { ["de"] = "PLZ" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("noteField"),
                    VariableId = VariableId.From("note"),
                    Label = new LocalizedText { ["de"] = "Notiz" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("birthdayField"),
                    VariableId = VariableId.From("birthday"),
                    Label = new LocalizedText { ["de"] = "Geburtstag" },
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
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("postalCodeField") },
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("noteField") },
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("birthdayField") }
                    ]
                }
            ]
        };
    }
}
