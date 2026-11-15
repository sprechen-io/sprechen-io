namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Stellt die gemeinsame Normalisierung von Sprachkennungen über die Formprüfung des Sprachcodewerts bereit.
/// </summary>
public static class IsoCode
{
    public static string NormalizeIso6391(string raw)
    {
        return LanguageCode.ParseIso6391(raw).Value;
    }

    public static string? TryNormalizeIso6391(string? raw)
    {
        return LanguageCode.TryParseIso6391(raw, out var code) ? code.Value : null;
    }
}



