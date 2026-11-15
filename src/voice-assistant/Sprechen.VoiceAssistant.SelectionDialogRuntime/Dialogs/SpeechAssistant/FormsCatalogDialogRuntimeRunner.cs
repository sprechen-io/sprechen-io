using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Führt die Anwendungsauswahl mit neuem Katalogzustand in der generischen Laufzeit aus.
/// Eine für den Neustart angeforderte Sprache wird getrennt vom Auswahlergebnis zurückgegeben.
/// </summary>
internal sealed class FormsCatalogDialogRuntimeRunner
{
    private readonly FormsCatalogRuntimeDefinitionFactory _definitionFactory;

    public FormsCatalogDialogRuntimeRunner(
        SelectionDialogSpeechTurnRunner speech,
        IFormsCatalogIntentParser intentParser,
        FormsCatalogDialogIntentRouter intentRouter,
        ISpeechAssistantTrace speechAssistantTrace)
        : this(
            speech,
            intentParser,
            intentRouter,
            speechAssistantTrace,
            new FormsCatalogSelectionTransitionPolicy(new SelectionConfirmationPolicyEvaluator()))
    {
    }

    public FormsCatalogDialogRuntimeRunner(
        SelectionDialogSpeechTurnRunner speech,
        IFormsCatalogIntentParser intentParser,
        FormsCatalogDialogIntentRouter intentRouter,
        ISpeechAssistantTrace speechAssistantTrace,
        FormsCatalogSelectionTransitionPolicy transitionPolicy)
    {
        _definitionFactory = new FormsCatalogRuntimeDefinitionFactory(
            speech ?? throw new ArgumentNullException(nameof(speech)),
            intentParser ?? throw new ArgumentNullException(nameof(intentParser)),
            intentRouter ?? throw new ArgumentNullException(nameof(intentRouter)),
            speechAssistantTrace ?? throw new ArgumentNullException(nameof(speechAssistantTrace)),
            transitionPolicy ?? throw new ArgumentNullException(nameof(transitionPolicy)));
    }

    public async Task<FormsCatalogDialogRuntimeResult> RunAsync(
        FormsCatalogDialogSession session,
        string correlationId,
        SpeechTranscript? initialCatalogTranscript,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        var state = new FormsCatalogDialogRuntimeState();
        var runtime = new SelectionDialogRuntime<ApplicationInfo, FormsCatalogDialogResult>(
            _definitionFactory.Create(session, correlationId, initialCatalogTranscript, state));

        var result = await runtime.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return new FormsCatalogDialogRuntimeResult(result, state.RestartIsoCode);
    }
}
