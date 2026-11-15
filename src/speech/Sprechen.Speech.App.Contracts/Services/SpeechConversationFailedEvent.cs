namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Übermittelt einen Gesprächsfehler mit Code und optionalem Detail zur Fehlerbehandlung des Ereignisstroms.
/// </summary>
public sealed record SpeechConversationFailedEvent(
    string ErrorCode,
    string? Detail) : SpeechConversationEvent;
