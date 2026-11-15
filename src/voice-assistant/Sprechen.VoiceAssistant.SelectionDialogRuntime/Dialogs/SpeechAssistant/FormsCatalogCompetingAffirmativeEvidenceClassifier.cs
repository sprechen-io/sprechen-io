using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.LanguageContracts;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ermittelt Befehls-, Zahl- und Kandidatenhinweise neben einer erkannten Zustimmung. Der Parser
/// kann dadurch zusätzliche Hinweise an der Bestätigung kennzeichnen, bevor die Auswahl über ihre
/// Annahme entscheidet.
/// </summary>
internal sealed class FormsCatalogCompetingAffirmativeEvidenceClassifier
{
    private readonly IFormsCatalogIntentMatcher _matcher;
    private readonly FormsCatalogAffirmativePhraseStripper _affirmativeStripper = new();
    private readonly FormsCatalogLanguageIntentClassifier _languageIntents;
    private readonly FormsCatalogNavigationIntentClassifier _navigationIntents;
    private readonly FormsCatalogQueryIntentClassifier _queryIntents;
    private readonly FormsCatalogOpenCandidateIntentClassifier _openCandidateIntents;
    private readonly FormsCatalogSelectionIntentClassifier _selectionIntents;
    private readonly SelectionCandidateMatcher<CatalogCandidateIdentity> _candidateMatcher = new(
        static candidate => candidate.Id,
        static candidate => candidate.DisplayName,
        TextNormalizer.NormalizeForCompactMatching);

    public FormsCatalogCompetingAffirmativeEvidenceClassifier(
        IFormsCatalogIntentMatcher matcher,
        LanguageChangeTargetResolver languageTargets,
        IReadOnlyList<string> supportedIsoCodes)
    {
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
        _languageIntents = new FormsCatalogLanguageIntentClassifier(matcher, languageTargets, supportedIsoCodes);
        _navigationIntents = new FormsCatalogNavigationIntentClassifier(matcher);
        _queryIntents = new FormsCatalogQueryIntentClassifier(matcher);
        _openCandidateIntents = new FormsCatalogOpenCandidateIntentClassifier(matcher);
        _selectionIntents = new FormsCatalogSelectionIntentClassifier(matcher);
    }

    public bool HasEvidence(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        // Befehle werden im Original und im Rest geprüft, damit ihre modellierten Phrasen
        // wirksam bleiben. Der Kandidatenvergleich erhält nur den Rest: Eine Zustimmung allein
        // soll nicht durch Teilzeichenfolgenvergleich als zusätzlicher Zielhinweis zählen.
        var withoutAffirmative = _affirmativeStripper.Remove(
            catalog,
            normalized,
            isoCode,
            politenessLevel);
        return HasModeledIntentEvidence(catalog, normalized, isoCode, politenessLevel)
            || HasModeledIntentEvidence(catalog, withoutAffirmative, isoCode, politenessLevel)
            || HasCandidateEvidence(catalog, withoutAffirmative, isoCode, politenessLevel);
    }

    private bool HasModeledIntentEvidence(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return _matcher.IsConfirmNo(catalog, normalized, isoCode, politenessLevel)
            || _matcher.IsCancel(catalog, normalized, isoCode, politenessLevel)
            || _languageIntents.TryClassify(catalog, normalized, isoCode, politenessLevel) is not null
            || _navigationIntents.TryClassify(catalog, normalized, isoCode, politenessLevel) is not null
            || _queryIntents.TryClassify(catalog, normalized, isoCode, politenessLevel) is not null
            || _openCandidateIntents.TryClassify(catalog, normalized, isoCode, politenessLevel) is not null
            || _selectionIntents.Classify(
                    catalog,
                    normalized,
                    normalized,
                    isoCode,
                    politenessLevel).Intent == FormsCatalogIntent.SelectByNumber;
    }

    private bool HasCandidateEvidence(
        FormsCatalog catalog,
        string target,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        var snapshot = _candidateMatcher.CreateSnapshot(
            CreateCandidateIdentities(catalog, isoCode, politenessLevel),
            SelectionRevision.Initial);
        return _candidateMatcher.Match(snapshot, target).Candidates.Length != 0;
    }

    private static IEnumerable<CatalogCandidateIdentity> CreateCandidateIdentities(
        FormsCatalog catalog,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        var textResolver = new FormsCatalogTextResolver();
        foreach (var application in catalog.Applications)
        {
            yield return new CatalogCandidateIdentity(
                $"application:{application.ApplicationId}",
                textResolver.ResolveApplicationDisplayName(application, isoCode, politenessLevel));

            foreach (var form in application.Forms)
            {
                yield return new CatalogCandidateIdentity(
                    $"form:{application.ApplicationId}:{form.FormId}",
                    textResolver.ResolveFormDisplayName(application, form, isoCode, politenessLevel));
            }
        }
    }

}
