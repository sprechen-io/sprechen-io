namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet den Abbruch der Anwendungsauswahl, auch als Ergebnis eines fehlgeschlagenen Auswahlablaufs.
/// </summary>
public sealed record SpeechAssistantApplicationSelectionCancelledEvent(string? EffectiveIsoCode = null) : SpeechAssistantEvent
{
    public string? NormalizedEffectiveIsoCode { get; } = string.IsNullOrWhiteSpace(EffectiveIsoCode)
        ? null
        : global::Sprechen.Speech.LanguageContracts.IsoCode.NormalizeIso6391(EffectiveIsoCode);
}


