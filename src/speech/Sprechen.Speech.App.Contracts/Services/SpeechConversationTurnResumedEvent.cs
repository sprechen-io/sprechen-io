namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Meldet die Fortsetzung einer Äußerung anhand ihres Index zur Korrektur eines vorläufigen Endes.
/// </summary>
public sealed record SpeechConversationTurnResumedEvent(
    int TurnIndex) : SpeechConversationEvent;
