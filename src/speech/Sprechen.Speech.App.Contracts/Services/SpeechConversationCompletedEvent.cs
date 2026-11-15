namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Meldet den Abschluss eines gestreamten Gesprächs zur Verarbeitung seines Endzustands.
/// </summary>
public sealed record SpeechConversationCompletedEvent : SpeechConversationEvent;
