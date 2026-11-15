namespace Sprechen.Service.Deepgram.Options;

/// <summary>
/// Ermittelt normalisierte Sprachcodes für die TTS-Konfigurationsprüfung und verwendet bei leerer Auswahl Ersatzwerte.
/// </summary>
internal sealed class DeepgramSupportedIsoCodeResolver
{
    public IReadOnlySet<string> Resolve(SupportedLanguagesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddConfiguredIsoCodes(options, supported);
        if (supported.Count > 0)
        {
            return supported;
        }

        var fallback = LanguageCodeNormalizer.NormalizeIso6391(options.DefaultIsoCode);
        if (fallback is not null)
        {
            supported.Add(fallback);
            return supported;
        }

        return SupportedLanguagesOptions.AllowedIsoCodes;
    }

    private static void AddConfiguredIsoCodes(SupportedLanguagesOptions options, ISet<string> supported)
    {
        foreach (var raw in options.IsoCodes ?? Array.Empty<string>())
        {
            var iso = LanguageCodeNormalizer.NormalizeIso6391(raw);
            if (iso is not null)
            {
                supported.Add(iso);
            }
        }
    }
}
