using Sprechen.Mda.Contracts.V1.Fields;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft doppelte Felddefinitionskennungen und die Zugehörigkeit einer angegebenen
/// Standardsprache zu den unterstützten Sprachen.
/// </summary>
internal sealed class FieldDefinitionsIdentityValidator
{
    public void Validate(FieldDefinitions catalog, List<PreflightIssue> errors)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(errors);

        ValidateDefaultLanguage(catalog, errors);
        ValidateUniqueDefinitionIds(catalog, errors);
    }

    private static void ValidateDefaultLanguage(FieldDefinitions catalog, List<PreflightIssue> errors)
    {
        if (string.IsNullOrWhiteSpace(catalog.DefaultLanguage)
            || catalog.SupportedLanguages.Contains(catalog.DefaultLanguage, StringComparer.Ordinal))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: $"defaultLanguage '{catalog.DefaultLanguage}' must be contained in supportedLanguages.",
            Path: "$.defaultLanguage",
            Id: catalog.FieldDefinitionsId));
    }

    private static void ValidateUniqueDefinitionIds(FieldDefinitions catalog, List<PreflightIssue> errors)
    {
        var definitionsById = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < catalog.Definitions.Count; i++)
        {
            var definition = catalog.Definitions[i];
            if (definitionsById.Add(definition.FieldDefinitionId))
            {
                continue;
            }

            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorIdDuplicate,
                Message: $"Duplicate fieldDefinitionId '{definition.FieldDefinitionId}'.",
                Path: $"$.definitions[{i}].fieldDefinitionId",
                Id: definition.FieldDefinitionId));
        }
    }
}
