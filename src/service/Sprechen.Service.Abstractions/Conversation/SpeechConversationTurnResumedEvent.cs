namespace Sprechen.Service.Abstractions.Conversation;

/// <summary>
/// Ereignis für die Fortsetzung eines Gesprächszugs nach einer Pause.
/// </summary>
public sealed record SpeechConversationTurnResumedEvent(
    int TurnIndex) : SpeechConversationEvent;
