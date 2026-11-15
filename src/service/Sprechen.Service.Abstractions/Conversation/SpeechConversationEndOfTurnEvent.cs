namespace Sprechen.Service.Abstractions.Conversation;

/// <summary>
/// Bestätigtes Ende eines Gesprächszugs mit Transkript.
/// </summary>
public sealed record SpeechConversationEndOfTurnEvent(
    int TurnIndex,
    string Transcript) : SpeechConversationEvent;
