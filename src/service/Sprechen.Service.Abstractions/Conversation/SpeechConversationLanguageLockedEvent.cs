namespace Sprechen.Service.Abstractions.Conversation;

/// <summary>
/// Ereignis zur festgelegten Sitzungssprache mit optionaler Zugnummer.
/// </summary>
public sealed record SpeechConversationLanguageLockedEvent(
    string LanguageCode,
    int? TurnIndex) : SpeechConversationEvent;
