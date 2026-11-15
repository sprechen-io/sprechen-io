using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Preflight;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests.Preflight.Validators;

public sealed class FormContractPreflightRuleOwnerTests
{
    [Fact]
    public void Validate_WhenVoicePromptsDuplicateIds_ReturnsDuplicatePromptId()
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
                        },
                        new VoicePrompt
                        {
                            PromptId = "prompt.main",
                            Text = new LocalizedText { ["de"] = "Noch einmal" }
                        }
                    ]
                }
            }
        };

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorIdDuplicate
            && issue.Path == "$.projections.voice.hints.prompts[1].promptId"
            && issue.Id == "prompt.main");
    }

    [Fact]
    public void Validate_WhenFieldGroupMemberKindUnsupported_ReturnsFeatureNotSupported()
    {
        var contract = CreateContract();
        contract.FieldGroups[0].Members.Add(new FieldGroupMember { Kind = "section", FieldId = FieldId.From("nameField") });

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorFeatureNotSupported
            && issue.Path == "$.fieldGroups[0].members[1].kind"
            && issue.Id == "main");
    }

    [Fact]
    public void Validate_WhenCopyValueActionReferencesUnknownVariables_ReturnsBothMissingRefs()
    {
        var contract = CreateContract();
        contract.Rules =
        [
            new RuleDefinition
            {
                RuleId = "copy-rule",
                Scope = "onInit",
                When = ParseJsonElement("""{"kind":"isSet","variableId":"name"}"""),
                Then =
                [
                    ParseJsonElement("""{"kind":"copyValue","fromVariableId":"unknownFrom","toVariableId":"unknownTo"}""")
                ]
            }
        ];

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorRefMissing
            && issue.Path == "$.rules[0].then[0].fromVariableId"
            && issue.Id == "unknownFrom");
        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorRefMissing
            && issue.Path == "$.rules[0].then[0].toVariableId"
            && issue.Id == "unknownTo");
    }

    [Fact]
    public void Validate_WhenVariableInitialValueIsNull_ReturnsNullForbidden()
    {
        var contract = CreateContract();
        contract.Variables[0].InitialValue = ParseJsonElement("null");

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorNullForbidden
            && issue.Path == "$.variables[0].initialValue"
            && issue.Id == "name");
    }

    [Fact]
    public void Validate_WhenRuleScopeUnsupported_ReturnsFeatureNotSupported()
    {
        var contract = CreateContract();
        contract.Rules =
        [
            new RuleDefinition
            {
                RuleId = "scope-rule",
                Scope = "perRender",
                When = ParseJsonElement("""{"kind":"isSet","variableId":"name"}""")
            }
        ];

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorFeatureNotSupported
            && issue.Path == "$.rules[0].scope"
            && issue.Id == "scope-rule");
    }

    [Fact]
    public void Validate_WhenConstValueExpressionIsNull_ReturnsNullForbidden()
    {
        var contract = CreateContract();
        contract.Rules =
        [
            new RuleDefinition
            {
                RuleId = "const-rule",
                Scope = "onInit",
                When = ParseJsonElement(
                    """{"kind":"equals","left":{"kind":"const","value":null},"right":{"kind":"const","value":"x"}}""")
            }
        ];

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorNullForbidden
            && issue.Path == "$.rules[0].when.left.value");
    }

    [Fact]
    public void Validate_WhenValueExpressionReferencesUnknownVariable_ReturnsWarning()
    {
        var contract = CreateContract();
        contract.Rules =
        [
            new RuleDefinition
            {
                RuleId = "value-ref-rule",
                Scope = "onInit",
                When = ParseJsonElement(
                    """{"kind":"equals","left":{"kind":"var","variableId":"unknown"},"right":{"kind":"const","value":"x"}}""")
            }
        ];

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Warnings, issue =>
            issue.Code == PreflightCodes.WarnExprVarUnknown
            && issue.Path == "$.rules[0].when.left.variableId"
            && issue.Id == "unknown");
    }

    private static FormContract CreateContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test-app",
            FormId = FormId.From("test-form"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "Name",
                    ValueType = ParseJsonElement("""{"kind":"string"}""")
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("nameField"),
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
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("nameField") }]
                }
            ]
        };
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
