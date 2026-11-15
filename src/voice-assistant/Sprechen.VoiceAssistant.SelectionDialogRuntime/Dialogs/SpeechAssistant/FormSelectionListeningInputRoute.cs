namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Überführt die gemeinsame Behandlung von Auswahlbefehlen in einen formularbezogenen Schritt.
/// Unbehandelte Eingaben erhalten eine Rückfrage unter Beibehaltung des Kandidatenbestands.
/// </summary>
internal sealed class FormSelectionListeningInputRoute
{
    private readonly IFormsCatalogIntentParser _intentParser;

    public FormSelectionListeningInputRoute(IFormsCatalogIntentParser intentParser)
    {
        _intentParser = intentParser ?? throw new ArgumentNullException(nameof(intentParser));
    }

    public async Task<FormSelectionIntentHandlingResult> ProcessAsync(
        FormSelectionDialogSession session,
        SelectionDialogCommonIntentHandler<FormInfo> intentHandler,
        string transcript,
        SelectionDialogTurnSnapshot<FormInfo> candidates,
        SelectionDialogPrompt? lastPrompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(intentHandler);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);

        var intentResult = _intentParser.Parse(
            session.Catalog,
            transcript,
            session.IsoCode,
            session.PolitenessLevel);
        var commonResult = await intentHandler.HandleAsync(
                FormsCatalogSelectionIntentMapper.Map(intentResult),
                candidates,
                lastPrompt,
                cancellationToken)
            .ConfigureAwait(false);

        if (commonResult.Handled)
        {
            return new FormSelectionIntentHandlingResult(
                commonResult.Step.NextPhase,
                commonResult.Step.CandidateTransition,
                commonResult.Step.SpokenPrompt,
                commonResult.Step.CompletionReason);
        }

        return new FormSelectionIntentHandlingResult(
            SelectionDialogPhase.Listening,
            SelectionDialogCandidateTransition<FormInfo>.Preserve(),
            FormSelectionPromptBuilder.GetUnrecognizedPrompt(
                session.PromptContext,
                session.IsoCode,
                session.PolitenessLevel));
    }
}
