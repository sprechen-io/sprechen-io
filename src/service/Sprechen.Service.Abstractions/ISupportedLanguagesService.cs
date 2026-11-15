namespace Sprechen.Service.Abstractions;

/// <summary>
/// Liefert unterstützte Sprachen, die Standardsprache und den wirksamen Sprachcode.
/// </summary>
public interface ISupportedLanguagesService
{
    IReadOnlyList<SupportedLanguage> Languages { get; }

    string DefaultIsoCode { get; }

    bool IsSupportedIsoCode(string isoCode);

    string GetEffectiveIsoCode(string? rawLanguageCode);
}
