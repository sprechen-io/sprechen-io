namespace Sprechen.Service.Abstractions.Conversation;

/// <summary>
/// Erkannte und vorgeschlagene Sprachen, ohne damit die Sitzungssprache festzulegen.
/// </summary>
public sealed record SpeechConversationLanguageDetectedEvent(
    IReadOnlyList<string> Languages,
    IReadOnlyList<string> HintedLanguages,
    int? TurnIndex) : SpeechConversationEvent
{
    public SpeechConversationLanguageDetectedEvent(IReadOnlyList<string> languages, int? turnIndex)
        : this(languages, Array.Empty<string>(), turnIndex)
    {
    }
}
