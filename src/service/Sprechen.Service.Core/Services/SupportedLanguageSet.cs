namespace Sprechen.Service.Core.Services;

/// <summary>
/// Speichert Standardsprache und unterstützte Sprachcodes und ermöglicht normalisierte Mitgliedschaftsprüfungen.
/// </summary>
internal sealed class SupportedLanguageSet
{
    private readonly HashSet<string> _supportedIsoCodes;

    public SupportedLanguageSet(string defaultIsoCode, IReadOnlyList<string> isoCodes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultIsoCode);
        ArgumentNullException.ThrowIfNull(isoCodes);

        DefaultIsoCode = defaultIsoCode;
        IsoCodes = isoCodes;
        _supportedIsoCodes = new HashSet<string>(isoCodes, StringComparer.OrdinalIgnoreCase);
    }

    public string DefaultIsoCode { get; }

    public IReadOnlyList<string> IsoCodes { get; }

    public bool Contains(string rawIsoCode)
    {
        var normalized = LanguageCodeNormalizer.NormalizeIso6391(rawIsoCode);
        return normalized is not null && _supportedIsoCodes.Contains(normalized);
    }

    public string SupportedIsoCodesDisplay()
        => string.Join(", ", IsoCodes);
}
