namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Meldet den Beginn einer Äußerung anhand ihres Index zur Zuordnung nachfolgender Gesprächsereignisse.
/// </summary>
public sealed record SpeechConversationTurnStartedEvent(
    int TurnIndex) : SpeechConversationEvent;
