namespace Sprechen.Service.Abstractions.Conversation;

/// <summary>
/// Ereignis für den Beginn eines Gesprächszugs.
/// </summary>
public sealed record SpeechConversationTurnStartedEvent(
    int TurnIndex) : SpeechConversationEvent;
