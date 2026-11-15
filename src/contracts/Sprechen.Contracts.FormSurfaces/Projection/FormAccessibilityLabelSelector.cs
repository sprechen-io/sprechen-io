using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Löst Feldtexte anhand von Sprache und Anrede auf. Bei fehlender Beschriftung
/// dient die Feldkennung als Ersatz, damit das Feld benannt bleibt.
/// </summary>
internal sealed class FormAccessibilityLabelSelector
{
    public string SelectLabel(
        FormContract contract,
        FieldDefinition field,
        string requestedLanguage,
        PolitenessLevel politenessLevel)
    {
        ValidateInputs(contract, field, requestedLanguage, politenessLevel);

        var fieldId = field.FieldId.Value;
        return TryResolveTrimmed(contract, field.Label, requestedLanguage, politenessLevel, out var label)
            ? label
            : fieldId;
    }

    public string? SelectHelpText(
        FormContract contract,
        FieldDefinition field,
        string requestedLanguage,
        PolitenessLevel politenessLevel)
    {
        ValidateInputs(contract, field, requestedLanguage, politenessLevel);

        return field.HelpText is not null
            && TryResolveTrimmed(contract, field.HelpText, requestedLanguage, politenessLevel, out var helpText)
                ? helpText
                : null;
    }

    public string? SelectPlaceholder(
        FormContract contract,
        FieldDefinition field,
        string requestedLanguage,
        PolitenessLevel politenessLevel)
    {
        ValidateInputs(contract, field, requestedLanguage, politenessLevel);

        return field.Placeholder is not null
            && TryResolveTrimmed(contract, field.Placeholder, requestedLanguage, politenessLevel, out var placeholder)
                ? placeholder
                : null;
    }

    public IReadOnlyList<string>? SelectExamples(
        FormContract contract,
        FieldDefinition field,
        string requestedLanguage,
        PolitenessLevel politenessLevel)
    {
        ValidateInputs(contract, field, requestedLanguage, politenessLevel);

        if (field.Examples is not { Count: > 0 })
        {
            return null;
        }

        var examples = new List<string>(field.Examples.Count);
        foreach (var example in field.Examples)
        {
            if (example is not null
                && TryResolveTrimmed(contract, example, requestedLanguage, politenessLevel, out var resolvedExample))
            {
                examples.Add(resolvedExample);
            }
        }

        return examples.Count > 0 ? examples : null;
    }

    public string? SelectAccessibleDescription(
        FormContract contract,
        FieldDefinition field,
        string requestedLanguage,
        PolitenessLevel politenessLevel)
    {
        ValidateInputs(contract, field, requestedLanguage, politenessLevel);

        return field.Projections?.Html?.AccessibleDescription is not null
            && TryResolveTrimmed(
                contract,
                field.Projections.Html.AccessibleDescription,
                requestedLanguage,
                politenessLevel,
                out var accessibleDescription)
                ? accessibleDescription
                : null;
    }

    private static void ValidateInputs(
        FormContract contract,
        FieldDefinition field,
        string requestedLanguage,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(field);
        if (field.FieldId == default)
        {
            throw new ArgumentException("FieldId must be provided.", nameof(field));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(requestedLanguage);

        if (!Enum.IsDefined(politenessLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(politenessLevel), politenessLevel, "Unknown politeness level.");
        }
    }

    private static bool TryResolveTrimmed(
        FormContract contract,
        IReadOnlyDictionary<string, string> text,
        string requestedLanguage,
        PolitenessLevel politenessLevel,
        out string resolved)
    {
        resolved = string.Empty;
        if (!LocalizedTextResolver.TryResolve(
                text,
                requestedLanguage,
                contract.DefaultLanguage,
                contract.SupportedLanguages,
                contract.StrictLocalization,
                politenessLevel,
                out var candidate)
            || string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        resolved = candidate.Trim();
        return true;
    }
}
