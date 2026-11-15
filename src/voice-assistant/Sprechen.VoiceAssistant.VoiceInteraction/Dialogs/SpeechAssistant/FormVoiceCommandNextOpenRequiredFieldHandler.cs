namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert den Fokus auf das nächste offene Pflichtfeld über die gemeinsame
/// Formularnavigation an und baut die Ansage aus deren Ergebnis und dem
/// anschließend gelesenen Feld auf.
/// </summary>
internal sealed class FormVoiceCommandNextOpenRequiredFieldHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandNextOpenRequiredFieldHandler(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public bool CanHandle(CommandId commandId) => commandId == GlobalCommandIds.NextOpenRequiredField;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var result = await context.Interpreter.TryFocusNextOpenRequiredFieldAsync().ConfigureAwait(false);

        string? fieldLabel = null;
        if (result.Kind == FocusNextOpenRequiredFieldResultKind.NoOpenRequiredField)
        {
            _evaluationTelemetry.TrackNavigationAidNoTarget(context.Session, GlobalCommandIds.NextOpenRequiredField);
        }
        else
        {
            var focusedField = await context.Interpreter.GetFocusedFieldAsync().ConfigureAwait(false);
            fieldLabel = focusedField.Label;
        }

        var prompt = FormNextOpenRequiredFieldPromptBuilder.BuildPrompt(
            context.Contract,
            result,
            fieldLabel,
            context.IsoCode,
            context.VoiceSettings.GetPolitenessLevel());

        await _speechService.SpeakSafeAsync(prompt, context.IsoCode, context.VoiceSettings, cancellationToken)
            .ConfigureAwait(false);

        return null;
    }
}
