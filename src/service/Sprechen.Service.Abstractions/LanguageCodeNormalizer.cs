namespace Sprechen.Service.Abstractions;

/// <summary>
/// Normalisiert Sprachcodes und ergänzt bekannte reine Sprachcodes um einen Regionsteil.
/// </summary>
public static class LanguageCodeNormalizer
{
    private static readonly IReadOnlyDictionary<string, string> Bcp47ByIso639 = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["de"] = "de-DE",
        ["en"] = "en-US",
        ["es"] = "es-ES",
        ["fr"] = "fr-FR",
        ["pt"] = "pt-BR",
        ["it"] = "it-IT",
        ["ja"] = "ja-JP",
        ["ko"] = "ko-KR",
        ["ru"] = "ru-RU",
        ["tr"] = "tr-TR",
        ["vi"] = "vi-VN",
        ["ar"] = "ar-XA",
        ["hi"] = "hi-IN",
        ["bn"] = "bn-IN",
        ["gu"] = "gu-IN",
        ["kn"] = "kn-IN",
        ["ml"] = "ml-IN",
        ["mr"] = "mr-IN",
        ["ta"] = "ta-IN",
        ["te"] = "te-IN",
        ["nl"] = "nl-NL",
        ["pl"] = "pl-PL",
        ["th"] = "th-TH",
        ["cmn"] = "cmn-CN"
    };

    public static string? NormalizeIso6391(string? raw)
    {
        return LanguageCode.TryParseIso6391(raw, out var code) ? code.Value : null;
    }

    public static string? NormalizeBcp47(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Contains('-', StringComparison.Ordinal))
        {
            return trimmed;
        }

        var lowerInvariant = trimmed.ToLowerInvariant();
        return Bcp47ByIso639.TryGetValue(lowerInvariant, out var normalized) ? normalized : trimmed;
    }
}
