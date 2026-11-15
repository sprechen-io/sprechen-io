using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Preflight;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests.Preflight.Validators;

public sealed class FormPreflightValidatorTests
{
    [Fact]
    public void FormIdentityValidator_BuildsIndexForValidContract()
    {
        var errors = new List<PreflightIssue>();

        var index = new FormIdentityValidator().Validate(CreateContract(), errors);

        Assert.Empty(errors);
        Assert.True(index.VariablesById.ContainsKey("name"));
        Assert.True(index.FieldsById.ContainsKey("nameField"));
        Assert.True(index.FieldGroupsById.ContainsKey("main"));
    }

    [Fact]
    public void FormIdentityValidator_ReturnsDuplicateIdError()
    {
        var contract = CreateContract();
        contract.Fields.Add(new FieldDefinition
        {
            FieldId = FieldId.From("nameField"),
            VariableId = VariableId.From("name"),
            Label = new LocalizedText { ["de"] = "Name 2" }
        });
        var errors = new List<PreflightIssue>();

        _ = new FormIdentityValidator().Validate(contract, errors);

        Assert.Contains(errors, issue => issue.Code == PreflightCodes.ErrorIdDuplicate && issue.Id == "nameField");
    }

    [Fact]
    public void FormSupportedSubsetValidator_AllowsSupportedSubset()
    {
        var contract = CreateContract();
        var errors = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        CreateSupportedSubsetValidator().Validate(contract, index, errors);

        Assert.Empty(errors);
    }

    [Fact]
    public void FormSupportedSubsetValidator_ReturnsUnsupportedInputModeError()
    {
        var contract = CreateContract();
        contract.Fields[0].PreferredInputModes = ["telepathy"];
        var errors = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        CreateSupportedSubsetValidator().Validate(contract, index, errors);

        Assert.Contains(errors, issue => issue.Code == PreflightCodes.ErrorFeatureNotSupported && issue.Id == "nameField");
    }

    [Fact]
    public void FormReferenceConsistencyValidator_AllowsResolvedReferences()
    {
        var contract = CreateContract();
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        CreateReferenceConsistencyValidator().Validate(contract, index, errors, warnings);

        Assert.Empty(errors);
        Assert.Empty(warnings);
    }

    [Fact]
    public void FormReferenceConsistencyValidator_ReturnsMissingVariableReference()
    {
        var contract = CreateContract();
        contract.Fields[0].VariableId = VariableId.From("missing");
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        CreateReferenceConsistencyValidator().Validate(contract, index, errors, warnings);

        Assert.Contains(errors, issue => issue.Code == PreflightCodes.ErrorRefMissing && issue.Id == "missing");
    }

    [Fact]
    public void FormFieldGroupGraphValidator_AllowsReachableAcyclicGraph()
    {
        var contract = CreateContract();
        var errors = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        new FormFieldGroupGraphValidator().Validate(contract.EntryFieldGroupId, index, errors);

        Assert.Empty(errors);
    }

    [Fact]
    public void FormFieldGroupGraphValidator_ReturnsCycleError()
    {
        var contract = CreateContract();
        contract.FieldGroups[0].Members.Add(new FieldGroupMember { Kind = "fieldGroup", FieldGroupId = FieldGroupId.From("main") });
        var errors = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        new FormFieldGroupGraphValidator().Validate(contract.EntryFieldGroupId, index, errors);

        Assert.Contains(errors, issue => issue.Code == PreflightCodes.ErrorFieldGroupCycle && issue.Id == "main");
    }

    [Fact]
    public void FormLocalizationCoverageValidator_AllowsCoveredLocalization()
    {
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();

        new FormLocalizationCoverageValidator().Validate(CreateContract(), errors, warnings);

        Assert.Empty(errors);
        Assert.Empty(warnings);
    }

    [Fact]
    public void FormLocalizationCoverageValidator_ReturnsMissingLocalizationAsErrorWhenStrict()
    {
        var contract = CreateContract();
        contract.Fields[0].Label.Clear();
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();

        new FormLocalizationCoverageValidator().Validate(contract, errors, warnings);

        Assert.Contains(errors, issue => issue.Code == PreflightCodes.ErrorLocalizationMissing && issue.Id == "nameField");
        Assert.Empty(warnings);
    }

    [Fact]
    public void FormLocalizationCoverageValidator_ReturnsMissingLocalizationAsWarningWhenNotStrict()
    {
        var contract = CreateContract();
        contract.StrictLocalization = false;
        contract.Fields[0].Label.Clear();
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();

        new FormLocalizationCoverageValidator().Validate(contract, errors, warnings);

        Assert.Empty(errors);
        Assert.Contains(warnings, issue => issue.Code == PreflightCodes.WarnLocalizationPartial && issue.Id == "nameField");
    }

    [Fact]
    public void FormPrivacyValidator_AllowsPublicVariable()
    {
        var contract = CreateContract();
        var errors = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        new FormPrivacyValidator().Validate(contract, index, errors);

        Assert.Empty(errors);
    }

    [Fact]
    public void FormPrivacyValidator_ReturnsSecretVoiceInputViolation()
    {
        var contract = CreateContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Secret;
        contract.Variables[0].SensitiveDataPolicy = new SensitiveDataPolicy { VoiceInputPolicy = "recommended" };
        var errors = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        new FormPrivacyValidator().Validate(contract, index, errors);

        Assert.Contains(errors, issue => issue.Code == PreflightCodes.ErrorPrivacyGuardrailViolation && issue.Id == "name");
    }

    [Fact]
    public void FormConstraintValidator_AllowsValidPatternConstraint()
    {
        var contract = CreateContract();
        contract.Constraints.Add(CreatePatternConstraint("[a-z]+"));
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        CreateConstraintValidator().Validate(contract.Constraints[0], "$.constraints[0]", index.VariablesById, index.FieldsById, errors, warnings);

        Assert.Empty(errors);
        Assert.Empty(warnings);
    }

    [Fact]
    public void FormConstraintValidator_ReturnsInvalidPatternError()
    {
        var contract = CreateContract();
        contract.Constraints.Add(CreatePatternConstraint("("));
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        CreateConstraintValidator().Validate(contract.Constraints[0], "$.constraints[0]", index.VariablesById, index.FieldsById, errors, warnings);

        Assert.Contains(errors, issue => issue.Code == PreflightCodes.ErrorSettingsInvalid && issue.Path == "$.constraints[0].pattern");
    }

    [Fact]
    public void FormRuleExpressionValidator_AllowsKnownVariableExpression()
    {
        var contract = CreateContract();
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        new FormRuleExpressionValidator().ValidateExpressionUnion(
            ParseJsonElement("""{"kind":"isSet","variableId":"name"}"""),
            "$.rules[0].when",
            index.VariablesById,
            errors,
            warnings);

        Assert.Empty(errors);
        Assert.Empty(warnings);
    }

    [Fact]
    public void FormRuleExpressionValidator_ReturnsUnknownVariableWarning()
    {
        var contract = CreateContract();
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        new FormRuleExpressionValidator().ValidateExpressionUnion(
            ParseJsonElement("""{"kind":"isSet","variableId":"missing"}"""),
            "$.rules[0].when",
            index.VariablesById,
            errors,
            warnings);

        Assert.Contains(warnings, issue => issue.Code == PreflightCodes.WarnExprVarUnknown && issue.Id == "missing");
    }

    [Fact]
    public void FormBindingValueTypeValidator_AllowsStringCodeStyleBinding()
    {
        var contract = CreateContract();
        contract.Fields[0].CodeStyle = true;
        var errors = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        new FormBindingValueTypeValidator().Validate(contract, index, errors);

        Assert.Empty(errors);
    }

    [Fact]
    public void FormBindingValueTypeValidator_ReturnsCodeStyleMismatch()
    {
        var contract = CreateContract(valueTypeJson: """{"kind":"integer"}""");
        contract.Fields[0].CodeStyle = true;
        var errors = new List<PreflightIssue>();
        var index = new FormIdentityValidator().Validate(contract, errors);

        new FormBindingValueTypeValidator().Validate(contract, index, errors);

        Assert.Contains(errors, issue => issue.Code == PreflightCodes.ErrorBindingTypeMismatch && issue.Id == "nameField");
    }

    private static FormContract CreateContract(string valueTypeJson = """{"kind":"string"}""")
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
                    ValueType = ParseJsonElement(valueTypeJson)
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("nameField"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
                    Editability = FieldEditability.UserEditable,
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

    private static ConstraintDefinition CreatePatternConstraint(string pattern)
    {
        return new ConstraintDefinition
        {
            ConstraintId = "constraint.pattern",
            Kind = ConstraintKind.Pattern,
            Scope = ConstraintScope.Field,
            Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("nameField") },
            Message = new LocalizedText { ["de"] = "Ungültig" },
            AdditionalProperties = new Dictionary<string, JsonElement>
            {
                ["pattern"] = ParseJsonElement(JsonSerializer.Serialize(pattern))
            }
        };
    }

    private static FormSupportedSubsetValidator CreateSupportedSubsetValidator()
    {
        return new FormSupportedSubsetValidator(CreateConstraintValidator());
    }

    private static FormReferenceConsistencyValidator CreateReferenceConsistencyValidator()
    {
        var ruleExpressionValidator = new FormRuleExpressionValidator();
        return new FormReferenceConsistencyValidator(new FormConstraintValidator(ruleExpressionValidator), ruleExpressionValidator);
    }

    private static FormConstraintValidator CreateConstraintValidator()
    {
        return new FormConstraintValidator(new FormRuleExpressionValidator());
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
