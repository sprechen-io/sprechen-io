using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Löst Texte einer Feldgruppe anhand von Sprache und Anrede auf, damit ihre Darstellung die Beschriftungen des Vertrags verwendet.
/// </summary>
internal sealed class FormFieldGroupMetadataProjector
{
    public FormSurfaceFieldGroupInfo Project(
        FormContract contract,
        FieldGroupDefinition fieldGroup,
        string requestedLanguage,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(fieldGroup);
        if (fieldGroup.FieldGroupId == default)
        {
            throw new ArgumentException("FieldGroupId must be provided.", nameof(fieldGroup));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(requestedLanguage);

        if (!Enum.IsDefined(politenessLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(politenessLevel), politenessLevel, "Unknown politeness level.");
        }

        var fieldGroupId = fieldGroup.FieldGroupId.Value;
        var title = TryResolveTrimmed(contract, fieldGroup.Title, requestedLanguage, politenessLevel, out var resolvedTitle)
            ? resolvedTitle
            : fieldGroupId;
        var voiceSummary = fieldGroup.VoiceSummary is not null
            && TryResolveTrimmed(contract, fieldGroup.VoiceSummary, requestedLanguage, politenessLevel, out var resolvedSummary)
                ? resolvedSummary
                : null;
        var description = fieldGroup.Description is not null
            && TryResolveTrimmed(contract, fieldGroup.Description, requestedLanguage, politenessLevel, out var resolvedDescription)
                ? resolvedDescription
                : null;

        return new FormSurfaceFieldGroupInfo(fieldGroupId, title, description, voiceSummary);
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
