
namespace Sprechen.VoiceAssistant.VoiceCommands.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert Ansagen für Formularlisten und Formularstatus an und erfasst
/// bei Statusabfragen die zugehörige Evaluationstelemetrie.
/// </summary>
internal sealed class FormVoiceCommandFormStatusHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandFormStatusHandler(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public bool CanHandle(CommandId commandId)
        => commandId.Value is GlobalCommandIds.FormList or GlobalCommandIds.FormStatus;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.Equals(context.CommandId, GlobalCommandIds.FormList, StringComparison.Ordinal))
        {
            await _speechService.SpeakSafeAsync(
                    FormStatusPromptBuilder.BuildFormListPrompt(context.Application, context.Contract, context.IsoCode, context.VoiceSettings),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        var statusSnapshot = context.StatusSnapshot
            ?? throw new InvalidOperationException("Form-status command requires an enriched status snapshot.");
        _evaluationTelemetry.TrackStatusSnapshot(context.Session, statusSnapshot.ToEvaluationStatusSnapshot());
        _evaluationTelemetry.TrackFieldAndGroupStatus(
            context.Session,
            (await context.Interpreter.GetDetailedStatusSnapshotAsync().ConfigureAwait(false)).ToEvaluationDetailedStatus());

        await _speechService.SpeakSafeAsync(
                FormStatusPromptBuilder.BuildFormStatusPrompt(
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
}
