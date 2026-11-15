namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Führt die zusammengesetzte Formularauswahl mit der generischen Auswahllaufzeit aus.
/// Formularspezifische Konfiguration und gemeinsame Dialogsteuerung bleiben dadurch getrennt.
/// </summary>
internal sealed class FormSelectionDialogRuntimeRunner
{
    private readonly FormSelectionRuntimeDefinitionFactory _definitionFactory;

    public FormSelectionDialogRuntimeRunner(
        SelectionDialogSpeechTurnRunner speech,
        IFormsCatalogIntentParser intentParser,
        IFormSelectionMatcher matcher,
        ISpeechAssistantTrace speechAssistantTrace)
        : this(
            speech,
            intentParser,
            matcher,
            speechAssistantTrace,
            new FormSelectionTransitionPolicy(new SelectionConfirmationPolicyEvaluator()))
    {
    }

    public FormSelectionDialogRuntimeRunner(
        SelectionDialogSpeechTurnRunner speech,
        IFormsCatalogIntentParser intentParser,
        IFormSelectionMatcher matcher,
        ISpeechAssistantTrace speechAssistantTrace,
        FormSelectionTransitionPolicy transitionPolicy)
    {
        _definitionFactory = new FormSelectionRuntimeDefinitionFactory(
            speech ?? throw new ArgumentNullException(nameof(speech)),
            intentParser ?? throw new ArgumentNullException(nameof(intentParser)),
            matcher ?? throw new ArgumentNullException(nameof(matcher)),
            speechAssistantTrace ?? throw new ArgumentNullException(nameof(speechAssistantTrace)),
            transitionPolicy ?? throw new ArgumentNullException(nameof(transitionPolicy)));
    }

    public async Task<FormSelectionDialogResult> RunAsync(
        FormSelectionDialogSession session,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        var runtime = new SelectionDialogRuntime<FormInfo, FormSelectionDialogResult>(
            _definitionFactory.Create(session, correlationId));

        return await runtime.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }
}
