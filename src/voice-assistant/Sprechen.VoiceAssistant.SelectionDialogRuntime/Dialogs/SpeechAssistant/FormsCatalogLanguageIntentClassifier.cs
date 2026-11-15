using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.LanguageContracts;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Erkennt modellierte Sprachwechselbefehle auch außerhalb der aktuellen Sitzungssprache. Die
/// getrennte Erkennung alleinstehender Sprachwünsche erlaubt dem Parser, modellierten Befehlen
/// weiterhin Vorrang zu geben.
/// </summary>
internal sealed class FormsCatalogLanguageIntentClassifier
{
    private readonly IFormsCatalogIntentMatcher _matcher;
    private readonly LanguageChangeTargetResolver _languageTargets;
    private readonly IReadOnlyList<string> _supportedIsoCodes;

    public FormsCatalogLanguageIntentClassifier(
        IFormsCatalogIntentMatcher matcher,
        LanguageChangeTargetResolver languageTargets,
        IReadOnlyList<string> supportedIsoCodes)
    {
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
        _languageTargets = languageTargets ?? throw new ArgumentNullException(nameof(languageTargets));
        _supportedIsoCodes = supportedIsoCodes ?? throw new ArgumentNullException(nameof(supportedIsoCodes));
    }

    public FormsCatalogIntentResult? TryClassify(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        // Sprachwechselbefehle werden in allen unterstützten Sprachen geprüft, damit ein
        // Wechselwunsch keine Kenntnis der bisherigen Dialogsprache voraussetzt. Innerhalb jedes
        // Prüfdurchlaufs hat die Sitzungssprache bei gemeinsamen Phrasen Vorrang.
        foreach (var candidateIso in EnumerateMatchLanguages(isoCode))
        {
            var changeLanguageTarget = _matcher.TryExtractChangeLanguageTarget(
                catalog,
                normalized,
                candidateIso,
                politenessLevel);
            if (changeLanguageTarget is not null)
            {
                return new FormsCatalogIntentResult(
                    FormsCatalogIntent.ChangeLanguage,
                    LanguageArgument: changeLanguageTarget);
            }
        }

        foreach (var candidateIso in EnumerateMatchLanguages(isoCode))
        {
            if (_matcher.IsChangeLanguage(catalog, normalized, candidateIso, politenessLevel))
            {
                return new FormsCatalogIntentResult(FormsCatalogIntent.ChangeLanguage);
            }
        }

        return null;
    }

    /// <summary>
    /// Erkennt einen Sprachwunsch ohne modelliertes Befehlspräfix. Der Zielresolver vergleicht
    /// den gesamten normalisierten Rest nach Entfernung bekannter Randwörter, statt beliebige
    /// Sprachnamen im Text zu suchen. Der getrennte Einstieg hält diese zusätzliche Erkennung aus
    /// der Prüfung konkurrierender Zustimmungsbefehle heraus.
    /// </summary>
    internal FormsCatalogIntentResult? TryClassifyBareLanguageName(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        foreach (var displayIso in _supportedIsoCodes)
        {
            if (_languageTargets.TryResolve(normalized, displayIso, out var targetIsoCode, out _)
                && !string.IsNullOrWhiteSpace(targetIsoCode))
            {
                return new FormsCatalogIntentResult(
                    FormsCatalogIntent.ChangeLanguage,
                    LanguageArgument: normalized);
            }
        }

        return null;
    }

    private IEnumerable<string> EnumerateMatchLanguages(string isoCode)
    {
        yield return isoCode;
        foreach (var candidate in _supportedIsoCodes)
        {
            if (!string.Equals(candidate, isoCode, StringComparison.OrdinalIgnoreCase))
            {
                yield return candidate;
            }
        }
    }
}
