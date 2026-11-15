using System.Text;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Preflight;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests.Preflight.Snapshots;

public sealed class FormContractPreflightSnapshotTests
{
    [Fact]
    public void Validate_RepresentativeFixtureCorpus_MatchesDiagnosticSnapshot()
    {
        var snapshot = BuildSnapshot(
            ("valid", CreateContract()),
            ("duplicate-and-missing-reference", CreateDuplicateAndMissingReferenceContract()),
            ("unsupported-input-mode", CreateUnsupportedInputModeContract()),
            ("binding-mismatch", CreateBindingMismatchContract()),
            ("invalid-pattern-constraint", CreateInvalidPatternConstraintContract()),
            ("graph-and-expression", CreateGraphAndExpressionContract()),
            ("localization-warning", CreateLocalizationWarningContract()),
            ("privacy-guardrail", CreatePrivacyGuardrailContract()),
            ("voice-profile", CreateVoiceProfileContract()));

        const string expected = """
        ## valid
        ok=True
        errors=0
        warnings=0

        ## duplicate-and-missing-reference
        ok=False
        errors=2
        error|E_ID_DUPLICATE|$.fields[1].fieldId|nameField|Duplicate fieldId 'nameField'.
        error|E_REF_MISSING|$.fields[1].variableId|missing|Field 'nameField' references missing variableId 'missing'.
        warnings=0

        ## unsupported-input-mode
        ok=False
        errors=1
        error|E_FEATURE_NOT_SUPPORTED|$.fields[0].preferredInputModes[0]|nameField|Unsupported preferredInputMode 'telepathy' in field 'nameField'.
        warnings=0

        ## binding-mismatch
        ok=False
        errors=1
        error|E_BINDING_TYPE_MISMATCH|$.fields[0]|nameField|codeStyle requires valueType 'string', but field 'nameField' is bound to 'integer'.
        warnings=0

        ## invalid-pattern-constraint
        ok=False
        errors=1
        error|E_SETTINGS_INVALID|$.constraints[0].pattern|constraint.pattern|Pattern constraint 'constraint.pattern' contains an invalid regular expression.
        warnings=0

        ## graph-and-expression
        ok=False
        errors=1
        error|E_FIELD_GROUP_CYCLE|$.fieldGroups[*].members|main|Field-group cycle detected at 'main'.
        warnings=1
        warning|W_EXPR_VAR_UNKNOWN|$.rules[0].when.variableId|missing|Expression references unknown variableId 'missing'.

        ## localization-warning
        ok=True
        errors=0
        warnings=1
        warning|W_LOCALIZATION_PARTIAL|$.fields[0].label|nameField|Missing localization for language 'de' in field 'nameField' label.

        ## privacy-guardrail
        ok=False
        errors=1
        error|E_PRIVACY_GUARDRAIL_VIOLATION|$.variables[*].sensitiveDataPolicy.voiceInputPolicy|name|Variable 'name' is privacyClass=secret but voiceInputPolicy='recommended' is not allowed.
        warnings=0

        ## voice-profile
        ok=False
        errors=1
        error|E_SETTINGS_INVALID|$.projections.voice.formVoiceProfile.noInputTimeoutMs|profile.main|noInputTimeoutMs must be >= 0, got -1.
        warnings=0


        """;

        Assert.Equal(Normalize(expected), snapshot);
    }

    private static string BuildSnapshot(params (string Name, FormContract Contract)[] fixtures)
    {
        var builder = new StringBuilder();
        foreach (var (name, contract) in fixtures)
        {
            var result = FormContractPreflight.Validate(contract);
            builder.AppendLine($"## {name}");
            builder.AppendLine($"ok={result.Ok}");
            builder.AppendLine($"errors={result.Errors.Count}");
            foreach (var issue in result.Errors)
            {
                builder.AppendLine(FormatIssue("error", issue));
            }

            builder.AppendLine($"warnings={result.Warnings.Count}");
            foreach (var issue in result.Warnings)
            {
                builder.AppendLine(FormatIssue("warning", issue));
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string FormatIssue(string severity, PreflightIssue issue)
    {
        return string.Join(
            "|",
            severity,
            issue.Code,
            issue.Path,
            issue.Id,
            issue.Message);
    }

    private static FormContract CreateDuplicateAndMissingReferenceContract()
    {
        var contract = CreateContract();
        contract.Fields.Add(new FieldDefinition
        {
            FieldId = FieldId.From("nameField"),
            VariableId = VariableId.From("missing"),
            Label = new LocalizedText { ["de"] = "Missing" },
            Editability = FieldEditability.UserEditable,
        });
        contract.FieldGroups[0].Members.Add(new FieldGroupMember { Kind = "field", FieldId = FieldId.From("nameField") });

        return contract;
    }

    private static FormContract CreateGraphAndExpressionContract()
    {
        var contract = CreateContract();
        contract.FieldGroups[0].Members.Add(new FieldGroupMember { Kind = "fieldGroup", FieldGroupId = FieldGroupId.From("main") });
        contract.Rules.Add(new RuleDefinition
        {
            RuleId = "rule.missing-variable",
            Scope = "onValueChanged",
            When = ParseJsonElement("""{"kind":"isSet","variableId":"missing"}"""),
            Then = []
        });

        return contract;
    }

    private static FormContract CreateUnsupportedInputModeContract()
    {
        var contract = CreateContract();
        contract.Fields[0].PreferredInputModes = ["telepathy"];

        return contract;
    }

    private static FormContract CreateBindingMismatchContract()
    {
        var contract = CreateContract(valueTypeJson: """{"kind":"integer"}""");
        contract.Fields[0].CodeStyle = true;

        return contract;
    }

    private static FormContract CreateInvalidPatternConstraintContract()
    {
        var contract = CreateContract();
        contract.Constraints.Add(new ConstraintDefinition
        {
            ConstraintId = "constraint.pattern",
            Kind = ConstraintKind.Pattern,
            Scope = ConstraintScope.Field,
            Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("nameField") },
            Message = new LocalizedText { ["de"] = "Invalid" },
            AdditionalProperties = new Dictionary<string, JsonElement>
            {
                ["pattern"] = ParseJsonElement(JsonSerializer.Serialize("("))
            }
        });

        return contract;
    }

    private static FormContract CreateLocalizationWarningContract()
    {
        var contract = CreateContract();
        contract.StrictLocalization = false;
        contract.Fields[0].Label.Clear();

        return contract;
    }

    private static FormContract CreatePrivacyGuardrailContract()
    {
        var contract = CreateContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Secret;
        contract.Variables[0].SensitiveDataPolicy = new SensitiveDataPolicy { VoiceInputPolicy = "recommended" };

        return contract;
    }

    private static FormContract CreateVoiceProfileContract()
    {
        var contract = CreateContract();
        contract.Projections = new ContractProjections
        {
            Voice = new VoiceProjection
            {
                FormVoiceProfile = new FormVoiceProfile
                {
                    FormVoiceProfileId = "profile.main",
                    NoInputTimeoutMs = -1
                }
            }
        };

        return contract;
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

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static string Normalize(string value)
    {
        return value.ReplaceLineEndings(Environment.NewLine);
    }
}
