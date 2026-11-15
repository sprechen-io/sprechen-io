namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Löst Beschreibungswünsche gegen die auswählbaren Anwendungen auf. Mehrdeutige Namen bleiben
/// zur Klärung erhalten, ohne bereits den Start einer Anwendung auszulösen.
/// </summary>
internal sealed class FormsCatalogDescribeIntentRoute(ApplicationMatcher applicationMatcher)
{
    private readonly ApplicationMatcher _applicationMatcher =
        applicationMatcher ?? throw new ArgumentNullException(nameof(applicationMatcher));

    public FormsCatalogIntentHandlingResult Route(
        FormsCatalogDialogTurnContext turnContext,
        FormsCatalogIntentResult intentResult)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(intentResult);

        var describeMatch = _applicationMatcher.CandidateMatcher.Match(
            turnContext.SelectableCandidates,
            intentResult.TargetName);
        if (describeMatch.Candidates.Length == 0)
        {
            return CreateNoMatchResult(turnContext, intentResult.TargetName);
        }

        if (describeMatch.Candidates.Length > 1)
        {
            var ambiguity = turnContext.SelectableCandidates.Retain(describeMatch.Candidates);
            return new FormsCatalogIntentHandlingResult(
                SelectionDialogPhase.Listening,
                SelectionDialogCandidateTransition<ApplicationInfo>.RetainAmbiguity(ambiguity),
                FormsCatalogPromptBuilder.GetAmbiguityPrompt(
                    turnContext.PromptContext,
                    ambiguity.Candidates,
                    turnContext.IsoCode,
                    turnContext.PolitenessLevel));
        }

        return CreateDescriptionResult(turnContext, describeMatch.Candidates[0].Item, resetCandidates: false);
    }

    public FormsCatalogIntentHandlingResult CompletePendingDescription(
        FormsCatalogDialogTurnContext turnContext,
        SelectionCandidate<ApplicationInfo> selected)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(selected);

        return CreateDescriptionResult(turnContext, selected.Item, resetCandidates: true);
    }

    private static FormsCatalogIntentHandlingResult CreateDescriptionResult(
        FormsCatalogDialogTurnContext turnContext,
        ApplicationInfo application,
        bool resetCandidates)
    {
        var descriptionPrompt = string.IsNullOrWhiteSpace(application.Description)
            ? FormsCatalogPromptBuilder.GetNoDescriptionPrompt(
                turnContext.PromptContext,
                turnContext.IsoCode,
                turnContext.PolitenessLevel)
            : SelectionDialogPrompt.FromText(application.Description);
        return new FormsCatalogIntentHandlingResult(
            SelectionDialogPhase.Listening,
            resetCandidates
                ? SelectionDialogCandidateTransition<ApplicationInfo>.ResetToInitial()
                : SelectionDialogCandidateTransition<ApplicationInfo>.Preserve(),
            descriptionPrompt);
    }

    private static FormsCatalogIntentHandlingResult CreateNoMatchResult(
        FormsCatalogDialogTurnContext turnContext,
        string? targetName)
    {
        var noDescribeMatchPrompt = FormsCatalogPromptBuilder.GetNoMatchPrompt(
            turnContext.PromptContext,
            targetName,
            turnContext.IsoCode,
            turnContext.PolitenessLevel);
        return new FormsCatalogIntentHandlingResult(
            SelectionDialogPhase.Listening,
            SelectionDialogCandidateTransition<ApplicationInfo>.Preserve(),
            noDescribeMatchPrompt);
    }
}
