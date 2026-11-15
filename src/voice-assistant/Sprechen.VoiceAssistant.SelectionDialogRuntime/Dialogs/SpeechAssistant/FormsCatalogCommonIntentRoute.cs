namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet allgemeine Katalogbefehle an die gemeinsame Auswahlbehandlung weiter.
/// Katalogspezifische Suche, Beschreibung und Sprachwechsel können daneben eigene Abläufe
/// behalten.
/// </summary>
internal sealed class FormsCatalogCommonIntentRoute
{
    public async Task<FormsCatalogIntentHandlingResult> RouteAsync(
        FormsCatalogDialogTurnContext turnContext,
        FormsCatalogIntentResult intentResult,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(intentResult);

        var commonResult = await turnContext.IntentHandler.HandleAsync(
                FormsCatalogSelectionIntentMapper.Map(intentResult),
                turnContext.Candidates,
                turnContext.LastPrompt,
                cancellationToken)
            .ConfigureAwait(false);

        if (commonResult.Handled)
        {
            return new FormsCatalogIntentHandlingResult(
                commonResult.Step.NextPhase,
                commonResult.Step.CandidateTransition,
                commonResult.Step.SpokenPrompt,
                commonResult.Step.CompletionReason);
        }

        return new FormsCatalogIntentHandlingResult(
            SelectionDialogPhase.Listening,
            SelectionDialogCandidateTransition<ApplicationInfo>.Preserve(),
            FormsCatalogPromptBuilder.GetUnrecognizedPrompt(
                turnContext.PromptContext,
                turnContext.IsoCode,
                turnContext.PolitenessLevel));
    }
}
