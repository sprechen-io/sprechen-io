namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet fehlende von nicht zuordenbaren Bestätigungsantworten.
/// Beide bleiben unentschieden und erteilen keine Zustimmung zur Übernahme
/// des Feldwerts. Der aufrufende Ablauf entscheidet über Rückfrage und Korrektur.
/// </summary>
internal sealed class FormVoiceCommandConfirmationMissingDecisionPolicy
{
    public ConfirmationDecision Resolve(bool hasTranscript)
        => ConfirmationDecision.Undecided(
            hasTranscript ? ConfirmationUndecidedReason.Unrecognized : ConfirmationUndecidedReason.Silence);
}
