using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Protokolliert globale Befehle und leitet sie mit dem Sitzungskontext
/// an die zuständige Verarbeitung weiter. ExecuteAsync beginnt bei
/// fehlendem Endergebnis die Feldaufforderung erneut.
/// </summary>
internal sealed class FormVoiceCommandGlobalCommandExecutionService
{
    private readonly IEvaluationCommandTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandGlobalCommandRouter _commandRouter;

    public FormVoiceCommandGlobalCommandExecutionService(
        IEvaluationCommandTelemetry evaluationTelemetry,
        IFormVoiceCommandGlobalCommandRouter commandRouter)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _commandRouter = commandRouter ?? throw new ArgumentNullException(nameof(commandRouter));
    }

    public async Task<FormVoiceCommandFieldHandlingResult> ExecuteAsync(
        FormVoiceCommandContext context,
        IFormDialogRuntime interpreter,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        string commandId,
        string? argument,
        CancellationToken cancellationToken)
    {
        var commandResult = await RouteAsync(
                context,
                interpreter,
                session,
                stateTracker,
                commandId,
                argument,
                cancellationToken)
            .ConfigureAwait(false);

        if (commandResult is null)
        {
            stateTracker.BeginFieldPromptRestart();
            return FormVoiceCommandFieldHandlingResult.Continue();
        }

        return FormVoiceCommandFieldHandlingResult.FromDialogResult(commandResult);
    }

    // Eine erkannte, hier nicht weitergeleitete Command-Id protokollieren, ohne
    // den Router zu rufen. Am Abschluss hält das einen geparsten Navigationsbefehl
    // von Stille und von einem unerkannten Satz auseinander.
    public void TrackRecognizedWithoutRouting(EvaluationSession session, string commandId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        _evaluationTelemetry.TrackGlobalCommand(session, CommandId.From(commandId.Trim()));
    }

    // Den globalen Befehl protokollieren und weiterleiten, ohne selbst den
    // Feldzug neu zu starten. Der aufgerufene Handler kann Zustand ändern.
    // Sein leeres Ergebnis bedeutet, dass kein Endergebnis vorliegt.
    public async Task<VoiceCommandDialogResult?> RouteAsync(
        FormVoiceCommandContext context,
        IFormDialogRuntime interpreter,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        string commandId,
        string? argument,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interpreter);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(stateTracker);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        var normalizedCommandId = CommandId.From(commandId.Trim());
        _evaluationTelemetry.TrackGlobalCommand(session, normalizedCommandId);
        var statusSnapshot = normalizedCommandId.Value is GlobalCommandIds.Status or GlobalCommandIds.FormStatus
            ? await FormVoiceCommandStatusSnapshotFactory.CreateAsync(context, interpreter).ConfigureAwait(false)
            : null;

        var commandContext = new FormVoiceCommandGlobalCommandContext(
            new FormVoiceCommandGlobalCommandRequest(
                normalizedCommandId,
                context.IsoCode,
                argument),
            new FormVoiceCommandSelectionContext(
                context.VoiceSettings,
                context.Application,
                context.CompletedFormIds,
                context.Form,
                context.Contract),
            new FormVoiceCommandGlobalCommandRuntime(
                interpreter,
                session,
                stateTracker))
        {
            StatusSnapshot = statusSnapshot
        };

        return await _commandRouter.TryHandleAsync(
                normalizedCommandId,
                commandContext,
                cancellationToken)
            .ConfigureAwait(false);
    }

}
