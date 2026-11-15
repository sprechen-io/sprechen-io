using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, ob ein mitgebrachter Transkripttext beim Katalogeinstieg als Auftrag übernommen werden
/// kann. Erkannte Ja-/Nein-Antworten und Wiederholungswünsche werden dabei vor einer
/// entsprechenden Rückfrage verworfen.
/// </summary>
internal sealed class FormsCatalogInitialTranscriptResolver
{
    private readonly IFormsCatalogIntentParser _intentParser;
    private readonly ApplicationMatcher _applicationMatcher;

    public FormsCatalogInitialTranscriptResolver(
        IFormsCatalogIntentParser intentParser,
        ApplicationMatcher applicationMatcher)
    {
        _intentParser = intentParser ?? throw new ArgumentNullException(nameof(intentParser));
        _applicationMatcher = applicationMatcher ?? throw new ArgumentNullException(nameof(applicationMatcher));
    }

    public string? Resolve(
        string? initialTranscript,
        FormsCatalog catalog,
        IReadOnlyList<ApplicationInfo> visibleApplications,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(visibleApplications);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        if (string.IsNullOrWhiteSpace(initialTranscript))
        {
            return null;
        }

        var normalizedInitialTranscript = initialTranscript.Trim();
        var initialIntentResult = _intentParser.Parse(
            catalog,
            normalizedInitialTranscript,
            isoCode,
            politenessLevel);
        var isExecutableAtEntry = initialIntentResult.Intent switch
        {
            FormsCatalogIntent.List or
            FormsCatalogIntent.Search or
            FormsCatalogIntent.SelectByNumber or
            FormsCatalogIntent.Help or
            FormsCatalogIntent.Describe or
            FormsCatalogIntent.Cancel or
            FormsCatalogIntent.ChangeLanguage => true,

            FormsCatalogIntent.Unknown or
            FormsCatalogIntent.SelectByName =>
                _applicationMatcher.CandidateMatcher.Match(
                    _applicationMatcher.CandidateMatcher.CreateSnapshot(
                        visibleApplications,
                        SelectionRevision.Initial),
                    initialIntentResult.TargetName).Candidates.Length > 0,

            FormsCatalogIntent.Repeat or
            FormsCatalogIntent.ConfirmYes or
            FormsCatalogIntent.ConfirmNo or
            FormsCatalogIntent.OpenCandidate => false,

            _ => false
        };

        return isExecutableAtEntry
            ? normalizedInitialTranscript
            : null;
    }

    public SpeechTranscript? Resolve(
        SpeechTranscript? initialTranscript,
        FormsCatalog catalog,
        IReadOnlyList<ApplicationInfo> visibleApplications,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        if (initialTranscript is null)
        {
            return null;
        }

        var resolvedText = Resolve(
            initialTranscript.Text,
            catalog,
            visibleApplications,
            isoCode,
            politenessLevel);
        return resolvedText is null
            ? null
            : initialTranscript with { Text = resolvedText };
    }
}
