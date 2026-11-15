using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Löst die Beschriftung einer Option anhand von Sprache und Anrede auf und verwendet bei fehlendem Text ihren Code, damit die Option benannt bleibt.
/// </summary>
internal sealed class FormSurfaceOptionLabelResolver
{
    public string Resolve(
        FormContract contract,
        JsonElement optionElement,
        string fallbackCode,
        string requestedLanguage,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedLanguage);

        if (!Enum.IsDefined(politenessLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(politenessLevel), politenessLevel, "Unknown politeness level.");
        }

        if (!optionElement.TryGetProperty(ValueTypePropertyNames.DisplayName, out var displayNameElement)
            || displayNameElement.ValueKind != JsonValueKind.Object)
        {
            return fallbackCode;
        }

        var localized = JsonSerializer.Deserialize<LocalizedText>(displayNameElement.GetRawText());
        if (localized is not null
            && LocalizedTextResolver.TryResolve(
                localized,
                requestedLanguage,
                contract.DefaultLanguage,
                contract.SupportedLanguages,
                contract.StrictLocalization,
                politenessLevel,
                out var resolved)
            && !string.IsNullOrWhiteSpace(resolved))
        {
            return resolved.Trim();
        }

        return fallbackCode;
    }
}
