namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet einen fehlgeschlagenen Parseversuch anhand des erkannten Befehlspräfixes ein, damit die Rückmeldung den passenden Fehlerfall erhält.
/// </summary>
internal sealed class FormVoiceCommandParsedCommandFailureDecisionResolver
{
    public bool TryResolve(
        FormVoiceCommandContext context,
        FormVoiceCommandTranscriptInterpretation interpretation,
        out ParsedCommandBranchDecision decision)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interpretation);

        decision = ParsedCommandBranchDecision.None();

        if (!interpretation.HasExplicitCommandPrefix || interpretation.HasParsedCommand)
        {
            return false;
        }

        // Hier nur den Fehlerfall einordnen. Auswahl und Zählung der Reparaturrückmeldung
        // bleiben beim aufrufenden Interpretationszweig.
        decision = ParsedCommandBranchDecision.PromptedFailure(FormVoiceCommandFieldErrorCodes.UnknownCommand);
        return true;
    }
}
