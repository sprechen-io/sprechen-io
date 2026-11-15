using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Localization;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class FormContractPreflightConstraintKindTests
{
    public static TheoryData<ConstraintDefinition> SupportedConstraintDefinitions => new()
    {
        CreateConstraint(ConstraintKind.Min, new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From("age") }, new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.Min] = ParseJsonElement("18")
        }),
        CreateConstraint(ConstraintKind.Max, new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From("age") }, new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.Max] = ParseJsonElement("65")
        }),
        CreateConstraint(ConstraintKind.Range, new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From("age") }, new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.Min] = ParseJsonElement("18"),
            [ConstraintPropertyNames.Max] = ParseJsonElement("65")
        }),
        CreateConstraint(ConstraintKind.DateMin, new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From("appointment") }, new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.MinDate] = ParseJsonElement("\"2026-04-01\"")
        }),
        CreateConstraint(ConstraintKind.DateMax, new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From("appointment") }, new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.MaxDate] = ParseJsonElement("\"2026-04-30\"")
        }),
        CreateConstraint(ConstraintKind.DateRange, new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From("appointment") }, new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.MinDate] = ParseJsonElement("\"2026-04-01\""),
            [ConstraintPropertyNames.MaxDate] = ParseJsonElement("\"2026-04-30\"")
        }),
        CreateConstraint(ConstraintKind.Equals, new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From("age") }, new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.Value] = ParseJsonElement("42")
        }),
        CreateConstraint(ConstraintKind.NotEquals, new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From("age") }, new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.Value] = ParseJsonElement("42")
        }),
        CreateConstraint(ConstraintKind.AllOrNone, new ConstraintTargetRef { Kind = TargetKinds.Form }, new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.VariableIds] = ParseJsonElement("[\"a\",\"b\",\"c\"]")
        }),
        CreateConstraint(ConstraintKind.Compare, new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From("age") }, new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.Operator] = ParseJsonElement("\"gte\""),
            [ConstraintPropertyNames.Value] = ParseJsonElement("18")
        }),
        CreateConstraint(ConstraintKind.CompareVariables, new ConstraintTargetRef { Kind = TargetKinds.Form }, new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.LeftVariableId] = ParseJsonElement("\"left\""),
            [ConstraintPropertyNames.RightVariableId] = ParseJsonElement("\"right\""),
            [ConstraintPropertyNames.Operator] = ParseJsonElement("\"gt\"")
        })
    };

    [Theory]
    [MemberData(nameof(SupportedConstraintDefinitions))]
    public void Validate_WhenConstraintKindIsNewlySupported_DoesNotEmitFeatureNotSupported(ConstraintDefinition constraint)
    {
        var contract = CreateMinimalContractWithConstraint(constraint);

        var result = FormContractPreflight.Validate(contract);

        Assert.DoesNotContain(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorFeatureNotSupported
            && issue.Id == constraint.ConstraintId);
    }

    [Fact]
    public void Validate_WhenConstraintKindIsCustomConstraint_EmitsFeatureNotSupported()
    {
        var contract = CreateMinimalContractWithConstraint(
            CreateConstraint(ConstraintKind.CustomConstraint, new ConstraintTargetRef { Kind = TargetKinds.Form }, new Dictionary<string, JsonElement>
            {
                [ConstraintPropertyNames.CustomConstraintName] = ParseJsonElement("\"customRule\"")
            }));

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorFeatureNotSupported
            && issue.Id == "test-constraint");
    }

    [Fact]
    public void Validate_WhenConstraintKindIsUnknownToken_ReportsFeatureNotSupported_WithoutThrowing()
    {
        // Das unbekannte JSON-Token wird als ConstraintKind.Unknown deserialisiert.
        // Die Vorprüfung soll es als Fehler melden, ohne die Deserialisierung abzubrechen.
        const string constraintJson = """
            {
                "constraintId": "test-constraint",
                "target": { "kind": "form" },
                "severity": "error",
                "code": "test.constraint",
                "message": { "de": "Constraint failed" },
                "kind": "totallyBogusKind"
            }
            """;

        ConstraintDefinition? deserialized = null;
        var exception = Record.Exception(() =>
            deserialized = JsonSerializer.Deserialize<ConstraintDefinition>(constraintJson));

        Assert.Null(exception);
        Assert.NotNull(deserialized);
        Assert.Equal(ConstraintKind.Unknown, deserialized!.Kind);

        var contract = CreateMinimalContractWithConstraint(deserialized);

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorFeatureNotSupported
            && issue.Id == "test-constraint"
            && issue.Path == "$.constraints[0].kind");
    }

    private static FormContract CreateMinimalContractWithConstraint(ConstraintDefinition constraint)
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
            StrictLocalization = false,
            Variables =
            [
                CreateVariable("age", "{\"kind\":\"integer\"}"),
                CreateVariable("appointment", "{\"kind\":\"date\"}"),
                CreateVariable("left", "{\"kind\":\"integer\"}"),
                CreateVariable("right", "{\"kind\":\"integer\"}"),
                CreateVariable("a", "{\"kind\":\"string\"}"),
                CreateVariable("b", "{\"kind\":\"string\"}"),
                CreateVariable("c", "{\"kind\":\"string\"}")
            ],
            Fields =
            [
                CreateField("ageField", "age", "Alter"),
                CreateField("appointmentField", "appointment", "Termin")
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("ageField") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("appointmentField") }
                    ]
                }
            ],
            Constraints = [constraint]
        };
    }

    private static VariableDefinition CreateVariable(string variableId, string valueTypeJson)
    {
        return new VariableDefinition
        {
            VariableId = VariableId.From(variableId),
            Name = variableId,
            ValueType = ParseJsonElement(valueTypeJson)
        };
    }

    private static FieldDefinition CreateField(string fieldId, string variableId, string label)
    {
        return new FieldDefinition
        {
            FieldId = FieldId.From(fieldId),
            VariableId = VariableId.From(variableId),
            Label = new LocalizedText { ["de"] = label },
            Editability = FieldEditability.UserEditable
        };
    }

    private static ConstraintDefinition CreateConstraint(ConstraintKind kind, ConstraintTargetRef target, Dictionary<string, JsonElement> additionalProperties)
    {
        return new ConstraintDefinition
        {
            ConstraintId = "test-constraint",
            Kind = kind,
            Target = target,
            Severity = ConstraintSeverity.Error,
            Code = "test.constraint",
            Message = new LocalizedText { ["de"] = "Constraint failed" },
            AdditionalProperties = additionalProperties
        };
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
