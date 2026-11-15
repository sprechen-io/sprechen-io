
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.LanguageContracts;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Katalogeingaben mit fester Priorität den Intent-Klassifizierern zu.
/// Zustimmungen behalten dabei Hinweise auf konkurrierende Befehle oder Ziele.
/// </summary>
internal sealed class FormsCatalogIntentParser : IFormsCatalogIntentParser
{
    private readonly FormsCatalogConfirmationIntentClassifier _confirmationIntents;
    private readonly FormsCatalogCompetingAffirmativeEvidenceClassifier _competingAffirmativeEvidence;
    private readonly FormsCatalogLanguageIntentClassifier _languageIntents;
    private readonly FormsCatalogNavigationIntentClassifier _navigationIntents;
    private readonly FormsCatalogQueryIntentClassifier _queryIntents;
    private readonly FormsCatalogOpenCandidateIntentClassifier _openCandidateIntents;
    private readonly FormsCatalogSelectionIntentClassifier _selectionIntents;

    public FormsCatalogIntentParser(
        IFormsCatalogIntentMatcher matcher,
        LanguageChangeTargetResolver languageTargets,
        ILanguageCatalog languageCatalog)
    {
        ArgumentNullException.ThrowIfNull(matcher);
        ArgumentNullException.ThrowIfNull(languageTargets);
        ArgumentNullException.ThrowIfNull(languageCatalog);
        var supportedIsoCodes = languageCatalog.SupportedLanguages
            .Select(static language => language.Value)
            .ToArray();
        _confirmationIntents = new FormsCatalogConfirmationIntentClassifier(matcher);
        _competingAffirmativeEvidence = new FormsCatalogCompetingAffirmativeEvidenceClassifier(
            matcher,
            languageTargets,
            supportedIsoCodes);
        _languageIntents = new FormsCatalogLanguageIntentClassifier(matcher, languageTargets, supportedIsoCodes);
        _navigationIntents = new FormsCatalogNavigationIntentClassifier(matcher);
        _queryIntents = new FormsCatalogQueryIntentClassifier(matcher);
        _openCandidateIntents = new FormsCatalogOpenCandidateIntentClassifier(matcher);
        _selectionIntents = new FormsCatalogSelectionIntentClassifier(matcher);
    }

    public FormsCatalogIntentResult Parse(
        FormsCatalog catalog,
        string transcript,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);

        var confirmation = _confirmationIntents.TryClassify(catalog, normalized, isoCode, politenessLevel);
        if (confirmation is not null)
        {
            return confirmation.Intent == FormsCatalogIntent.ConfirmYes
                ? confirmation with
                {
                    HasCompetingIntentEvidence = _competingAffirmativeEvidence.HasEvidence(
                        catalog,
                        normalized,
                        isoCode,
                        politenessLevel)
                }
                : confirmation;
        }

        return _languageIntents.TryClassify(catalog, normalized, isoCode, politenessLevel)
            ?? _navigationIntents.TryClassify(catalog, normalized, isoCode, politenessLevel)
            ?? _queryIntents.TryClassify(catalog, normalized, isoCode, politenessLevel)
            ?? _openCandidateIntents.TryClassify(catalog, normalized, isoCode, politenessLevel)
            // Erst nach den modellierten Befehlen, aber vor der freien Kandidatenauswahl: Ein
            // alleinstehender Sprachwunsch soll nicht zunächst als Anwendungsname behandelt
            // werden.
            ?? _languageIntents.TryClassifyBareLanguageName(normalized)
            ?? _selectionIntents.Classify(catalog, normalized, transcript, isoCode, politenessLevel);
    }

}
