using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests.FormContracts;

public sealed class FormContractFieldBindingResolverTests
{
    [Fact]
    public void Resolve_WhenReusableConstraintTargetsOwnFieldDefinition_RewritesConstraintTargetToLocalFieldId()
    {
        var bindings = new[]
        {
            new FormFieldBindingDefinition
            {
                FieldId = FieldId.From("localEmailField"),
                VariableId = VariableId.From("emailVar"),
                FieldDefinitionId = "sharedEmailField"
            }
        };
        var definitionsById = new Dictionary<string, ReusableFieldDefinition>(StringComparer.Ordinal)
        {
            ["sharedEmailField"] = new ReusableFieldDefinition
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
        };

        var resolved = FormContractFieldBindingResolver.Resolve(bindings, definitionsById);

        var resolvedConstraint = Assert.Single(Assert.Single(resolved).FieldConstraints!);
        Assert.Equal("localEmailField", resolvedConstraint.Target.FieldId);
    }
}
