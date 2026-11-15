namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet das Ergebnis des Abschlussdialogs mit einem optionalen Endergebnis.
/// Ohne Endergebnis übernimmt der aufrufende Feldablauf wieder die Steuerung.
/// </summary>
internal sealed record FormClosingReviewTurnResult(FormVoiceCommandFieldHandlingResult? Terminal)
{
    public static FormClosingReviewTurnResult ReturnToFieldLoop()
        => new(Terminal: null);

    public static FormClosingReviewTurnResult AsTerminal(FormVoiceCommandFieldHandlingResult terminal)
    {
        ArgumentNullException.ThrowIfNull(terminal);
        return new FormClosingReviewTurnResult(terminal);
    }
}
