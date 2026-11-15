namespace Sprechen.Forms.Pages;

/// <summary>
/// Sucht im Accept-Language-Headerwert den ersten Eintrag, dessen Sprachtag einer verfügbaren Sprache entspricht. Sonst leer.
/// </summary>
internal sealed class FormsPageAcceptedLanguageHeaderResolver
{
    private readonly FormsPageLanguageAvailabilityResolver _languageAvailabilityResolver;

    public FormsPageAcceptedLanguageHeaderResolver(FormsPageLanguageAvailabilityResolver languageAvailabilityResolver)
    {
        _languageAvailabilityResolver = languageAvailabilityResolver
            ?? throw new ArgumentNullException(nameof(languageAvailabilityResolver));
    }

    public string Resolve(string? acceptedLanguageHeader, IReadOnlyList<string> availableLanguages)
    {
        if (string.IsNullOrWhiteSpace(acceptedLanguageHeader))
        {
            return string.Empty;
        }

        foreach (var acceptedLanguageEntry in SplitAcceptedLanguageEntries(acceptedLanguageHeader))
        {
            var languageTag = acceptedLanguageEntry.Split(';', 2, StringSplitOptions.TrimEntries)[0];
            var resolved = _languageAvailabilityResolver.ResolveAvailableLanguageOrEmpty(languageTag, availableLanguages);
            if (resolved.Length > 0)
            {
                return resolved;
            }
        }

        return string.Empty;
    }

    private string[] SplitAcceptedLanguageEntries(string acceptedLanguageHeader)
    {
        return acceptedLanguageHeader.Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
