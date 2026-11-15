namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Entscheidet anhand übergebener Modus- und Parsergebnisse über die Befehlsausführung, damit die Interpretation dieselben Vorrangregeln verwendet.
/// </summary>
internal sealed class FormVoiceCommandParsedCommandExecutionDecisionResolver
{
    public ParsedCommandBranchDecision Resolve(FormVoiceCommandTranscriptInterpretation interpretation)
    {
        ArgumentNullException.ThrowIfNull(interpretation);

        // Bei manueller Korrektur genügt auf Textfeldern ein Parsergebnis. Andere Felder
        // benötigen zusätzlich die übergebene Ausführungsfreigabe.
        if (IsManualCorrectionCommand(interpretation)
            && (interpretation.IsTextField || interpretation.ShouldExecuteParsedCommand))
        {
            return ParsedCommandBranchDecision.ManualCorrection();
        }

        if (interpretation.ShouldExecuteParsedCommandInDigitsMode
            || !interpretation.IsDigitsMode && interpretation.ShouldExecuteParsedCommand)
        {
            return ParsedCommandBranchDecision.Execution();
        }

        return ParsedCommandBranchDecision.None();
    }

    private static bool IsManualCorrectionCommand(FormVoiceCommandTranscriptInterpretation interpretation)
    {
        return interpretation.HasParsedCommand
            && string.Equals(interpretation.ParsedCommandId, GlobalCommandIds.EditValue, StringComparison.Ordinal);
    }
}
