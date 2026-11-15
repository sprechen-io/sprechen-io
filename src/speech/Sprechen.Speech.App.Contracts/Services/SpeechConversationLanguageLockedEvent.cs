namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Meldet eine festgelegte Gesprächssprache mit optionalem Äußerungsbezug für nachfolgende Verarbeitung.
/// </summary>
public sealed record SpeechConversationLanguageLockedEvent(
    string LanguageCode,
    int? TurnIndex) : SpeechConversationEvent;
