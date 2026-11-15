namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Übermittelt das erkannte Äußerungsende mit Index und Transkript zur Zuordnung im Gesprächsstrom.
/// </summary>
public sealed record SpeechConversationEndOfTurnEvent(
    int TurnIndex,
    string Transcript) : SpeechConversationEvent;
