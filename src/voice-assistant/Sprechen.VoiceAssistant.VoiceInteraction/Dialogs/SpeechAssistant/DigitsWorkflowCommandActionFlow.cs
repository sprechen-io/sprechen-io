namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Führt lokale Ziffernbefehle am Puffer aus und fordert deren Rückmeldung an. Abschluss und nächster Aufnahmezug bleiben durch das Ergebnis unterscheidbar.
/// </summary>
internal sealed class DigitsWorkflowCommandActionFlow
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly DigitsWorkflowNoValuePrompt _noValuePrompt;

    public DigitsWorkflowCommandActionFlow(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService,
        DigitsWorkflowNoValuePrompt noValuePrompt)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _noValuePrompt = noValuePrompt ?? throw new ArgumentNullException(nameof(noValuePrompt));
    }

    /// <summary>
    /// Wendet den lokalen Befehl auf den Ziffernpuffer an und meldet bei Abschlussbefehl
    /// mit nichtleerem Puffer den Abschluss. Weitere Aufnahmen veranlasst der gemeinsame Treiber.
    /// </summary>
    public async Task<CharacterAccumulationTurnOutcome> HandleAsync(
        DigitsWorkflowCommand command,
        DigitsWorkflowBuffer digitBuffer,
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        EvaluationSession session,
        CancellationToken cancellationToken)
    {
        if (command == DigitsWorkflowCommand.Continue)
        {
            if (digitBuffer.HasDigits)
            {
                return CharacterAccumulationTurnOutcome.Finalize();
            }

            _evaluationTelemetry.TrackRepairAttempt(session, field.FieldId, DigitsWorkflowRepairEventCodes.NoValue);
            await _noValuePrompt.SpeakAsync(context, cancellationToken).ConfigureAwait(false);
            return CharacterAccumulationTurnOutcome.AwaitNextTurn();
        }

        await HandleEditCommandAsync(command, digitBuffer, context, field, session, cancellationToken)
            .ConfigureAwait(false);
        return CharacterAccumulationTurnOutcome.AwaitNextTurn();
    }

    private Task HandleEditCommandAsync(
        DigitsWorkflowCommand command,
        DigitsWorkflowBuffer digitBuffer,
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        EvaluationSession session,
        CancellationToken cancellationToken)
    {
        return command switch
        {
            DigitsWorkflowCommand.RemoveLastDigit => HandleEditOperationAsync(
                digitBuffer.TryRemoveLastDigit(),
                DigitsWorkflowRepairEventCodes.DeleteLastDigit,
                () => FormDigitsPromptBuilder.GetDigitsLastDigitRemovedPrompt(
                    context.Contract,
                    context.IsoCode,
                    digitBuffer.GetSpokenDigits(),
                    context.VoiceSettings.GetPolitenessLevel()),
                context,
                field,
                session,
                cancellationToken),
            DigitsWorkflowCommand.RemoveLastBlock => HandleEditOperationAsync(
                digitBuffer.TryRemoveLastBlock(),
                DigitsWorkflowRepairEventCodes.DeleteLastBlock,
                () => FormDigitsPromptBuilder.GetDigitsLastBlockRemovedPrompt(
                    context.Contract,
                    context.IsoCode,
                    digitBuffer.GetSpokenDigits(),
                    context.VoiceSettings.GetPolitenessLevel()),
                context,
                field,
                session,
                cancellationToken),
            DigitsWorkflowCommand.ReadbackDigits => HandleReadbackAsync(
                digitBuffer,
                context,
                field,
                session,
                cancellationToken),
            // Erneutes Diktieren entfernt zuerst den letzten Block. Die danach angeforderte
            // Ansage benennt diese Löschung. Ihre Anforderung belegt noch keine gehörte Ausgabe.
            DigitsWorkflowCommand.RedictateLastBlock => HandleEditOperationAsync(
                digitBuffer.TryPrepareRepeatLastBlock(),
                DigitsWorkflowRepairEventCodes.RedictateBlock,
                () => FormDigitsPromptBuilder.GetDigitsRedictateBlockPrompt(
                    context.Contract,
                    context.IsoCode,
                    context.VoiceSettings.GetPolitenessLevel()),
                context,
                field,
                session,
                cancellationToken),
            _ => Task.CompletedTask
        };
    }

    private async Task HandleReadbackAsync(
        DigitsWorkflowBuffer digitBuffer,
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        EvaluationSession session,
        CancellationToken cancellationToken)
    {
        _evaluationTelemetry.TrackRepairAttempt(session, field.FieldId, DigitsWorkflowRepairEventCodes.ReadbackDigits);
        await _speechService
            .SpeakSafeAsync(
                FormDigitsPromptBuilder.GetDigitsCurrentReadbackPrompt(
                    context.Contract,
                    context.IsoCode,
                    digitBuffer.GetSpokenDigits(),
                    context.VoiceSettings.GetPolitenessLevel()),
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task HandleEditOperationAsync(
        bool operationSucceeded,
        string telemetryEvent,
        Func<string> successPromptFactory,
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        EvaluationSession session,
        CancellationToken cancellationToken)
    {
        if (!operationSucceeded)
        {
            _evaluationTelemetry.TrackRepairAttempt(session, field.FieldId, DigitsWorkflowRepairEventCodes.NoValue);
            await _noValuePrompt.SpeakAsync(context, cancellationToken).ConfigureAwait(false);
            return;
        }

        _evaluationTelemetry.TrackRepairAttempt(session, field.FieldId, telemetryEvent);
        await _speechService
            .SpeakSafeAsync(successPromptFactory(), context.IsoCode, context.VoiceSettings, cancellationToken)
            .ConfigureAwait(false);
    }
}
