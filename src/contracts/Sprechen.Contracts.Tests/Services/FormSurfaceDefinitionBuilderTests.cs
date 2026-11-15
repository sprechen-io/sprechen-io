using System.Text.Json;
using Sprechen.Contracts.FormSurfaces;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Contracts.Tests.Services;

public sealed class FormSurfaceDefinitionBuilderTests
{
    private readonly FormSurfaceDefinitionBuilder _builder = new();

    [Fact]
    public void BuildFormSurfaceDefinition_WithNullContract_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _builder.BuildFormSurfaceDefinition(null!, "de", PolitenessLevel.Formal));
    }

    [Fact]
    public void BuildFormSurfaceDefinition_WhenContractContainsFields_ReturnsVariableBindingsAndResolvedMetadata()
    {
        var contract = CreateContract();

        var formProjection = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal);

        Assert.Equal("entry", formProjection.EntryFieldGroupId);
        Assert.Collection(
            formProjection.Fields,
            email =>
            {
                Assert.Equal("email", email.FieldId);
                Assert.Equal("emailVar", email.VariableId);
                Assert.Equal("entry", email.FieldGroupId);
                Assert.Equal("E-Mail", email.Label);
                Assert.Equal("email", email.InputType);
                Assert.True(email.Required);
            },
            newsletter =>
            {
                Assert.Equal("newsletter", newsletter.FieldId);
                Assert.Equal("newsletterVar", newsletter.VariableId);
                Assert.Equal("boolean", newsletter.ValueTypeKind);
                Assert.Equal("select", newsletter.InputType);
                Assert.NotNull(newsletter.Options);
                Assert.Equal(2, newsletter.Options!.Count);
            });
    }

    [Fact]
    public void BuildFormSurfaceDefinition_WithNestedFieldGroups_KeepsTraversalOrder()
    {
        var contract = CreateContract();
        contract.FieldGroups =
        [
            new FieldGroupDefinition
            {
                FieldGroupId = FieldGroupId.From("entry"),
                Title = new LocalizedText { ["de"] = "Entry" },
                Members =
                [
                    new FieldGroupMember { Kind = "fieldGroup", FieldGroupId = FieldGroupId.From("nested") },
                    new FieldGroupMember { Kind = "field", FieldId = FieldId.From("newsletter") }
                ]
            },
            new FieldGroupDefinition
            {
                FieldGroupId = FieldGroupId.From("nested"),
                Title = new LocalizedText { ["de"] = "Nested" },
                Members =
                [
                    new FieldGroupMember { Kind = "field", FieldId = FieldId.From("email") }
                ]
            }
        ];

        var formProjection = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal);

        Assert.Equal(["email", "newsletter"], formProjection.Fields.Select(field => field.FieldId.Value));
        Assert.True(formProjection.FieldGroupsById.ContainsKey("entry"));
        Assert.True(formProjection.FieldGroupsById.ContainsKey("nested"));
    }

    [Fact]
    public void BuildFormSurfaceDefinition_WhenInformalPolitenessConfigured_ResolvesInformalVariants()
    {
        var contract = CreateContract();
        contract.Fields[0].Label = new LocalizedText
        {
            ["de@formal"] = "Ihre E-Mail-Adresse",
            ["de@informal"] = "Deine E-Mail-Adresse"
        };
        contract.FieldGroups[0].VoiceSummary = new LocalizedText
        {
            ["de@formal"] = "Bitte geben Sie Ihre Kontaktdaten an.",
            ["de@informal"] = "Gib deine Kontaktdaten an."
        };

        var formProjection = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Informal);

        Assert.Equal("Deine E-Mail-Adresse", formProjection.Fields[0].Label);
        Assert.Equal("Gib deine Kontaktdaten an.", formProjection.FieldGroupsById["entry"].VoiceSummary);
    }

    [Fact]
    public void BuildFormSurfaceDefinition_WhenFieldGroupReferencesMissingField_ThrowsInvalidOperationException()
    {
        var contract = CreateContract();
        contract.FieldGroups[0].Members =
        [
            new FieldGroupMember { Kind = "field", FieldId = FieldId.From("missing") }
        ];

        Assert.Throws<InvalidOperationException>(() => _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal));
    }

    private static FormContract CreateContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "sample.simple",
            FormId = FormId.From("contact-demo"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("entry"),
            StrictLocalization = false,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("emailVar"),
                    Name = "email",
                    ValueType = ParseJsonElement("""{"kind":"string","formatHint":"email"}"""),
                    SemanticTag = "contact.email"
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("newsletterVar"),
                    Name = "newsletter",
                    ValueType = ParseJsonElement("""{"kind":"boolean"}""")
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("email"),
                    VariableId = VariableId.From("emailVar"),
                    Label = new LocalizedText { ["de"] = "E-Mail" },
                    HelpText = new LocalizedText { ["de"] = "Bitte geben Sie Ihre E-Mail-Adresse ein." },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("newsletter"),
                    VariableId = VariableId.From("newsletterVar"),
                    Label = new LocalizedText { ["de"] = "Newsletter" },
                    Editability = FieldEditability.UserEditable
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
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("email") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("newsletter") }
                    ]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "required-email",
                    Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("email") },
                    Severity = ConstraintSeverity.Error,
                    Code = "E_REQUIRED",
                    Message = new LocalizedText { ["de"] = "E-Mail ist erforderlich." },
                    Kind = ConstraintKind.Required
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
