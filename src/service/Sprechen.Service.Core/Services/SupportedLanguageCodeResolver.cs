namespace Sprechen.Service.Core.Services;

/// <summary>
/// Prüft einen erforderlichen Sprachcode gegen die konfigurierte Sprachmenge und liefert ihn normalisiert zurück.
/// </summary>
internal sealed class SupportedLanguageCodeResolver
{
    private readonly SupportedLanguageSet _languageSet;

    public SupportedLanguageCodeResolver(SupportedLanguageSet languageSet)
    {
        _languageSet = languageSet ?? throw new ArgumentNullException(nameof(languageSet));
    }

    public string ResolveRequired(string? rawLanguageCode)
    {
        if (string.IsNullOrWhiteSpace(rawLanguageCode))
        {
            throw new InvalidOperationException(
                $"LanguageCode is required. Supported: {_languageSet.SupportedIsoCodesDisplay()}.");
        }

        var normalized = LanguageCodeNormalizer.NormalizeIso6391(rawLanguageCode);
        if (normalized is null)
        {
            throw new InvalidOperationException(
                $"LanguageCode must be ISO 639-1 (two letters). Supported: {_languageSet.SupportedIsoCodesDisplay()}.");
        }

        if (!_languageSet.Contains(normalized))
        {
            throw new InvalidOperationException(
                $"LanguageCode '{normalized}' is not supported. Supported: {_languageSet.SupportedIsoCodesDisplay()}.");
        }

        return normalized;
    }
}
