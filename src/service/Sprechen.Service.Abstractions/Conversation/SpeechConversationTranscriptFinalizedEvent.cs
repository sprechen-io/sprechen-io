namespace Sprechen.Service.Abstractions.Conversation;

/// <summary>
/// Abschließendes Transkript mit optionaler Erkennungssicherheit und Zugnummer.
/// </summary>
public sealed record SpeechConversationTranscriptFinalizedEvent(
    string Transcript,
    int? Confidence,
    int? TurnIndex) : SpeechConversationEvent;
