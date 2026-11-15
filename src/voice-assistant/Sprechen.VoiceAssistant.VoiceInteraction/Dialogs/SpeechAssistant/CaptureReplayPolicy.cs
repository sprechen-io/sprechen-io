namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Entscheidet aus dem übergebenen Freigabekennzeichen und einem nichtleeren Ansagetext,
/// ob dieser Text erneut angefordert werden soll. Die Entscheidung trimmt den Text.
/// Aufnahmeursache, Feldbezug und einmalige Verwendung prüft der aufrufende Ablauf.
/// </summary>
internal sealed class CaptureReplayPolicy
{
    public CaptureReplayDecision Decide(bool autoReplayEligible, string? currentFieldTurnAnnouncement)
    {
        if (!autoReplayEligible)
        {
            return CaptureReplayDecision.None;
        }

        if (string.IsNullOrWhiteSpace(currentFieldTurnAnnouncement))
        {
            return CaptureReplayDecision.None;
        }

        return CaptureReplayDecision.Replay(currentFieldTurnAnnouncement.Trim());
    }
}
