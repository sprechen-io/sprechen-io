namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Unterscheidet erfolgreiche und fehlgeschlagene Veröffentlichung des Ersatzpfads, damit eine angeforderte Umschaltung nicht als vollzogen gilt.
/// </summary>
public enum WakeWordFallbackPhase
{
    Published = 0,
    PublishFailed = 1
}
