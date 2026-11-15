namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Trägt die nach einem Sprachwechsel zurückgegebene verwendbare Sprache zum Reducer, damit dieser den
/// Formulardialog damit neu anfordern kann.
/// </summary>
public sealed record SpeechAssistantVoiceCommandLanguageChangedEvent : SpeechAssistantEvent
{
    public SpeechAssistantVoiceCommandLanguageChangedEvent(string normalizedIsoCode)
    {
        NormalizedIsoCode = IsoCode.TryNormalizeIso6391(normalizedIsoCode)
            ?? throw new ArgumentException("Expected ISO 639-1 language code.", nameof(normalizedIsoCode));
    }

    public string NormalizedIsoCode { get; }
}
