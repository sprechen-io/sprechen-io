namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Benennt den einzeln abgefragten Datumsbestandteil im konstruktiven Eingabedialog,
/// damit Ablauf, Ansage und Parser denselben Teilwert aus Tag, Monat oder Jahr bearbeiten.
/// </summary>
public enum DateComponentSlot
{
    Day,
    Month,
    Year
}
