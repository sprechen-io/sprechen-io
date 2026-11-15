using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Variables;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class FormContractResolverTests
{
    [Fact]
    public void Resolve_WhenReusableConstraintTargetsOwnFieldDefinition_RewritesConstraintTargetToLocalFieldId()
    {
        var document = new FormContractDocument
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "sample.checkout",
            FormId = FormId.From("address-demo"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("entry"),
            StrictLocalization = false,
            FieldDefinitions = new FieldDefinitionsReference
            {
                FieldDefinitionsId = "sample.shared.fields",
                CatalogVersion = "1"
            },
            VariableDefinitions = new VariableDefinitionsReference
            {
                VariableDefinitionsId = "sample.shared.variables",
                CatalogVersion = "1"
            },
            Variables =
            [
                new FormVariableBindingDefinition
                {
                    VariableId = VariableId.From("emailVar"),
                    VariableDefinitionId = "sharedEmailVar"
                }
            ],
            Fields =
            [
                new FormFieldBindingDefinition
                {
                    FieldId = FieldId.From("localEmailField"),
                    VariableId = VariableId.From("emailVar"),
                    FieldDefinitionId = "sharedEmailField"
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("entry"),
                    Title = new LocalizedText { ["de"] = "Kontakt" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("localEmailField") }
                    ]
                }
            ]
        };

        var fieldCatalog = new FieldDefinitions
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            FieldDefinitionsId = "sample.shared.fields",
            SupportedLanguages = ["de"],
            StrictLocalization = false,
            Definitions =
            [
                new ReusableFieldDefinition
                {
                    FieldDefinitionId = "sharedEmailField",
                    Label = new LocalizedText { ["de"] = "E-Mail" },
                    Editability = FieldEditability.UserEditable,
                    FieldConstraints =
                    [
                        new ConstraintDefinition
                        {
                            ConstraintId = "required-email",
                            Target = new ConstraintTargetRef
                            {
                                Kind = "field",
                                FieldId = FieldId.From("sharedEmailField")
                            },
                            Severity = ConstraintSeverity.Error,
                            Code = "E_REQUIRED",
                            Message = new LocalizedText { ["de"] = "E-Mail ist erforderlich." },
                            Kind = ConstraintKind.Required
                        }
                    ]
                }
            ]
        };

        var variableCatalog = new VariableDefinitions
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            VariableDefinitionsId = "sample.shared.variables",
            SupportedLanguages = ["de"],
            StrictLocalization = false,
            Definitions =
            [
                new ReusableVariableDefinition
                {
                    VariableDefinitionId = "sharedEmailVar",
                    Name = "email",
                    ValueType = CreateValueType("string")
                }
            ]
        };

        var resolved = FormContractResolver.Resolve(document, fieldCatalog, variableCatalog, formVoiceProfile: null);

        var resolvedConstraint = Assert.Single(Assert.Single(resolved.Fields).FieldConstraints!);
        Assert.Equal("localEmailField", resolvedConstraint.Target.FieldId);
    }

    private static JsonElement CreateValueType(string kind)
    {
        using var doc = JsonDocument.Parse($"{{\"kind\":\"{kind}\"}}");
        return doc.RootElement.Clone();
    }
}


