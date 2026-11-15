using System.Text.Json;
using Sprechen.Contracts.FormSurfaces;
using Sprechen.Forms.Services;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;
using ReferenceFormDefinitionBuilder = Sprechen.Contracts.FormSurfaces.FormSurfaceDefinitionBuilder;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Forms.Tests.Services;

public sealed class ReferenceFormFieldBuilderTests
{
    private readonly ReferenceFormDefinitionBuilder _builder = new();

    #region Constructor and Parameter Validation

    [Fact]
    public void BuildFields_WithNullContract_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _builder.BuildFormSurfaceDefinition(null!, "de", PolitenessLevel.Formal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildFields_WithEmptyOrWhitespaceLanguage_ThrowsArgumentException(string language)
    {
        var contract = CreateContract();

        Assert.Throws<ArgumentException>(() => _builder.BuildFormSurfaceDefinition(contract, language, PolitenessLevel.Formal));
    }

    [Fact]
    public void BuildFields_WithNullLanguage_ThrowsArgumentNullException()
    {
        var contract = CreateContract();

        Assert.Throws<ArgumentNullException>(() => _builder.BuildFormSurfaceDefinition(contract, null!, PolitenessLevel.Formal));
    }

    #endregion

    #region Basic Functionality

    [Fact]
    public void BuildFields_WhenContractContainsFields_ReturnsOrderedFieldsWithInputTypes()
    {
        var contract = CreateContract();

        var formProjection = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal);
        var fields = formProjection.Fields;

        Assert.Equal("entry", formProjection.EntryFieldGroupId);
        Assert.Contains("entry", formProjection.FieldGroupsById.Keys);

        Assert.Collection(fields,
            email =>
            {
                Assert.Equal("email", email.FieldId);
                Assert.Equal("entry", email.FieldGroupId);
                Assert.Equal("E-Mail", email.Label);
                Assert.Equal("string", email.ValueTypeKind);
                Assert.False(email.CodeStyle);
                Assert.Equal("email", email.InputType);
                Assert.True(email.Required);
                Assert.True(email.IsUserEditable);
                Assert.Equal("Bitte geben Sie Ihre E-Mail-Adresse ein.", email.HelpText);
                Assert.Null(email.Placeholder);
            },
            newsletter =>
            {
                Assert.Equal("newsletter", newsletter.FieldId);
                Assert.Equal("entry", newsletter.FieldGroupId);
                Assert.Equal("Newsletter", newsletter.Label);
                Assert.Equal("boolean", newsletter.ValueTypeKind);
                Assert.False(newsletter.CodeStyle);
                Assert.Equal("select", newsletter.InputType);
                Assert.False(newsletter.Required);
                Assert.True(newsletter.IsUserEditable);
                Assert.NotNull(newsletter.Options);
                Assert.Equal(2, newsletter.Options!.Count);
            });
    }

    [Fact]
    public void BuildFields_WhenFieldHasExamplesAndAccessibleDescription_ResolvesForRequestedLanguage()
    {
        var contract = CreateContract();
        contract.Fields[0].Examples =
        [
            new LocalizedText { ["de"] = "max@example.com" },
            new LocalizedText { ["de"] = "alice@example.com" }
        ];
        contract.Fields[0].Projections = new FieldProjections
        {
            Html = new FieldHtmlProjection
            {
                AccessibleDescription = new LocalizedText { ["de"] = "Wir verwenden diese E-Mail nur für Rückfragen." }
            }
        };

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Equal(["max@example.com", "alice@example.com"], fields[0].Examples);
        Assert.Equal("Wir verwenden diese E-Mail nur für Rückfragen.", fields[0].AccessibleDescription);
    }

    [Fact]
    public void BuildFields_WhenSectionHasDescription_ResolvesForRequestedLanguage()
    {
        var contract = CreateContract();
        contract.FieldGroups[0].Description = new LocalizedText { ["de"] = "Bitte geben Sie Ihre Kontaktdaten an." };

        var formProjection = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal);

        Assert.True(formProjection.FieldGroupsById.TryGetValue("entry", out var fieldGroup));
        Assert.Equal("Bitte geben Sie Ihre Kontaktdaten an.", fieldGroup.Description);
    }

    [Fact]
    public void BuildFields_WhenMultilinePreferred_RendersTextareaForText()
    {
        var contract = CreateContract();
        contract.Variables.Add(new VariableDefinition
        {
            VariableId = VariableId.From("commentVar"),
            Name = "comment",
            ValueType = ParseJsonElement("""{"kind":"string"}""")
        });
        contract.Fields.Add(new FieldDefinition
        {
            FieldId = FieldId.From("comment"),
            VariableId = VariableId.From("commentVar"),
            Label = new LocalizedText { ["de"] = "Kommentar" },
            Editability = FieldEditability.UserEditable,
            PresentationHints = new FieldPresentationHints { MultilinePreferred = true }
        });
        contract.FieldGroups[0].Members.Add(new FieldGroupMember { Kind = "field", FieldId = FieldId.From("comment") });

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;
        var comment = fields.Single(field => string.Equals(field.FieldId, "comment", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("textarea", comment.InputType);
    }

    [Fact]
    public void BuildFields_WithEmptyFields_ReturnsEmptyList()
    {
        var contract = CreateContract();
        contract.Fields = [];

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Empty(fields);
    }

    [Fact]
    public void BuildFields_WithEmptyFieldGroups_ReturnsEmptyList()
    {
        var contract = CreateContract();
        contract.FieldGroups = [];

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Empty(fields);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildFields_WithNullOrEmptyEntryFieldGroupId_ReturnsEmptyList(string? entryFieldGroupId)
    {
        var contract = CreateContract();
        contract.EntryFieldGroupId = string.IsNullOrWhiteSpace(entryFieldGroupId) ? default : FieldGroupId.From(entryFieldGroupId);

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Empty(fields);
    }

    #endregion

    #region Variable Handling

    [Fact]
    public void BuildFields_WithNullVariable_SkipsVariable()
    {
        var contract = CreateContract();
        contract.Variables = [null!, contract.Variables[0], contract.Variables[1]];

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Equal(2, fields.Count);
    }

    [Fact]
    public void BuildFields_WithDuplicateVariableIds_FailsClosed()
    {
        var contract = CreateContract();
        contract.Variables =
        [
            new VariableDefinition
            {
                VariableId = VariableId.From("emailVar"),
                Name = "email1",
                ValueType = ParseJsonElement("""{"kind":"string","formatHint":"email"}""")
            },
            new VariableDefinition
            {
                VariableId = VariableId.From("emailVar"),
                Name = "email2",
                ValueType = ParseJsonElement("""{"kind":"string","formatHint":"phone"}""")
            },
            contract.Variables[1]
        ];

        var thrown = Assert.Throws<InvalidOperationException>(
            () => _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal));

        Assert.Equal("A modeled identifier was not an exact unique canonical key.", thrown.Message);
    }

    [Fact]
    public void BuildFields_WithWhitespaceVariableId_FailsClosed()
    {
        var contract = CreateContract();
        contract.Variables[0].VariableId = VariableId.From("  emailVar  ");

        var thrown = Assert.Throws<InvalidOperationException>(
            () => _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal));

        Assert.Equal("Form surface field references unknown variable 'emailVar'.", thrown.Message);
    }

    #endregion

    #region Field Handling

    [Fact]
    public void BuildFields_WithNullField_SkipsField()
    {
        var contract = CreateContract();
        contract.Fields = [null!, contract.Fields[0], contract.Fields[1]];

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Equal(2, fields.Count);
    }

    [Fact]
    public void BuildFields_WithReadOnlyField_ProjectsReadOnlyDisplaySignal()
    {
        var contract = CreateContract();
        contract.Fields[0].Editability = FieldEditability.ReadOnly;

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Collection(fields,
            readOnlyField =>
            {
                Assert.False(readOnlyField.IsUserEditable);
                Assert.True(readOnlyField.IsReadOnlyDisplay);
            },
            editableField =>
            {
                Assert.True(editableField.IsUserEditable);
                Assert.False(editableField.IsReadOnlyDisplay);
            });
    }

    [Fact]
    public void BuildFields_WithComputedField_KeepsReadOnlyDisplayFalse()
    {
        var contract = CreateContract();
        contract.Fields[0].Editability = FieldEditability.Computed;

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.False(fields[0].IsUserEditable);
        Assert.False(fields[0].IsReadOnlyDisplay);
    }

    [Fact]
    public void BuildFields_WithDuplicateFieldIds_FailsClosed()
    {
        var contract = CreateContract();
        contract.Fields =
        [
            new FieldDefinition
            {
                FieldId = FieldId.From("email"),
                VariableId = VariableId.From("emailVar"),
                Label = new LocalizedText { ["de"] = "E-Mail 1" },
                Editability = FieldEditability.UserEditable
            },
            new FieldDefinition
            {
                FieldId = FieldId.From("email"),
                VariableId = VariableId.From("emailVar"),
                Label = new LocalizedText { ["de"] = "E-Mail 2" },
                Editability = FieldEditability.UserEditable
            },
            contract.Fields[1]
        ];

        var thrown = Assert.Throws<InvalidOperationException>(
            () => _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal));

        Assert.Equal("A modeled identifier was not an exact unique canonical key.", thrown.Message);
    }

    [Fact]
    public void BuildFields_WithWhitespaceFieldId_FailsClosed()
    {
        var contract = CreateContract();
        contract.Fields[0].FieldId = FieldId.From("  email  ");

        var thrown = Assert.Throws<InvalidOperationException>(
            () => _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal));

        Assert.Equal("Form surface field 'email' was not found.", thrown.Message);
    }

    #endregion

    #region FieldGroup Handling

    [Fact]
    public void BuildFields_WithNullSection_SkipsSection()
    {
        var contract = CreateContract();
        contract.FieldGroups = [null!, contract.FieldGroups[0]];

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.NotEmpty(fields);
    }

    [Fact]
    public void BuildFields_WithNestedFieldGroups_TraversesRecursively()
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
                    new FieldGroupMember { Kind = "fieldGroup", FieldGroupId = FieldGroupId.From("nested") }
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

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Single(fields);
        Assert.Equal("email", fields[0].FieldId);
    }

    [Fact]
    public void BuildFields_WithCircularSectionReference_HandlesGracefully()
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
                    new FieldGroupMember { Kind = "field", FieldId = FieldId.From("email") },
                    new FieldGroupMember { Kind = "fieldGroup", FieldGroupId = FieldGroupId.From("entry") }
                ]
            }
        ];

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Single(fields);
    }

    [Fact]
    public void BuildFields_WithNullMember_SkipsMember()
    {
        var contract = CreateContract();
        contract.FieldGroups[0].Members = [null!, new FieldGroupMember { Kind = "field", FieldId = FieldId.From("email") }];

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Single(fields);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildFields_WithEmptyMemberFieldId_ThrowsInvalidOperationException(string? fieldId)
    {
        var contract = CreateContract();
        contract.FieldGroups[0].Members = [new FieldGroupMember { Kind = "field", FieldId = string.IsNullOrWhiteSpace(fieldId) ? null : FieldId.From(fieldId) }];

        Assert.Throws<InvalidOperationException>(() => _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildFields_WithEmptyMemberFieldGroupId_ThrowsInvalidOperationException(string? fieldGroupId)
    {
        var contract = CreateContract();
        contract.FieldGroups =
        [
            new FieldGroupDefinition
            {
                FieldGroupId = FieldGroupId.From("entry"),
                Members = [new FieldGroupMember { Kind = "fieldGroup", FieldGroupId = string.IsNullOrWhiteSpace(fieldGroupId) ? null : FieldGroupId.From(fieldGroupId) }]
            }
        ];

        Assert.Throws<InvalidOperationException>(() => _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal));
    }

    [Fact]
    public void BuildFields_WithNonExistentSection_ThrowsInvalidOperationException()
    {
        var contract = CreateContract();
        contract.FieldGroups[0].Members = [new FieldGroupMember { Kind = "fieldGroup", FieldGroupId = FieldGroupId.From("nonexistent") }];

        Assert.Throws<InvalidOperationException>(() => _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal));
    }

    [Fact]
    public void BuildFields_WithNonExistentField_ThrowsInvalidOperationException()
    {
        var contract = CreateContract();
        contract.FieldGroups[0].Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("nonexistent") }];

        Assert.Throws<InvalidOperationException>(() => _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal));
    }

    #endregion

    #region Input Type Mapping

    [Theory]
    [InlineData("email", "email")]
    [InlineData("phone", "tel")]
    [InlineData("date", "date")]
    [InlineData("number", "number")]
    [InlineData("unknown", "text")]
    public void BuildFields_MapsFormatHintToInputType(string formatHint, string expectedInputType)
    {
        var contract = CreateContractWithFormatHint(formatHint);

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Equal(expectedInputType, fields[0].InputType);
    }

    [Fact]
    public void BuildFields_WithBooleanValueType_ReturnsSelect()
    {
        var contract = CreateContract();

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;
        var newsletter = fields.Single(field => string.Equals(field.FieldId, "newsletter", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("select", newsletter.InputType);
        Assert.Equal("boolean", newsletter.ValueTypeKind);
        Assert.False(newsletter.CodeStyle);
        Assert.NotNull(newsletter.Options);
    }

    [Fact]
    public void BuildFields_WithContactEmailSemanticTag_ReturnsEmail()
    {
        var contract = CreateContract();
        contract.Variables[0].ValueType = ParseJsonElement("""{"kind":"string"}""");
        contract.Variables[0].SemanticTag = "contact.email";

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Equal("email", fields[0].InputType);
    }

    [Fact]
    public void BuildFields_WithoutVariableId_ReturnsText()
    {
        var contract = CreateContract();
        contract.Fields[0].VariableId = default;

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Equal("text", fields[0].InputType);
    }

    [Fact]
    public void BuildFields_WithNonObjectValueType_ReturnsText()
    {
        var contract = CreateContract();
        contract.Variables[0].ValueType = ParseJsonElement("\"string\"");
        contract.Variables[0].SemanticTag = null; // Ohne semantischen Tag wird nur die Behandlung des nicht objektförmigen Werttyps geprüft.

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Equal("text", fields[0].InputType);
    }

    #endregion

    #region Constraint Handling

    [Fact]
    public void BuildFields_WithPatternConstraint_SetsPattern()
    {
        var contract = CreateContractWithConstraint("pattern", new Dictionary<string, JsonElement>
        {
            ["pattern"] = ParseJsonElement("\"^[A-Z]+$\"")
        });

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Equal("^[A-Z]+$", fields[0].Pattern);
    }

    [Fact]
    public void BuildFields_WithMinLengthConstraint_SetsMinLength()
    {
        var contract = CreateContractWithConstraint("minLength", new Dictionary<string, JsonElement>
        {
            ["minLength"] = ParseJsonElement("5")
        });

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Equal(5, fields[0].MinLength);
    }

    [Fact]
    public void BuildFields_WithMaxLengthConstraint_SetsMaxLength()
    {
        var contract = CreateContractWithConstraint("maxLength", new Dictionary<string, JsonElement>
        {
            ["maxLength"] = ParseJsonElement("100")
        });

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Equal(100, fields[0].MaxLength);
    }

    [Fact]
    public void BuildFields_WithOneOfConstraint_SetsOneOfValues()
    {
        var contract = CreateContractWithConstraint("oneOf", new Dictionary<string, JsonElement>
        {
            ["values"] = ParseJsonElement("""["A","B","C"]""")
        });

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.NotNull(fields[0].Options);
        Assert.Equal(["A", "B", "C"], fields[0].Options!.Select(o => o.Value));
    }

    [Fact]
    public void BuildFields_WithNullConstraint_SkipsConstraint()
    {
        var contract = CreateContract();
        contract.Constraints = [null!];

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.False(fields[0].Required);
    }

    [Fact]
    public void BuildFields_WithNonFieldTargetConstraint_SkipsConstraint()
    {
        var contract = CreateContract();
        contract.Constraints =
        [
            new ConstraintDefinition
            {
                ConstraintId = "c1",
                Target = new ConstraintTargetRef { Kind = "form" },
                Kind = ConstraintKind.Required
            }
        ];

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.False(fields[0].Required);
    }

    [Fact]
    public void BuildFields_WithEmptyConstraintKind_SkipsConstraint()
    {
        var contract = CreateContract();
        contract.Constraints =
        [
            new ConstraintDefinition
            {
                ConstraintId = "c1",
                Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("email") },
                Kind = ConstraintKind.Unknown
            }
        ];

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.False(fields[0].Required);
    }

    [Fact]
    public void BuildFields_WithNullAdditionalProperties_ReturnsNullConstraintValues()
    {
        var contract = CreateContractWithConstraint("pattern", null);

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Null(fields[0].Pattern);
    }

    [Fact]
    public void BuildFields_WithWrongTypeProperty_ReturnsNull()
    {

        var contract = CreateContractWithConstraint("pattern", new Dictionary<string, JsonElement>
        {
            ["pattern"] = ParseJsonElement("123")
        });

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Null(fields[0].Pattern);
    }

    [Fact]
    public void BuildFields_WithEmptyOneOfArray_ReturnsNull()
    {
        var contract = CreateContractWithConstraint("oneOf", new Dictionary<string, JsonElement>
        {
            ["values"] = ParseJsonElement("[]")
        });

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Null(fields[0].Options);
    }

    [Fact]
    public void BuildFields_WithNonStringOneOfArrayItems_FiltersItems()
    {
        var contract = CreateContractWithConstraint("oneOf", new Dictionary<string, JsonElement>
        {
            ["values"] = ParseJsonElement("""["A",123,"B",null]""")
        });

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.NotNull(fields[0].Options);
        Assert.Equal(["A", "B"], fields[0].Options!.Select(o => o.Value));
    }

    [Fact]
    public void BuildFields_WithInvalidNumberForMinLength_ReturnsNull()
    {
        var contract = CreateContractWithConstraint("minLength", new Dictionary<string, JsonElement>
        {
            ["minLength"] = ParseJsonElement("\"not-a-number\"")
        });

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Null(fields[0].MinLength);
    }

    #endregion

    #region Localization

    [Fact]
    public void BuildFields_WithLabelNotResolved_UsesFieldIdAsLabel()
    {
        var contract = CreateContract();
        contract.Fields[0].Label = new LocalizedText { ["en"] = "Email" };
        contract.StrictLocalization = true;

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Equal("email", fields[0].Label);
    }

    [Fact]
    public void BuildFields_WithEmptyHelpText_ReturnsEmptyString()
    {
        var contract = CreateContract();
        contract.Fields[0].HelpText = null;

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Null(fields[0].HelpText);
    }

    [Fact]
    public void BuildFields_WithWhitespaceLabel_TrimsLabel()
    {
        var contract = CreateContract();
        contract.Fields[0].Label = new LocalizedText { ["de"] = "  E-Mail  " };

        var fields = _builder.BuildFormSurfaceDefinition(contract, "de", PolitenessLevel.Formal).Fields;

        Assert.Equal("E-Mail", fields[0].Label);
    }

    #endregion

    #region Helper Methods

    private static FormContract CreateContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "sample.profile",
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

    private static FormContract CreateContractWithFormatHint(string formatHint)
    {
        var contract = CreateContract();
        contract.Variables[0].ValueType = ParseJsonElement($$$"""{"kind":"string","formatHint":"{{{formatHint}}}"}""");
        contract.Variables[0].SemanticTag = null; // Ohne semantischen Tag bestimmt allein formatHint den Eingabetyp.
        return contract;
    }

    private static FormContract CreateContractWithConstraint(string kind, Dictionary<string, JsonElement>? additionalProperties)
    {
        var contract = CreateContract();
        contract.Constraints =
        [
            new ConstraintDefinition
            {
                ConstraintId = "test-constraint",
                Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("email") },
                Severity = ConstraintSeverity.Error,
                Code = "E_TEST",
                Kind = ConstraintKindExtensions.TryFromWireToken(kind, out var parsedKind) ? parsedKind : ConstraintKind.Unknown,
                AdditionalProperties = additionalProperties
            }
        ];
        return contract;
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    #endregion
}


