using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Steuert Zielprüfung, Bestätigung und Speicherung eines Sprachwechsels im Anwendungskatalog.
/// Ein bestätigter Wechsel fordert einen Neustart mit der gespeicherten Sprache an. Ablehnung und
/// Dialogabbruch bleiben getrennte Ergebnisse.
/// </summary>
internal sealed class FormsCatalogDialogLanguageChangeHandler
{
    private readonly FormsCatalogLanguageChangeWorkflow _languageChangeWorkflow;
    private readonly FormsCatalogLanguageChangeConfirmationRunner _confirmationRunner;
    private readonly FormsCatalogLanguageChangePersistence _persistence;

    public FormsCatalogDialogLanguageChangeHandler(
        SelectionDialogSpeechTurnRunner speech,
        IFormsCatalogIntentParser intentParser,
        FormsCatalogLanguageChangeWorkflow languageChangeWorkflow,
        ISpeechAssistantTrace speechAssistantTrace)
    {
        _languageChangeWorkflow = languageChangeWorkflow ?? throw new ArgumentNullException(nameof(languageChangeWorkflow));
        _confirmationRunner = new FormsCatalogLanguageChangeConfirmationRunner(
            speech ?? throw new ArgumentNullException(nameof(speech)),
            intentParser ?? throw new ArgumentNullException(nameof(intentParser)),
            languageChangeWorkflow,
            speechAssistantTrace ?? throw new ArgumentNullException(nameof(speechAssistantTrace)));
        _persistence = new FormsCatalogLanguageChangePersistence(
            languageChangeWorkflow,
            speechAssistantTrace ?? throw new ArgumentNullException(nameof(speechAssistantTrace)));
    }

    public async Task<FormsCatalogIntentHandlingResult> HandleAsync(
        FormsCatalogDialogTurnContext turnContext,
        string? rawLanguageTarget,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        var supportedLanguages = _languageChangeWorkflow.FormatSupportedLanguages(
            turnContext.PromptContext.SupportedLanguages,
            turnContext.IsoCode);

        if (string.IsNullOrWhiteSpace(rawLanguageTarget))
        {
            return CreateListeningResult(
                turnContext,
                FormDialogFlowPromptTextResolver.GetMissingArgumentPrompt(turnContext.IsoCode, supportedLanguages, turnContext.PolitenessLevel),
                expectsLanguageTarget: true);
        }

        if (!_languageChangeWorkflow.TryResolveTarget(
                rawLanguageTarget,
                turnContext.PromptContext.SupportedLanguages,
                turnContext.IsoCode,
                out var targetIsoCode,
                out var targetDisplayName))
        {
            return CreateListeningResult(
                turnContext,
                FormDialogFlowPromptTextResolver.GetUnsupportedPrompt(turnContext.IsoCode, supportedLanguages),
                expectsLanguageTarget: true);
        }

        var currentIsoCode = IsoCode.TryNormalizeIso6391(turnContext.IsoCode) ?? turnContext.IsoCode.Trim().ToLowerInvariant();
        if (string.Equals(targetIsoCode, currentIsoCode, StringComparison.Ordinal))
        {
            return CreateListeningResult(
                turnContext,
                FormDialogFlowPromptTextResolver.GetAlreadySelectedPrompt(turnContext.IsoCode, targetDisplayName),
                expectsLanguageTarget: true);
        }

        var confirm = await _confirmationRunner.ConfirmAsync(
                turnContext,
                targetIsoCode,
                cancellationToken)
            .ConfigureAwait(false);
        if (confirm == FormsCatalogLanguageChangeDecision.Cancelled)
        {
            return new FormsCatalogIntentHandlingResult(
                SelectionDialogPhase.Completed,
                SelectionDialogCandidateTransition<ApplicationInfo>.ClearTerminal(),
                FormsCatalogPromptBuilder.GetCancelPrompt(
                    turnContext.PromptContext,
                    turnContext.IsoCode,
                    turnContext.PolitenessLevel),
                SelectionDialogCompletionReason.UserCancelled);
        }

        if (confirm == FormsCatalogLanguageChangeDecision.CaptureFaultBudgetExhausted)
        {
            return new FormsCatalogIntentHandlingResult(
                SelectionDialogPhase.Completed,
                SelectionDialogCandidateTransition<ApplicationInfo>.ClearTerminal(),
                FormsCatalogPromptBuilder.GetCaptureFailedTerminalPrompt(
                    turnContext.PromptContext,
                    turnContext.IsoCode,
                    turnContext.PolitenessLevel),
                SelectionDialogCompletionReason.CaptureFaultBudgetExhausted);
        }

        if (confirm != FormsCatalogLanguageChangeDecision.Confirmed)
        {
            // Diese Rückgabe bietet keine eigene Sprachangabe je Ansageabschnitt. Eine
            // zielsprachige Ergänzung würde mit der Sitzungssprache wiedergegeben. Getrennte
            // Sprachsegmente benötigen einen entsprechend erweiterten Ansagevertrag.
            return new FormsCatalogIntentHandlingResult(
                SelectionDialogPhase.Listening,
                SelectionDialogCandidateTransition<ApplicationInfo>.Preserve(),
                SelectionDialogPrompt.FromText(_languageChangeWorkflow.GetLanguageKeptPrompt(turnContext.IsoCode)));
        }

        var persistedIsoCode = await _persistence.PersistAsync(
                turnContext.CorrelationId,
                targetIsoCode,
                cancellationToken)
            .ConfigureAwait(false);

        return FormsCatalogIntentHandlingResult.SilentRestartTerminal(persistedIsoCode);
    }

    private static FormsCatalogIntentHandlingResult CreateListeningResult(
        FormsCatalogDialogTurnContext turnContext,
        string promptText,
        bool expectsLanguageTarget)
        => new(
            SelectionDialogPhase.Listening,
            SelectionDialogCandidateTransition<ApplicationInfo>.Preserve(),
            SelectionDialogPrompt.FromText(promptText),
            ExpectsLanguageTarget: expectsLanguageTarget);
}
