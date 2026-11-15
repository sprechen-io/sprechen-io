using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class FieldDefinitionsPreflightTests
{
    [Fact]
    public void Validate_WhenStrictLocalizationMissingReusableFieldLabel_ReturnsError()
    {
        var catalog = CreateLocalizedCatalog(strictLocalization: true);

        var result = FieldDefinitionsPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Empty(result.Warnings);
        Assert.Contains(
            result.Errors,
            issue => issue.Code == PreflightCodes.ErrorLocalizationMissing
                && string.Equals(issue.Path, "$.definitions[0].label", StringComparison.Ordinal)
                && string.Equals(issue.Id, "nameFieldDefinition", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenNonStrictLocalizationMissingReusableFieldLabel_ReturnsWarning()
    {
        var catalog = CreateLocalizedCatalog(strictLocalization: false);

        var result = FieldDefinitionsPreflight.Validate(catalog);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
        Assert.Contains(
            result.Warnings,
            issue => issue.Code == PreflightCodes.WarnLocalizationPartial
                && string.Equals(issue.Path, "$.definitions[0].label", StringComparison.Ordinal)
                && string.Equals(issue.Id, "nameFieldDefinition", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenFieldDefinitionIdIsDuplicate_ReturnsDuplicateAtSecondDefinitionPath()
    {
        var catalog = CreateLocalizedCatalog(strictLocalization: true);
        catalog.SupportedLanguages = ["de"];
        catalog.Definitions.Add(new ReusableFieldDefinition
        {
            FieldDefinitionId = "nameFieldDefinition",
            Label = new LocalizedText { ["de"] = "Name erneut" },
            Editability = FieldEditability.UserEditable
        });

        var result = FieldDefinitionsPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(
            result.Errors,
            issue => issue.Code == PreflightCodes.ErrorIdDuplicate
                && string.Equals(issue.Path, "$.definitions[1].fieldDefinitionId", StringComparison.Ordinal)
                && string.Equals(issue.Id, "nameFieldDefinition", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenReusableConstraintTargetsDifferentFieldDefinitionId_ReturnsFeatureNotSupported()
    {
        var catalog = new FieldDefinitions
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            FieldDefinitionsId = "sample.shared.fields",
            SupportedLanguages = ["de"],
            StrictLocalization = true,
            Definitions =
            [
                new ReusableFieldDefinition
                {
                    FieldDefinitionId = "nameFieldDefinition",
                    Label = new LocalizedText { ["de"] = "Name" },
                    Editability = FieldEditability.UserEditable,
                    FieldConstraints =
                    [
                        new ConstraintDefinition
                        {
                            ConstraintId = "required-name",
                            Target = new ConstraintTargetRef
                            {
                                Kind = "field",
                                FieldId = FieldId.From("otherFieldDefinition")
                            },
                            Severity = ConstraintSeverity.Error,
                            Code = "E_REQUIRED",
                            Message = new LocalizedText { ["de"] = "Name ist erforderlich." },
                            Kind = ConstraintKind.Required
                        }
                    ]
                }
            ]
        };

        var result = FieldDefinitionsPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(
            result.Errors,
            issue => issue.Code == PreflightCodes.ErrorFeatureNotSupported
                && string.Equals(issue.Path, "$.definitions[0].fieldConstraints[0].target", StringComparison.Ordinal));
    }

    private static FieldDefinitions CreateLocalizedCatalog(bool strictLocalization)
    {
        return new FieldDefinitions
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            FieldDefinitionsId = "sample.shared.fields",
            SupportedLanguages = ["de", "en"],
            StrictLocalization = strictLocalization,
            Definitions =
            [
                new ReusableFieldDefinition
                {
                    FieldDefinitionId = "nameFieldDefinition",
                    Label = new LocalizedText { ["de"] = "Name" },
                    Editability = FieldEditability.UserEditable
                }
            ]
        };
    }
}

