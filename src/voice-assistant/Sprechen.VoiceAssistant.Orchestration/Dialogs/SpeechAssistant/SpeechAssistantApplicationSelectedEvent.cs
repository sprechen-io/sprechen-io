namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet die im Katalogdialog ausgewählte Anwendung an den Reducer.
/// </summary>
public sealed record SpeechAssistantApplicationSelectedEvent(
    string ApplicationId,
    string? ApplicationName,
    int FormCount,
    string? DefaultFormId,
    string? EffectiveIsoCode) : SpeechAssistantEvent
{
    /// <summary>
    /// Prüft, ob die gemeldete Anwendung mehr als ein Formular enthält und der Reducer deshalb die
    /// Formularauswahl anfordert.
    /// </summary>
    public bool HasMultipleForms => FormCount > 1;

    public string? NormalizedEffectiveIsoCode { get; } = string.IsNullOrWhiteSpace(EffectiveIsoCode)
        ? null
        : global::Sprechen.Speech.LanguageContracts.IsoCode.NormalizeIso6391(EffectiveIsoCode);
}



