namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Übermittelt einen aktualisierten Transkriptstand mit optionaler Konfidenz und Äußerungszuordnung für laufende Rückmeldungen.
/// </summary>
public sealed record SpeechConversationTranscriptUpdateEvent(
    string Transcript,
    int? Confidence,
    int? TurnIndex) : SpeechConversationEvent;
