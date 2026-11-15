namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Übermittelt erkannte Sprachen und Sprachhinweise mit optionalem Äußerungsbezug zur Auswertung des Gesprächsstroms.
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
