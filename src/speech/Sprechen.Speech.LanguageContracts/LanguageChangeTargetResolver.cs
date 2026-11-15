namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Ordnet Sprachwechselargumente einer unterstützten Sprache zu und liefert lokalisierte Bezeichnungen.
/// </summary>
public sealed class LanguageChangeTargetResolver
{
    private readonly LanguageChangeCandidateNormalizer _candidateNormalizer;
    private readonly ILanguageCatalog _languageCatalog;
    private readonly LanguageTargetCandidateMatcher _targetMatcher;

    public LanguageChangeTargetResolver(ILanguageCatalog languageCatalog)
    {
        _languageCatalog = languageCatalog ?? throw new ArgumentNullException(nameof(languageCatalog));
        _candidateNormalizer = new LanguageChangeCandidateNormalizer(_languageCatalog);
        _targetMatcher = new LanguageTargetCandidateMatcher(_languageCatalog);
    }

    public bool TryResolve(
        string? rawArgument,
        string displayLanguageIsoCode,
        out string targetIsoCode,
        out string displayName)
    {
        targetIsoCode = string.Empty;
        displayName = string.Empty;

        var normalizedCandidate = _candidateNormalizer.Normalize(rawArgument);
        if (normalizedCandidate.Length == 0)
        {
            return false;
        }

        var displayLanguage = ResolveDisplayLanguage(displayLanguageIsoCode);

        foreach (var language in _languageCatalog.SupportedLanguages)
        {
            if (!_targetMatcher.Matches(language, displayLanguage, normalizedCandidate))
            {
                continue;
            }

            targetIsoCode = language.Value;
            displayName = _languageCatalog.GetDisplayName(language, displayLanguage);
            return true;
        }

        return TryResolveWithinOneEdit(normalizedCandidate, displayLanguage, out targetIsoCode, out displayName);
    }

    /// <summary>
    /// Akzeptiert nach erfolglosem exaktem Vergleich einen Näherungstreffer für genau eine Sprache.
    /// </summary>
    /// <remarks>
    /// Mehrere passende Sprachen ergeben keinen Treffer. Die Zeichentoleranz und Mindestlänge
    /// prüft der Kandidatenvergleich. Dieser Schritt erzwingt die Eindeutigkeit über alle Sprachen.
    /// </remarks>
    private bool TryResolveWithinOneEdit(
        string normalizedCandidate,
        LanguageCode displayLanguage,
        out string targetIsoCode,
        out string displayName)
    {
        targetIsoCode = string.Empty;
        displayName = string.Empty;

        LanguageCode? single = null;
        foreach (var language in _languageCatalog.SupportedLanguages)
        {
            if (!_targetMatcher.MatchesWithinOneEdit(language, displayLanguage, normalizedCandidate))
            {
                continue;
            }

            if (single is not null)
            {
                return false;
            }

            single = language;
        }

        if (single is not { } resolved)
        {
            return false;
        }

        targetIsoCode = resolved.Value;
        displayName = _languageCatalog.GetDisplayName(resolved, displayLanguage);
        return true;
    }

    public string FormatSupportedLanguages(string displayLanguageIsoCode)
    {
        var displayLanguage = ResolveDisplayLanguage(displayLanguageIsoCode);
        return string.Join(
            ", ",
            _languageCatalog.SupportedLanguages
                .Select(language => _languageCatalog.GetDisplayName(language, displayLanguage))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
    }

    public string FormatSupportedLanguages(
        IEnumerable<string> supportedIsoCodes,
        string displayLanguageIsoCode)
    {
        ArgumentNullException.ThrowIfNull(supportedIsoCodes);

        var displayLanguage = ResolveDisplayLanguage(displayLanguageIsoCode);
        return string.Join(
            ", ",
            supportedIsoCodes
                .Select(IsoCode.TryNormalizeIso6391)
                .Where(static isoCode => !string.IsNullOrWhiteSpace(isoCode))
                .Distinct(StringComparer.Ordinal)
                .Select(static isoCode => LanguageCode.TryParseIso6391(isoCode, out var language)
                    ? language
                    : default)
                .Where(language => language != default && _languageCatalog.IsSupported(language))
                .Select(language => _languageCatalog.GetDisplayName(language, displayLanguage))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
    }

    public string GetDisplayName(string targetIsoCode, string displayLanguageIsoCode)
    {
        var language = ResolveSupportedLanguage(targetIsoCode);
        var displayLanguage = ResolveDisplayLanguage(displayLanguageIsoCode);
        return _languageCatalog.GetDisplayName(language, displayLanguage);
    }

    private LanguageCode ResolveDisplayLanguage(string displayLanguageIsoCode)
    {
        if (LanguageCode.TryParseIso6391(displayLanguageIsoCode, out var displayLanguage)
            && _languageCatalog.IsSupported(displayLanguage))
        {
            return displayLanguage;
        }

        return _languageCatalog.DefaultLanguage;
    }

    private LanguageCode ResolveSupportedLanguage(string targetIsoCode)
    {
        if (!LanguageCode.TryParseIso6391(targetIsoCode, out var language)
            || !_languageCatalog.IsSupported(language))
        {
            throw new InvalidOperationException($"Language '{targetIsoCode}' is not supported.");
        }

        return language;
    }
}
