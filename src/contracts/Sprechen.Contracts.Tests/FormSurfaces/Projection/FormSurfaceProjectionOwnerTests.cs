using System.Text.Json;
using Sprechen.Contracts.FormSurfaces;
using Sprechen.Contracts.Tests.FormSurfaces;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Contracts.Tests.FormSurfaces.Projection;

public sealed class FormSurfaceProjectionOwnerTests
{
    private readonly FormSurfaceProjectionTestContractFactory _factory = new();

    [Fact]
    public void FormConstraintProjector_ProjectByFieldId_MergesSupportedUnconditionalFieldConstraints()
    {
        var contract = _factory.CreateContractShell();
        var field = _factory.CreateField("email", "emailVar", "E-Mail");
        field.FieldConstraints =
        [
            _factory.CreateConstraint(
                "email-max",
                "email",
                ConstraintKinds.MaxLength,
                new Dictionary<string, JsonElement> { [ConstraintPropertyNames.MaxLength] = _factory.ParseJsonElement("80") })
        ];
        var conditional = _factory.CreateConstraint(
            "email-conditional",
            "email",
            ConstraintKinds.Pattern,
            new Dictionary<string, JsonElement> { [ConstraintPropertyNames.Pattern] = _factory.ParseJsonElement("\"ignored\"") });
        conditional.When = _factory.ParseJsonElement("""{"kind":"literal","value":true}""");
        contract.Constraints =
        [
            _factory.CreateConstraint("email-required", "email", ConstraintKinds.Required, new Dictionary<string, JsonElement>()),
            _factory.CreateConstraint(
                "email-pattern",
                "email",
                ConstraintKinds.Pattern,
                new Dictionary<string, JsonElement> { [ConstraintPropertyNames.Pattern] = _factory.ParseJsonElement("\"^[^@]+$\"") }),
            conditional
        ];
        var fieldsById = new Dictionary<string, FieldDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["email"] = field
        };

        var variablesById = new Dictionary<string, VariableDefinition>(StringComparer.Ordinal)
        {
            ["emailVar"] = _factory.CreateVariable("emailVar", """{"kind":"string"}""")
        };

        var constraints = new FormConstraintProjector().ProjectByFieldId(contract, fieldsById, variablesById);

        var emailConstraints = Assert.Single(constraints);
        Assert.Equal("email", emailConstraints.Key);
        Assert.True(emailConstraints.Value.Required);
        Assert.Equal("^[^@]+$", emailConstraints.Value.Pattern);
        Assert.Equal(80, emailConstraints.Value.MaxLength);
    }

    [Fact]
    public void FormSurfaceProjectionIndexBuilder_KeepsExactOrdinalKeysAndRejectsDuplicates()
    {
        var contract = _factory.CreateContractShell();
        contract.Variables =
        [
            _factory.CreateVariable("emailVar", """{"kind":"string"}"""),
            _factory.CreateVariable("EMAILVAR", """{"kind":"string"}""")
        ];
        contract.Fields = [_factory.CreateField("email", "emailVar", "E-Mail")];
        contract.FieldGroups =
        [
            new FieldGroupDefinition
            {
                FieldGroupId = FieldGroupId.From("entry"),
                Title = new LocalizedText { ["de"] = "Entry" },
                Members = []
            }
        ];

        var index = new FormSurfaceProjectionIndexBuilder().Build(contract);

        Assert.Equal(2, index.VariablesById.Count);
        Assert.True(index.VariablesById.ContainsKey("emailVar"));
        Assert.True(index.VariablesById.ContainsKey("EMAILVAR"));
        Assert.False(index.VariablesById.ContainsKey("EMAILVAR".ToLowerInvariant()));
        Assert.True(index.FieldsById.ContainsKey("email"));
        Assert.False(index.FieldsById.ContainsKey(" email "));
        Assert.True(index.FieldGroupsById.ContainsKey("entry"));

        contract.Variables.Add(_factory.CreateVariable("emailVar", """{"kind":"string"}"""));
        Assert.Throws<InvalidOperationException>(() => new FormSurfaceProjectionIndexBuilder().Build(contract));
    }

    [Fact]
    public void FormConstraintProjector_ProjectByFieldId_WithNullContract_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FormConstraintProjector().ProjectByFieldId(
            null!,
            new Dictionary<string, FieldDefinition>(),
            new Dictionary<string, VariableDefinition>()));
    }

    [Fact]
    public void FormSurfaceCarriedRules_From_WithUnsupportedKind_CarriesNoSide()
    {
        var constraint = _factory.CreateConstraint(
            "unsupported",
            "email",
            "custom",
            new Dictionary<string, JsonElement> { [ConstraintPropertyNames.Pattern] = _factory.ParseJsonElement("\"ignored\"") });

        var carried = CreateCarriedRules().From([constraint]);

        Assert.Equal(FieldConstraints.Empty, carried);
    }

    [Fact]
    public void FormSurfaceCarriedRules_From_WithBoundNobodyCanRead_ReportsTheBrokenContract()
    {
        // Eine unlesbare Grenze muss auffallen, damit sie nicht ersatzlos aus der Formularoberfläche entfällt.
        var constraint = _factory.CreateConstraint(
            "ageMin",
            "age",
            ConstraintKinds.Min,
            new Dictionary<string, JsonElement> { [ConstraintPropertyNames.Min] = _factory.ParseJsonElement("\"achtzehn\"") });

        var error = Assert.Throws<InvalidOperationException>(() => CreateCarriedRules().From([constraint]));

        Assert.Contains("ageMin", error.Message, StringComparison.Ordinal);
    }

    private static FormSurfaceCarriedRules CreateCarriedRules()
        => new(new FormConstraintPropertyReader(), new FormStatedBoundReader());

    [Fact]
    public void FormOptionProjector_ProjectOptions_ResolvesEnumAndBooleanOptions()
    {
        var contract = _factory.CreateContractShell();
        var enumVariable = _factory.CreateVariable(
            "colorVar",
            """
            {"kind":"enum","values":[{"code":"red","displayName":{"de":"Rot"}},{"code":"blue","displayName":{"de":"Blau"}}]}
            """);
        var projector = new FormOptionProjector();

        var enumOptions = projector.ProjectOptions(
            contract,
            ValueTypeKinds.Enum,
            enumVariable,
            FieldConstraints.Empty,
            "de",
            PolitenessLevel.Formal);
        var booleanOptions = projector.ProjectOptions(
            contract,
            ValueTypeKinds.Boolean,
            null,
            FieldConstraints.Empty,
            "es",
            PolitenessLevel.Formal);

        Assert.Collection(
            enumOptions!,
            red => Assert.Equal(("red", "Rot"), (red.Value, red.Label)),
            blue => Assert.Equal(("blue", "Blau"), (blue.Value, blue.Label)));
        Assert.Collection(
            booleanOptions!,
            yes => Assert.Equal(("true", "S\u00ed"), (yes.Value, yes.Label)),
            no => Assert.Equal(("false", "No"), (no.Value, no.Label)));
    }

    [Fact]
    public void FormEnumLikeOptionProjector_TryProject_SkipsMalformedOptions()
    {
        var contract = _factory.CreateContractShell();
        var valueType = _factory.ParseJsonElement(
            """
            {"kind":"enum","values":[{"code":"valid","displayName":{"de":"G\u00fcltig"}},{"displayName":{"de":"Invalid"}},{"code":""}]}
            """);

        var projected = new FormEnumLikeOptionProjector().TryProject(
            contract,
            valueType,
            "de",
            PolitenessLevel.Formal,
            out var options);

        Assert.True(projected);
        var option = Assert.Single(options);
        Assert.Equal(("valid", "G\u00fcltig"), (option.Value, option.Label));
    }

    [Fact]
    public void FormSurfaceOptionLabelResolver_Resolve_WithMissingDisplayName_ReturnsFallback()
    {
        var contract = _factory.CreateContractShell();
        var optionElement = _factory.ParseJsonElement("""{"code":"fallback"}""");

        var label = new FormSurfaceOptionLabelResolver().Resolve(
            contract,
            optionElement,
            "fallback",
            "de",
            PolitenessLevel.Formal);

        Assert.Equal("fallback", label);
    }

    [Fact]
    public void FormBooleanOptionProjector_Project_ResolvesLocalizedLabels()
    {
        var options = new FormBooleanOptionProjector().Project("it");

        Assert.Collection(
            options,
            yes => Assert.Equal(("true", "S\u00ec"), (yes.Value, yes.Label)),
            no => Assert.Equal(("false", "No"), (no.Value, no.Label)));
    }

    [Theory]
    [InlineData("de", "Ja", "Nein")]
    [InlineData("en", "Yes", "No")]
    [InlineData("fr", "Oui", "Non")]
    [InlineData("es", "S\u00ed", "No")]
    [InlineData("it", "S\u00ec", "No")]
    public void FormBooleanOptionProjector_Project_ResolvesEverySupportedLanguage(string isoCode, string expectedYes, string expectedNo)
    {
        var options = new FormBooleanOptionProjector().Project(isoCode);

        Assert.Collection(
            options,
            yes => Assert.Equal(("true", expectedYes), (yes.Value, yes.Label)),
            no => Assert.Equal(("false", expectedNo), (no.Value, no.Label)));
    }

    [Fact]
    public void FormOptionProjector_ProjectOptions_WithUnsupportedPoliteness_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FormOptionProjector().ProjectOptions(
            _factory.CreateContractShell(),
            ValueTypeKinds.Boolean,
            null,
            FieldConstraints.Empty,
            "de",
            (PolitenessLevel)99));
    }

    [Fact]
    public void FormInputTypeMapper_MapInputType_MapsFormatHintsAndPresentationRules()
    {
        var mapper = new FormInputTypeMapper();
        var emailVariable = _factory.CreateVariable("emailVar", """{"kind":"string","formatHint":"email"}""");
        var multilineField = _factory.CreateField("comments", "commentsVar", "Kommentare");
        multilineField.PresentationHints = new FieldPresentationHints { MultilinePreferred = true };

        var emailType = mapper.MapInputType(ValueTypeKinds.String, emailVariable, _factory.CreateField("email", "emailVar", "E-Mail"), FieldConstraints.Empty, null);
        var textareaType = mapper.MapInputType(ValueTypeKinds.String, _factory.CreateVariable("commentsVar", """{"kind":"string"}"""), multilineField, FieldConstraints.Empty, null);
        var setType = mapper.MapInputType(ValueTypeKinds.Set, _factory.CreateVariable("tagsVar", """{"kind":"set"}"""), _factory.CreateField("tags", "tagsVar", "Tags"), FieldConstraints.Empty, []);

        Assert.Equal("email", emailType);
        Assert.Equal("textarea", textareaType);
        Assert.Equal("checkbox", setType);
    }

    [Fact]
    public void FormStatedBoundReader_ReadsAWholeNumberInBothNotationsAndACalendarDay()
    {
        var reader = new FormStatedBoundReader();
        var wholeNumber = _factory.CreateConstraint(
            "ageMin",
            "age",
            ConstraintKinds.Min,
            new Dictionary<string, JsonElement> { [ConstraintPropertyNames.Min] = _factory.ParseJsonElement("18") });
        var sameBoundAsText = _factory.CreateConstraint(
            "ageMin",
            "age",
            ConstraintKinds.Min,
            new Dictionary<string, JsonElement> { [ConstraintPropertyNames.Min] = _factory.ParseJsonElement("\"18\"") });
        var calendarDay = _factory.CreateConstraint(
            "bornAfter",
            "birthday",
            ConstraintKinds.DateMin,
            new Dictionary<string, JsonElement> { [ConstraintPropertyNames.MinDate] = _factory.ParseJsonElement("\"2026-02-28\"") });

        Assert.Equal("18", reader.Read(wholeNumber, ConstraintPropertyNames.Min, boundsAreCalendarDates: false)?.MarkupText);
        Assert.Equal("18", reader.Read(sameBoundAsText, ConstraintPropertyNames.Min, boundsAreCalendarDates: false)?.MarkupText);
        Assert.Equal("2026-02-28", reader.Read(calendarDay, ConstraintPropertyNames.MinDate, boundsAreCalendarDates: true)?.MarkupText);
        Assert.Null(reader.Read(wholeNumber, ConstraintPropertyNames.Max, boundsAreCalendarDates: false));

        // Das Datumsformat allein genügt nicht; auch der Kalendertag muss existieren.
        var impossibleDay = _factory.CreateConstraint(
            "bornAfter",
            "birthday",
            ConstraintKinds.DateMin,
            new Dictionary<string, JsonElement> { [ConstraintPropertyNames.MinDate] = _factory.ParseJsonElement("\"2026-02-30\"") });

        Assert.Throws<InvalidOperationException>(
            () => reader.Read(impossibleDay, ConstraintPropertyNames.MinDate, boundsAreCalendarDates: true));
    }

    [Fact]
    public void FormInputTypeMapper_MapInputType_WithNullField_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FormInputTypeMapper().MapInputType(
            ValueTypeKinds.String,
            null,
            null!,
            FieldConstraints.Empty,
            null));
    }

    [Fact]
    public void FormFieldGroupMetadataProjector_Project_ResolvesLocalizedMetadata()
    {
        var contract = _factory.CreateContractShell();
        var fieldGroup = new FieldGroupDefinition
        {
            FieldGroupId = FieldGroupId.From("entry"),
            Title = new LocalizedText { ["de@formal"] = "Kontakt" },
            Description = new LocalizedText { ["de"] = "Beschreibung" },
            VoiceSummary = new LocalizedText { ["de"] = "Zusammenfassung" }
        };

        var projection = new FormFieldGroupMetadataProjector().Project(contract, fieldGroup, "de", PolitenessLevel.Formal);

        Assert.Equal("entry", projection.FieldGroupId);
        Assert.Equal("Kontakt", projection.Title);
        Assert.Equal("Beschreibung", projection.Description);
        Assert.Equal("Zusammenfassung", projection.VoiceSummary);
    }

    [Fact]
    public void FormFieldGroupMetadataProjector_Project_WithNullFieldGroup_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FormFieldGroupMetadataProjector().Project(
            _factory.CreateContractShell(),
            null!,
            "de",
            PolitenessLevel.Formal));
    }

    [Fact]
    public void FormAccessibilityLabelSelector_SelectsResolvedFieldText()
    {
        var contract = _factory.CreateContractShell();
        var field = _factory.CreateField("email", "emailVar", "E-Mail");
        field.HelpText = new LocalizedText { ["de"] = "Hilfe" };
        field.Placeholder = new LocalizedText { ["de"] = "Platzhalter" };
        field.Examples = [new LocalizedText { ["de"] = "beispiel@example.com" }];
        field.Projections = new FieldProjections
        {
            Html = new FieldHtmlProjection
            {
                AccessibleDescription = new LocalizedText { ["de"] = "Beschreibung" }
            }
        };
        var selector = new FormAccessibilityLabelSelector();

        Assert.Equal("E-Mail", selector.SelectLabel(contract, field, "de", PolitenessLevel.Formal));
        Assert.Equal("Hilfe", selector.SelectHelpText(contract, field, "de", PolitenessLevel.Formal));
        Assert.Equal("Platzhalter", selector.SelectPlaceholder(contract, field, "de", PolitenessLevel.Formal));
        Assert.Equal(["beispiel@example.com"], selector.SelectExamples(contract, field, "de", PolitenessLevel.Formal));
        Assert.Equal("Beschreibung", selector.SelectAccessibleDescription(contract, field, "de", PolitenessLevel.Formal));
    }

    [Fact]
    public void FormAccessibilityLabelSelector_SelectLabel_WithNullField_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FormAccessibilityLabelSelector().SelectLabel(
            _factory.CreateContractShell(),
            null!,
            "de",
            PolitenessLevel.Formal));
    }

    [Fact]
    public void FormFieldProjector_Project_ComposesFieldSurface()
    {
        var contract = _factory.CreateContractShell();
        var field = _factory.CreateField("email", "emailVar", "E-Mail");
        field.HelpText = new LocalizedText { ["de"] = "Hilfe" };
        var variablesById = new Dictionary<string, VariableDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["emailVar"] = _factory.CreateVariable("emailVar", """{"kind":"string","formatHint":"email"}""")
        };
        var constraints = FieldConstraints.Empty with { Required = true, Pattern = ".+@.+" };
        var projector = CreateFieldProjector();

        var projection = projector.Project(contract, field, "entry", variablesById, constraints, "de", PolitenessLevel.Formal);

        Assert.Equal("email", projection.FieldId);
        Assert.Equal("entry", projection.FieldGroupId);
        Assert.Equal("emailVar", projection.VariableId);
        Assert.Equal("E-Mail", projection.Label);
        Assert.Equal("string", projection.ValueTypeKind);
        Assert.Equal("email", projection.InputType);
        Assert.True(projection.Required);
        Assert.True(projection.IsUserEditable);
        Assert.Equal(".+@.+", projection.Pattern);
        Assert.Equal("Hilfe", projection.HelpText);
    }

    [Theory]
    [InlineData(FieldEditability.UserEditable, true, false)]
    [InlineData(FieldEditability.ReadOnly, false, true)]
    [InlineData(FieldEditability.Computed, false, false)]
    [InlineData(FieldEditability.Unknown, false, false)]
    public void FormFieldProjector_Project_ProjectsReadOnlyDisplayExactlyForReadOnlyEditability(
        FieldEditability editability,
        bool expectedIsUserEditable,
        bool expectedIsReadOnlyDisplay)
    {
        var contract = _factory.CreateContractShell();
        var field = _factory.CreateField("instructions", "instructionsVar", "Hinweis");
        field.Editability = editability;
        var variablesById = new Dictionary<string, VariableDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["instructionsVar"] = _factory.CreateVariable("instructionsVar", """{"kind":"string"}""")
        };

        var projection = CreateFieldProjector().Project(
            contract,
            field,
            "entry",
            variablesById,
            FieldConstraints.Empty,
            "de",
            PolitenessLevel.Formal);

        Assert.Equal(expectedIsUserEditable, projection.IsUserEditable);
        Assert.Equal(expectedIsReadOnlyDisplay, projection.IsReadOnlyDisplay);
    }

    [Fact]
    public void FormFieldProjector_Project_WithUnknownVariable_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => CreateFieldProjector().Project(
            _factory.CreateContractShell(),
            _factory.CreateField("email", "emailVar", "E-Mail"),
            "entry",
            new Dictionary<string, VariableDefinition>(StringComparer.OrdinalIgnoreCase),
            FieldConstraints.Empty,
            "de",
            PolitenessLevel.Formal));
    }

    private static FormFieldProjector CreateFieldProjector()
    {
        return new FormFieldProjector(
            new FormAccessibilityLabelSelector(),
            new FormOptionProjector(),
            new FormInputTypeMapper());
    }
}
