namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Übermittelt ein finalisiertes Transkript mit optionaler Konfidenz und Äußerungszuordnung zur Verarbeitung im Gesprächsstrom.
/// </summary>
public sealed record SpeechConversationTranscriptFinalizedEvent(
    string Transcript,
    int? Confidence,
    int? TurnIndex) : SpeechConversationEvent;
