using System.Collections.Immutable;


namespace Sprechen.VoiceAssistant.VoiceCommands.Dialogs.SpeechAssistant;

/// <summary>
/// Bearbeitet Vorwärts- und Rückwärtsbefehle im Formular. Ist kein nächstes Feld
/// erreichbar, wird die Abgabe versucht. Ein nicht möglicher Rückwärtsschritt
/// wird angesagt.
/// </summary>
internal sealed class FormVoiceCommandNavigationHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSubmissionService _submissionService;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandNavigationHandler(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSubmissionService submissionService,
        IFormVoiceCommandSpeechService speechService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _submissionService = submissionService ?? throw new ArgumentNullException(nameof(submissionService));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public bool CanHandle(CommandId commandId)
        => commandId.Value is GlobalCommandIds.Next or GlobalCommandIds.Back;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.Equals(context.CommandId, GlobalCommandIds.Back, StringComparison.Ordinal))
        {
            if (!await context.Interpreter.TryMoveFocusBackAsync().ConfigureAwait(false))
            {
                _evaluationTelemetry.TrackNavigationError(context.Session, NavigationErrorCodes.CannotMoveBack, ConstraintScopes.CrossGroup);

                // Einmal je Befehl, bevor der äußere Ablauf das aktuelle Feld erneut ansagt: ohne
                // diesen Satz folgt auf den Befehl nur die Wiederholung, und die ist von einem
                // überhörten Befehl nicht zu unterscheiden.
                await _speechService.SpeakSafeAsync(
                        FormBackNavigationPromptBuilder.GetBackUnavailablePrompt(
                            context.Contract,
                            context.IsoCode,
                            context.VoiceSettings.GetPolitenessLevel()),
                        context.IsoCode,
                        context.VoiceSettings,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return null;
        }

        if (await context.Interpreter.TryMoveFocusNextAsync().ConfigureAwait(false))
        {
            return null;
        }

        if (await _submissionService.TrySubmitAsync(
                context.Interpreter,
                context.Contract,
                context.Session,
                context.IsoCode,
                context.VoiceSettings,
                context.StateTracker,
                cancellationToken)
            .ConfigureAwait(false) == FormSubmissionResult.Submitted)
        {
            context.StateTracker.RecordSubmitted();
            return VoiceCommandDialogResult.Submitted(
                context.Contract.ApplicationId,
                context.Contract.FormId,
                (await context.Interpreter.GetValuesSnapshotAsync().ConfigureAwait(false))
                    .ToImmutableDictionary(StringComparer.Ordinal));
        }

        return null;
    }
}
