namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet eine zusätzliche Rückfrage zum Vorlesen und Korrigieren
/// von der weiteren Behandlung ohne diese Rückfrage.
/// </summary>
internal enum SpellingEscalationDecision
{
    /// <summary>
    /// Den Wert zum Vorlesen und Korrigieren anbieten. Eine unmittelbare
    /// Übernahme gehört nicht zu diesem Angebot.
    /// </summary>
    Escalate,

    /// <summary>
    /// Den erfassten Wert ohne zusätzliche Korrekturrückfrage weiterbehandeln.
    /// </summary>
    Defer,
}
