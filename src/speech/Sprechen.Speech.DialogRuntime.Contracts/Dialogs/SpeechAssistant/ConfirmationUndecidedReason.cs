namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet eine fehlende von einer unpassenden Antwort auf eine
/// Bestätigungsfrage, damit die Rückfrage den passenden Grund nennt.
/// </summary>
public enum ConfirmationUndecidedReason
{
    /// <summary>Die Aufnahme lieferte kein Transkript: Stille oder ein Aufnahmefehler ohne Sprache.</summary>
    Silence,

    /// <summary>Ein Transkript kam an, war aber weder Antwortwort noch verankertes Kommando.</summary>
    Unrecognized
}
