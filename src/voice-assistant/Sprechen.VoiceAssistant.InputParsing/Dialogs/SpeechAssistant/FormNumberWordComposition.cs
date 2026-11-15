namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Kennzeichnet das Ergebnis einer Zahlwortzusammensetzung, damit Aufrufer
/// Wert, Nichterkennung, mehrere Zahlen und Bereichsüberschreitung unterscheiden können.
/// </summary>
internal enum FormNumberWordComposition
{
    None,
    Value,
    MultipleNumbers,
    OutOfRange,
}
