namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Unterscheidet Erkennungszwecke für freie Sprache, Buchstabieren und Zifferneingabe zur Auswahl des passenden Profils.
/// </summary>
public enum RecognitionProfile
{
    Conversational = 0,
    Spelling = 1,
    Digits = 2
}
