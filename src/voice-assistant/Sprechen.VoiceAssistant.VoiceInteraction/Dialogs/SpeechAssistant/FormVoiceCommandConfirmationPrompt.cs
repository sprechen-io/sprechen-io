namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt den Text einer Bestätigungsfrage, den vorgesehenen Vorlesewert
/// und dessen Freigabestatus. Zusätzliche Kennzeichen halten fest, ob der
/// Wert genannt und Buchstabieren angeboten wird, damit eine Rückfrage
/// dieselben Möglichkeiten verwendet.
/// </summary>
internal sealed record FormVoiceCommandConfirmationPrompt(
    string Prompt,
    string Value,
    bool IncludesValue,
    FormSpokenValueProjectionStatus Status,
    bool OffersSpell = false);
