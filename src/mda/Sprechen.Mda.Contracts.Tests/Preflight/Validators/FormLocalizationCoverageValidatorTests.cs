using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Preflight;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests.Preflight.Validators;

public sealed class FormLocalizationCoverageValidatorTests
{
    [Fact]
    public void Validate_WhenFieldConstraintMessageMissesLanguage_ReturnsConstraintMessagePath()
    {
        var contract = CreateContract();
        contract.Fields[0].FieldConstraints =
        [
            new ConstraintDefinition
            {
                ConstraintId = "constraint.field",
                Kind = ConstraintKind.Required,
                Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("nameField") },
                Message = new LocalizedText { ["de"] = "Name ist erforderlich." }
            }
        ];
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();

        new FormLocalizationCoverageValidator().Validate(contract, errors, warnings);

        Assert.Contains(errors, issue =>
            issue.Code == PreflightCodes.ErrorLocalizationMissing
            && issue.Path == "$.fields[0].fieldConstraints[0].message"
            && issue.Id == "constraint.field");
    }

    [Fact]
    public void Validate_WhenVariableInvariantConstraintMissesLanguage_ReturnsInvariantConstraintMessagePath()
    {
        var contract = CreateContract();
        contract.Variables[0].InvariantConstraints =
        [
            ParseJsonElement(
                """
                {
                  "kind": "required",
                  "target": { "kind": "variable", "variableId": "name" },
                  "message": { "de": "Name ist erforderlich." }
                }
                """)
        ];
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();

        new FormLocalizationCoverageValidator().Validate(contract, errors, warnings);

        Assert.Contains(errors, issue =>
            issue.Code == PreflightCodes.ErrorLocalizationMissing
            && issue.Path == "$.variables[0].invariantConstraints[0].message"
            && issue.Id == "name");
    }

    [Fact]
    public void Validate_WhenVoicePromptMissesLanguage_ReturnsVoicePromptPath()
    {
        var contract = CreateContract();
        contract.Projections = new ContractProjections
        {
            Voice = new VoiceProjection
            {
                Hints = new VoiceHintsDefinition
                {
                    Prompts =
                    [
                        new VoicePrompt
                        {
                            PromptId = "prompt.main",
                            Text = new LocalizedText { ["de"] = "Hallo" }
                        }
                    ]
                }
            }
        };
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();

        new FormLocalizationCoverageValidator().Validate(contract, errors, warnings);

        Assert.Contains(errors, issue =>
            issue.Code == PreflightCodes.ErrorLocalizationMissing
            && issue.Path == "$.projections.voice.hints.prompts[0].text"
            && issue.Id == "prompt.main");
    }

    private static FormContract CreateContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test-app",
            FormId = FormId.From("test-form"),
            SupportedLanguages = ["de", "en"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "Name",
                    Description = Text("Name", "Name"),
                    ValueType = ParseJsonElement("""{"kind":"string"}""")
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("nameField"),
                    VariableId = VariableId.From("name"),
                    Label = Text("Name", "Name"),
                    HelpText = Text("Hilfe", "Help"),
                    Placeholder = Text("Name", "Name")
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = Text("Hauptgruppe", "Main group"),
                    Description = Text("Beschreibung", "Description"),
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("nameField") }]
                }
            ]
        };
    }

    private static LocalizedText Text(string de, string en)
        => new() { ["de"] = de, ["en"] = en };

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
