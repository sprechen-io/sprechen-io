namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Unterscheidet Eingabesteuerelemente für die feldgerechte Darstellung einer stillen Formulareingabe.
/// </summary>
public enum SpeechAssistantSilentInputControlKind
{
    Text = 0,
    Email = 1,
    Telephone = 2,
    MultilineText = 3,
    CodeText = 4,
}
