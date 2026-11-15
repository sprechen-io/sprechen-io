
namespace Sprechen.VoiceAssistant.VoiceCommands.Dialogs.SpeechAssistant;

/// <summary>
/// Beantwortet Status- und Zusammenfassungsbefehle aus dem aktuellen
/// Formularzustand und protokolliert Statusabfragen für die Evaluation.
/// </summary>
internal sealed class FormVoiceCommandStatusHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandStatusHandler(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public bool CanHandle(CommandId commandId)
        => commandId.Value is GlobalCommandIds.Status or GlobalCommandIds.Summary;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();

        if (string.Equals(context.CommandId, GlobalCommandIds.Status, StringComparison.Ordinal))
        {
            var statusSnapshot = context.StatusSnapshot
                ?? throw new InvalidOperationException("Status command requires an enriched status snapshot.");
            _evaluationTelemetry.TrackStatusSnapshot(context.Session, statusSnapshot.ToEvaluationStatusSnapshot());
            _evaluationTelemetry.TrackFieldAndGroupStatus(
                context.Session,
                (await context.Interpreter.GetDetailedStatusSnapshotAsync().ConfigureAwait(false)).ToEvaluationDetailedStatus());

            await _speechService.SpeakSafeAsync(
                    FormStatusPromptBuilder.BuildStatusPrompt(
                        context.Application,
                        context.CurrentForm,
                        statusSnapshot,
                        context.Contract,
                        context.IsoCode,
                        context.VoiceSettings),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);

            return null;
        }

        var summaryItems = await context.Interpreter.BuildSummaryItemsAsync().ConfigureAwait(false);
        await _speechService.SpeakSafeAsync(
                FormSummaryPromptBuilder.BuildSummaryPrompt(
                    context.Contract,
                    context.IsoCode,
                    politenessLevel,
                    summaryItems),
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);

        return null;
    }
}
