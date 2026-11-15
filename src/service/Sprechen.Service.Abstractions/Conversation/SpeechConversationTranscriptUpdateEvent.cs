namespace Sprechen.Service.Abstractions.Conversation;

/// <summary>
/// Zwischenstand des Transkripts mit optionaler Erkennungssicherheit und Zugnummer.
/// </summary>
public sealed record SpeechConversationTranscriptUpdateEvent(
    string Transcript,
    int? Confidence,
    int? TurnIndex) : SpeechConversationEvent;
