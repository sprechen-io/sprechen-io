namespace Sprechen.Service.Abstractions.Conversation;

/// <summary>
/// Vorläufig erkanntes Ende eines Gesprächszugs mit Transkript.
/// </summary>
public sealed record SpeechConversationEagerEndOfTurnEvent(
    int TurnIndex,
    string Transcript) : SpeechConversationEvent;
