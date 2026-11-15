using Sprechen.Service.Core.Services;

namespace Sprechen.Service.Core.Tests;

internal sealed class TestSupportedLanguagesService : ISupportedLanguagesService
{
    private readonly HashSet<string> _isoCodes = new(SupportedLanguagesOptions.AllowedIsoCodes, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SupportedLanguage> Languages { get; } = new[]
    {
        new SupportedLanguage("de", "Deutsch"),
        new SupportedLanguage("en", "English"),
        new SupportedLanguage("fr", "français"),
        new SupportedLanguage("es", "español"),
        new SupportedLanguage("it", "italiano")
    };

    public string DefaultIsoCode => "de";

    public bool IsSupportedIsoCode(string isoCode)
    {
        var normalized = LanguageCodeNormalizer.NormalizeIso6391(isoCode);
        return normalized is not null && _isoCodes.Contains(normalized);
    }

    public string GetEffectiveIsoCode(string? rawLanguageCode)
    {
        if (string.IsNullOrWhiteSpace(rawLanguageCode))
        {
            throw new InvalidOperationException("LanguageCode is required.");
        }

        var normalized = LanguageCodeNormalizer.NormalizeIso6391(rawLanguageCode);
        if (normalized is null || !_isoCodes.Contains(normalized))
        {
            throw new InvalidOperationException($"Unsupported language '{rawLanguageCode}'.");
        }

        return normalized;
    }
}
