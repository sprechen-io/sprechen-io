using Sprechen.Speech.App.Contracts.Evaluation;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Behandelt Abschluss, Übernahme und Vorlesen der Mengenauswahl und meldet dem
/// Treiber Abschluss oder Fortsetzung. Eine vorhandene Auswahl erhält beim Abschluss
/// das Übernahmemerkmal. Eine leere Auswahl darf ohne dieses Merkmal abschließen,
/// wenn keine positive Mindestanzahl vorliegt. Weitere Feldbedingungen prüft dieser Schritt nicht.
/// Die Ansage der aktuellen Auswahl erfolgt über den übergebenen Rückruf.
/// </summary>
internal sealed class SetWorkflowCommandActionFlow
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;

    public SetWorkflowCommandActionFlow(IEvaluationFormTelemetry evaluationTelemetry)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
    }

    public async Task<CharacterAccumulationTurnOutcome> HandleAsync(
        SetWorkflowCommand command,
        bool hasSelection,
        CharacterAccumulationContext context,
        Func<CharacterAccumulationContext, CancellationToken, Task> announceSelection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(announceSelection);

        switch (command)
        {
            // Beide Abschlussbefehle gleich behandeln. Bei vorhandener Auswahl das
            // Übernahmemerkmal setzen. Ein zugelassener leerer Abschluss erhält es nicht.
            case SetWorkflowCommand.AcceptSelection:
            case SetWorkflowCommand.Complete:
                if (hasSelection)
                {
                    return CharacterAccumulationTurnOutcome.FinalizeByExplicitAccept();
                }

                if (AllowsEmptySelection(context.Field))
                {
                    return CharacterAccumulationTurnOutcome.Finalize();
                }

                // Bei leerer Auswahl mit positiver Mindestanzahl die fehlende Angabe melden,
                // den Fehlversuch für die Hilfestufung registrieren und den Ansagerückruf ausführen.
                _evaluationTelemetry.TrackRepairAttempt(
                    context.Session,
                    context.Field.FieldId,
                    SetWorkflowRepairEventCodes.NoValue);
                await context.AutoHelp.RegisterFailureAsync(context.Field.FieldId, cancellationToken)
                    .ConfigureAwait(false);
                await announceSelection(context, cancellationToken).ConfigureAwait(false);
                return CharacterAccumulationTurnOutcome.AwaitNextTurn();

            case SetWorkflowCommand.ReadbackSelection:
                // Das erneute Vorlesen protokollieren und dieselbe Auswahlansage anfordern,
                // die auch im Fortsetzungsweg verwendet wird. Den Puffer dabei nicht verändern.
                _evaluationTelemetry.TrackRepairAttempt(
                    context.Session,
                    context.Field.FieldId,
                    SetWorkflowRepairEventCodes.ReadbackSelection);
                await announceSelection(context, cancellationToken).ConfigureAwait(false);
                return CharacterAccumulationTurnOutcome.AwaitNextTurn();

            default:
                return CharacterAccumulationTurnOutcome.AwaitNextTurn();
        }
    }

    // Die leere Auswahl bei fehlender oder nicht positiver Mindestanzahl zulassen.
    // Eine gesonderte Pflichtbedingung wird erst im anschließenden Validierungsweg geprüft.
    private static bool AllowsEmptySelection(FormFieldSnapshot field)
        => (field.MinSelected ?? 0) <= 0;
}
