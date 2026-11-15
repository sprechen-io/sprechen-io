using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class FormContractPreflightConstraintScopeTests
{
    [Fact]
    public void Validate_WhenConstraintScopeUnsupported_ReturnsFeatureNotSupportedError()
    {
        // Das unbekannte JSON-Token wird als ConstraintScope.Unknown deserialisiert.
        // Die Vorprüfung soll es als Fehler melden, ohne die Deserialisierung abzubrechen.
        const string constraintJson = """
            {
                "constraintId": "requiredName",
                "target": { "kind": "field", "fieldId": "nameField" },
                "severity": "error",
                "code": "form.required",
                "message": { "de": "Name ist erforderlich." },
                "kind": "required",
                "scope": "groupOnly"
            }
            """;

        ConstraintDefinition? deserialized = null;
        var exception = Record.Exception(() =>
            deserialized = JsonSerializer.Deserialize<ConstraintDefinition>(constraintJson));

        Assert.Null(exception);
        Assert.NotNull(deserialized);
        Assert.Equal(ConstraintScope.Unknown, deserialized!.Scope);

        var contract = CreateMinimalContract();
        contract.Constraints[0] = deserialized;

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorFeatureNotSupported
            && issue.Path == "$.constraints[0].scope");
    }

    [Fact]
    public void Validate_WhenConstraintScopeCrossGroup_IsAccepted()
    {
        var contract = CreateMinimalContract();
        contract.Constraints[0].Scope = ConstraintScope.CrossGroup;

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
    }

    private static FormContract CreateMinimalContract()
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
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
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
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "requiredName",
                    Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("nameField") },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.required",
                    Message = new LocalizedText { ["de"] = "Name ist erforderlich." },
                    Kind = ConstraintKind.Required
                }
            ]
        };
    }
}


