namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Interpretiert eine einzelne, ausdrücklich benannte Datumskomponente: Tag, Monat
/// oder vierstelliges Jahr. Die Zuordnung bestimmt, wie eine Zahl verstanden wird.
/// Monatsnamen werden in den fünf unterstützten Sprachen erkannt. Der Sprachcode
/// bestimmt die Erkennung gesprochener Ziffern. So benötigt der schrittweise
/// Datumsdialog nur die Komponenteninterpretation statt des gesamten Transkriptparsers.
/// </summary>
public interface IFormDateComponentParser
{
    FormDateComponentParseOutcome ParseDateComponent(
        string? requestedIsoCode,
        DateComponentSlot slot,
        string transcript);
}
