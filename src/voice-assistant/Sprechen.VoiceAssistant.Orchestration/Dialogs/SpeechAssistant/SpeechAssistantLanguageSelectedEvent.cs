namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet die aufgelöste Sprachauswahl an den Assistentenreducer, damit der Einstieg seine weiteren
/// Aufträge in dieser Sprache ausführt.
/// </summary>
public sealed record SpeechAssistantLanguageSelectedEvent(string IsoCode) : SpeechAssistantEvent
{
    public string NormalizedIsoCode { get; } = global::Sprechen.Speech.LanguageContracts.IsoCode.NormalizeIso6391(IsoCode);
}

