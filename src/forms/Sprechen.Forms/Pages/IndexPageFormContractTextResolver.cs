using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Löst Vertragstexte in angefragter Sprache und Höflichkeitsstufe auf. Ohne Auflösung oder bei Leertext bleibt der Text leer.
/// </summary>
internal sealed class IndexPageFormContractTextResolver
{
    public string Resolve(
        LocalizedText? text,
        FormContract contract,
        string requestedLanguage,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedLanguage);

        if (text is null)
        {
            return string.Empty;
        }

        if (!LocalizedTextResolver.TryResolve(
                text,
                requestedLanguage,
                contract.DefaultLanguage,
                contract.SupportedLanguages,
                contract.StrictLocalization,
                politenessLevel,
                out var resolved))
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(resolved) ? string.Empty : resolved.Trim();
    }
}
