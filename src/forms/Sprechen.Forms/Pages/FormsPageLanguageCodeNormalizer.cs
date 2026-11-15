using Sprechen.Contracts.FormSurfaces;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Normiert Sprachcodes über `Iso6391LanguageCode`. Leere oder nicht normierbare Eingaben liefern die leere Zeichenfolge.
/// </summary>
internal sealed class FormsPageLanguageCodeNormalizer
{
    public string NormalizeOrEmpty(string? value)
    {
        return TryNormalize(value, out var normalized) ? normalized : string.Empty;
    }

    public bool TryNormalize(string? value, out string normalized)
    {
        return Iso6391LanguageCode.TryNormalize(Normalize(value), out normalized);
    }

    private string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
