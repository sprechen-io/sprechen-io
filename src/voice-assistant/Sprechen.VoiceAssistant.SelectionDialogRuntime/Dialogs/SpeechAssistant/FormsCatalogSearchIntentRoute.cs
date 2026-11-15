namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Überführt eine Suche im Ausgangsbestand in einen revidierten Auswahlbestand mit erhaltenen
/// Kandidatennummern. Für Treffer wird eine weitere Auswahlansage vorbereitet. Eine erfolglose
/// Suche lässt den bisherigen Bestand bestehen.
/// </summary>
internal sealed class FormsCatalogSearchIntentRoute(ApplicationMatcher applicationMatcher)
{
    private readonly ApplicationMatcher _applicationMatcher =
        applicationMatcher ?? throw new ArgumentNullException(nameof(applicationMatcher));

    public FormsCatalogIntentHandlingResult Route(
        FormsCatalogDialogTurnContext turnContext,
        FormsCatalogIntentResult intentResult)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(intentResult);

        var searchResults = _applicationMatcher.Search(turnContext.Applications, intentResult.Query);
        if (searchResults.Count == 0)
        {
            var noSearchResultsPrompt = FormsCatalogPromptBuilder.GetNoSearchResultsPrompt(
                turnContext.PromptContext,
                intentResult.Query,
                turnContext.IsoCode,
                turnContext.PolitenessLevel);
            return new FormsCatalogIntentHandlingResult(
                SelectionDialogPhase.Listening,
                SelectionDialogCandidateTransition<ApplicationInfo>.Preserve(),
                noSearchResultsPrompt);
        }

        var resultIds = searchResults
            .Select(application => application.Id)
            .ToHashSet(StringComparer.Ordinal);
        var replacement = turnContext.InitialCandidates.Revise(
            turnContext.Candidates.Revision.Next(),
            turnContext.InitialCandidates.Candidates.Where(candidate => resultIds.Contains(candidate.Identifier)));
        var searchPrompt = replacement.Count == 1
            ? FormsCatalogPromptBuilder.GetSearchResultsPrompt(
                turnContext.PromptContext,
                replacement.Candidates,
                intentResult.Query,
                turnContext.IsoCode,
                turnContext.PolitenessLevel)
            : FormsCatalogPromptBuilder.GetAmbiguityPrompt(
                turnContext.PromptContext,
                replacement.Candidates,
                turnContext.IsoCode,
                turnContext.PolitenessLevel);
        return new FormsCatalogIntentHandlingResult(
            SelectionDialogPhase.Listening,
            replacement.Count > 1
                ? SelectionDialogCandidateTransition<ApplicationInfo>.ReplaceVisibleWithAmbiguity(replacement)
                : SelectionDialogCandidateTransition<ApplicationInfo>.ReplaceVisible(replacement),
            searchPrompt);
    }
}
