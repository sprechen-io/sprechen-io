
namespace Sprechen.VoiceAssistant.VoiceCommands.Dialogs.SpeechAssistant;

/// <summary>
/// Bearbeitet Sprungbefehle zu Formularzielen und unterscheidet
/// Fokuswechsel, reine Vorleseziele und nicht gefundene Ziele.
/// </summary>
internal sealed class FormVoiceCommandJumpHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandJumpHandler(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public bool CanHandle(CommandId commandId) => commandId == GlobalCommandIds.JumpTo;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();

        if (string.IsNullOrWhiteSpace(context.Argument))
        {
            _evaluationTelemetry.TrackNavigationError(context.Session, NavigationErrorCodes.JumpMissingArgument, ConstraintScopes.CrossGroup);

            await _speechService.SpeakSafeAsync(
                    FormNavigationSwitchPromptBuilder.GetJumpToMissingArgumentPrompt(
                        context.Contract,
                        context.IsoCode,
                        politenessLevel),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        var jumpResult = await context.Interpreter.TryJumpToAsync(context.Argument).ConfigureAwait(false);
        switch (jumpResult.Kind)
        {
            case FormJumpToResultKind.Focused:
                break;

            case FormJumpToResultKind.SpokenReadOnlyTarget:
                // Das Ziel ist zum Vorlesen erreicht. Der Eingabefokus wurde nicht versetzt.
                // Ohne Positionsansage vorlesen und keinen Navigationsfehler melden.
                await SpeakReadOnlyTargetAsync(context, jumpResult, politenessLevel, cancellationToken).ConfigureAwait(false);
                break;

            case FormJumpToResultKind.NotFound:
            default:
                _evaluationTelemetry.TrackNavigationError(context.Session, NavigationErrorCodes.JumpTargetNotFound, ConstraintScopes.CrossGroup);

                await _speechService.SpeakSafeAsync(
                        FormNavigationSwitchPromptBuilder.GetJumpToNotFoundPrompt(context.Contract, context.IsoCode, politenessLevel),
                        context.IsoCode,
                        context.VoiceSettings,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;
        }

        return null;
    }

    private async Task SpeakReadOnlyTargetAsync(
        FormVoiceCommandGlobalCommandContext context,
        FormJumpToResult jumpResult,
        PolitenessLevel politenessLevel,
        CancellationToken cancellationToken)
    {
        // Einen vorhandenen Feldgruppentitel mit derselben Einleitung wie beim
        // Gruppeneintritt ankündigen, anschließend die gelieferten Inhalte vorlesen.
        if (!string.IsNullOrWhiteSpace(jumpResult.FieldGroupTitle))
        {
            await _speechService.SpeakSafeAsync(
                    FormFieldGroupIntroPromptBuilder.GetFieldGroupIntroPrompt(
                        context.Contract,
                        context.IsoCode,
                        politenessLevel,
                        jumpResult.FieldGroupTitle.Trim()),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var readback in jumpResult.Readbacks)
        {
            // Nichtverfügbarkeit gesondert ansagen. Vorhandenen Sprechtext mit Bezeichnung
            // vorlesen. Bei zurückgehaltenen oder ohne Sprechtext maskierten Werten nur
            // die Bezeichnung nennen. Verbleibende Fälle erhalten die Leerwertansage.
            var prompt = readback.Status switch
            {
                FormSpokenValueProjectionStatus.Unavailable => FormReadOnlyFieldPromptBuilder.GetUnavailablePrompt(
                    context.Contract,
                    context.IsoCode,
                    politenessLevel,
                    readback.Label),
                FormSpokenValueProjectionStatus.FullValue
                    or FormSpokenValueProjectionStatus.Empty
                    or FormSpokenValueProjectionStatus.Masked
                    when !string.IsNullOrWhiteSpace(readback.SpokenValue)
                    => FormReadOnlyFieldPromptBuilder.GetReadbackPrompt(
                        context.Contract,
                        context.IsoCode,
                        politenessLevel,
                        readback.Label,
                        readback.SpokenValue),
                FormSpokenValueProjectionStatus.Withheld
                    or FormSpokenValueProjectionStatus.Masked
                    => FormReadOnlyFieldPromptBuilder.GetAnnouncementLabelOnlyPrompt(
                        context.Contract,
                        context.IsoCode,
                        politenessLevel,
                        readback.Label),
                _ => FormReadOnlyFieldPromptBuilder.GetEmptyValuePrompt(
                    context.Contract,
                    context.IsoCode,
                    politenessLevel,
                    readback.Label),
            };

            await _speechService.SpeakSafeAsync(prompt, context.IsoCode, context.VoiceSettings, cancellationToken).ConfigureAwait(false);
        }
    }
}
