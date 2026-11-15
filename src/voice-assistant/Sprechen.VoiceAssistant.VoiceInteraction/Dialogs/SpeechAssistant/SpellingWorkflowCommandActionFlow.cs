namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Wendet Abschluss- und Bearbeitungsbefehle auf den Buchstabierpuffer an
/// und fordert gegebenenfalls seine Wiedergabe an. Nur ein nicht leerer Puffer
/// kann abgeschlossen werden. Weitere Erfassung und Feldübernahme folgen beim Aufrufer.
/// </summary>
internal sealed class SpellingWorkflowCommandActionFlow
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly SpellingWorkflowNoValuePrompt _noValuePrompt;

    public SpellingWorkflowCommandActionFlow(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService,
        SpellingWorkflowNoValuePrompt noValuePrompt)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _noValuePrompt = noValuePrompt ?? throw new ArgumentNullException(nameof(noValuePrompt));
    }

    public async Task<CharacterAccumulationTurnOutcome> HandleAsync(
        SpellingWorkflowCommand command,
        SpellingWorkflowBuffer buffer,
        SpellingBlockReadback blockReadback,
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        EvaluationSession session,
        CancellationToken cancellationToken)
    {
        switch (command)
        {
            case SpellingWorkflowCommand.Complete:
                if (buffer.HasCharacters)
                {
                    return CharacterAccumulationTurnOutcome.Finalize();
                }

                await SpeakNoValueAsync(context, field, session, cancellationToken).ConfigureAwait(false);
                break;

            case SpellingWorkflowCommand.RemoveLastCharacter:
                await HandleEditAsync(
                        buffer.TryRemoveLastCharacter(),
                        SpellingWorkflowRepairEventCodes.DeleteLastCharacter,
                        FormSpellingWorkflowPromptBuilder.GetSpellingLastCharacterRemovedPrompt(
                            context.Contract,
                            context.IsoCode,
                            context.VoiceSettings.GetPolitenessLevel()),
                        context,
                        field,
                        session,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;

            case SpellingWorkflowCommand.RemoveLastBlock:
                await HandleEditAsync(
                        buffer.TryRemoveLastBlock(),
                        SpellingWorkflowRepairEventCodes.DeleteLastBlock,
                        FormSpellingWorkflowPromptBuilder.GetSpellingLastBlockRemovedPrompt(
                            context.Contract,
                            context.IsoCode,
                            context.VoiceSettings.GetPolitenessLevel()),
                        context,
                        field,
                        session,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;

            case SpellingWorkflowCommand.ClearBuffer:
                await HandleEditAsync(
                        buffer.TryClear(),
                        SpellingWorkflowRepairEventCodes.ClearBuffer,
                        FormSpellingWorkflowPromptBuilder.GetSpellingBufferClearedPrompt(
                            context.Contract,
                            context.IsoCode,
                            context.VoiceSettings.GetPolitenessLevel()),
                        context,
                        field,
                        session,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;

            case SpellingWorkflowCommand.ReadbackCharacters:
                _evaluationTelemetry.TrackRepairAttempt(
                    session,
                    field.FieldId,
                    SpellingWorkflowRepairEventCodes.ReadbackCharacters);
                await ReadBackWholeValueAsync(buffer, blockReadback, context, field, session, cancellationToken)
                    .ConfigureAwait(false);
                break;
        }

        return CharacterAccumulationTurnOutcome.AwaitNextTurn();
    }

    private async Task ReadBackWholeValueAsync(
        SpellingWorkflowBuffer buffer,
        SpellingBlockReadback blockReadback,
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        EvaluationSession session,
        CancellationToken cancellationToken)
    {
        if (!buffer.HasCharacters)
        {
            await SpeakNoValueAsync(context, field, session, cancellationToken).ConfigureAwait(false);
            return;
        }

        await blockReadback
            .ReadBackWholeValueAsync(context, field, buffer.GetValue(), buffer.GetSpokenCharacters(), cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task HandleEditAsync(
        bool operationSucceeded,
        string telemetryEvent,
        string successPrompt,
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        EvaluationSession session,
        CancellationToken cancellationToken)
    {
        if (!operationSucceeded)
        {
            await SpeakNoValueAsync(context, field, session, cancellationToken).ConfigureAwait(false);
            return;
        }

        _evaluationTelemetry.TrackRepairAttempt(session, field.FieldId, telemetryEvent);
        await _speechService
            .SpeakSafeAsync(successPrompt, context.IsoCode, context.VoiceSettings, cancellationToken)
            .ConfigureAwait(false);
    }

    private Task SpeakNoValueAsync(
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        EvaluationSession session,
        CancellationToken cancellationToken)
    {
        _evaluationTelemetry.TrackRepairAttempt(
            session,
            field.FieldId,
            SpellingWorkflowRepairEventCodes.NoValue);
        return _noValuePrompt.SpeakAsync(context, cancellationToken);
    }
}
