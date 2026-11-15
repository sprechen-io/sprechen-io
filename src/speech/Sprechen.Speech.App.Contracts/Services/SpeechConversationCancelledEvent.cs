namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Meldet den Abbruch eines gestreamten Gesprächs zur Verarbeitung seines Endzustands.
/// </summary>
public sealed record SpeechConversationCancelledEvent : SpeechConversationEvent;
