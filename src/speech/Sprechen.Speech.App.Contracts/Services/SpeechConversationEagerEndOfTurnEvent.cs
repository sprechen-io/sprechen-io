namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Übermittelt ein vorläufig erkanntes Äußerungsende samt Transkript zur frühzeitigen Verarbeitung im Gesprächsstrom.
/// </summary>
public sealed record SpeechConversationEagerEndOfTurnEvent(
    int TurnIndex,
    string Transcript) : SpeechConversationEvent;
