namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Deaktiviert die Rückfrage, ob eine Äußerung Inhalt oder Befehl ist.
/// Nach der Parameterprüfung liefert jeder Aufruf <c>false</c>.
/// </summary>
internal sealed class FormVoiceCommandAmbiguitySignalEligibility
{
    public bool IsEligible(
        FormVoiceCommandContext context,
        FormVoiceCommandTranscriptInterpretation interpretation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interpretation);

        // Für diesen Entscheidungspunkt keine Mehrdeutigkeitsrückfrage freigeben.
        // Die vorgelagerte Befehlsentscheidung und ihre Modusausnahmen bleiben eigene Regeln.
        return false;
    }
}
