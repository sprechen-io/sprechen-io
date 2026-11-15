namespace Sprechen.Service.Abstractions.Conversation;

/// <summary>
/// Fehlerereignis des Gesprächsstroms mit Code und optionaler Beschreibung.
/// </summary>
public sealed record SpeechConversationFailedEvent(
    string ErrorCode,
    string? Detail) : SpeechConversationEvent;
