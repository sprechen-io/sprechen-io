using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Preflight;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests.Preflight.Validators;

public sealed class FormConstraintValidatorTests
{
    [Fact]
    public void ValidateSupportedConstraint_WhenJsonScopeUnsupported_ReturnsScopeError()
    {
        var errors = new List<PreflightIssue>();

        CreateValidator().ValidateSupportedConstraint(
            ParseJsonElement("""{"kind":"required","scope":"unknown"}"""),
            "$.variables[0].invariantConstraints[0]",
            errors);

        Assert.Contains(errors, issue =>
            issue.Code == PreflightCodes.ErrorFeatureNotSupported
            && issue.Path == "$.variables[0].invariantConstraints[0].scope");
    }

    [Fact]
    public void FormTargetedCrossFieldConstraint_EmitsDuplicationWarning()
    {
        // Formularweit verankerte feldübergreifende Regeln können Meldungen
        // vervielfachen. Die Vorprüfung warnt deshalb und empfiehlt einen Feld-
        // oder Variablenbezug für eine eindeutigere Fehlerzuordnung.
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();
        var constraint = new ConstraintDefinition
        {
            ConstraintId = "endAfterStart",
            Kind = ConstraintKind.CompareVariables,
            Target = new ConstraintTargetRef { Kind = "form" },
            AdditionalProperties = new Dictionary<string, JsonElement>
            {
                ["leftVariableId"] = ParseJsonElement("\"endDate\""),
                ["rightVariableId"] = ParseJsonElement("\"startDate\""),
                ["operator"] = ParseJsonElement("\"gte\"")
            }
        };

        CreateValidator().Validate(
            constraint,
            "$.constraints[0]",
            variablesById: new Dictionary<string, VariableDefinition>(),
            fieldsById: new Dictionary<string, FieldDefinition>(),
            errors,
            warnings);

        Assert.Contains(warnings, issue =>
            issue.Code == PreflightCodes.WarnCrossFieldFormTarget
            && issue.Id == "endAfterStart");
    }

    [Fact]
    public void FieldTargetedCrossFieldConstraint_EmitsNoDuplicationWarning()
    {
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();
        var constraint = new ConstraintDefinition
        {
            ConstraintId = "endAfterStart",
            Kind = ConstraintKind.CompareVariables,
            Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("endDateField") },
            AdditionalProperties = new Dictionary<string, JsonElement>
            {
                ["leftVariableId"] = ParseJsonElement("\"endDate\""),
                ["rightVariableId"] = ParseJsonElement("\"startDate\""),
                ["operator"] = ParseJsonElement("\"gte\"")
            }
        };
        var fieldsById = new Dictionary<string, FieldDefinition>
        {
            ["endDateField"] = new() { FieldId = FieldId.From("endDateField"), VariableId = VariableId.From("endDate") }
        };

        CreateValidator().Validate(
            constraint,
            "$.constraints[0]",
            variablesById: new Dictionary<string, VariableDefinition>(),
            fieldsById,
            errors,
            warnings);

        Assert.DoesNotContain(warnings, issue => issue.Code == PreflightCodes.WarnCrossFieldFormTarget);
    }

    [Fact]
    public void Validate_WhenJsonFieldTargetMissing_ReturnsReferenceError()
    {
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();

        CreateValidator().Validate(
            ParseJsonElement("""{"kind":"required","target":{"kind":"field","fieldId":"missing"}}"""),
            "$.constraints[0]",
            variablesById: new Dictionary<string, VariableDefinition>(),
            fieldsById: new Dictionary<string, FieldDefinition>(),
            errors,
            warnings);

        Assert.Contains(errors, issue =>
            issue.Code == PreflightCodes.ErrorRefMissing
            && issue.Path == "$.constraints[0].target.fieldId"
            && issue.Id == "missing");
    }

    [Fact]
    public void Validate_WhenTypedOneOfValuesContainNull_ReturnsNullForbiddenError()
    {
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();
        var constraint = new ConstraintDefinition
        {
            ConstraintId = "constraint.oneOf",
            Kind = ConstraintKind.OneOf,
            Scope = ConstraintScope.Field,
            Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("nameField") },
            AdditionalProperties = new Dictionary<string, JsonElement>
            {
                ["values"] = ParseJsonElement("""["A", null]""")
            }
        };
        var fieldsById = new Dictionary<string, FieldDefinition>
        {
            ["nameField"] = new() { FieldId = FieldId.From("nameField"), VariableId = VariableId.From("name") }
        };

        CreateValidator().Validate(
            constraint,
            "$.constraints[0]",
            variablesById: new Dictionary<string, VariableDefinition>(),
            fieldsById,
            errors,
            warnings);

        Assert.Contains(errors, issue =>
            issue.Code == PreflightCodes.ErrorNullForbidden
            && issue.Path == "$.constraints[0].values[1]"
            && issue.Id == "constraint.oneOf");
    }

    [Fact]
    public void ValidateSupportedConstraint_WhenJsonOneOfValuesContainNull_ReturnsNullForbiddenError()
    {
        var errors = new List<PreflightIssue>();

        CreateValidator().ValidateSupportedConstraint(
            ParseJsonElement("""{"kind":"oneOf","values":["A", null]}"""),
            "$.variables[0].invariantConstraints[0]",
            errors);

        Assert.Contains(errors, issue =>
            issue.Code == PreflightCodes.ErrorNullForbidden
            && issue.Path == "$.variables[0].invariantConstraints[0].values[1]"
            && issue.Id is null);
    }

    private static FormConstraintValidator CreateValidator()
        => new(new FormRuleExpressionValidator());

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
