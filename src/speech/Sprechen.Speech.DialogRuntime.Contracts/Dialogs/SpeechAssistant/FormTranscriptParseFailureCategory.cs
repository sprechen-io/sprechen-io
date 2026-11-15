namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Maschinenlesbarer Grund, warum ein Feldtranskript nicht zum kanonischen Wert
/// wurde, damit die Reparaturansage die Klasse nennen kann statt den Parsertext
/// vorzulesen.
/// </summary>
public enum FormTranscriptParseFailureCategory
{
    NotRecognized,
    AmbiguousYesNo,
    DecimalNotSupported,
    MultipleNumbers,
    NumberOutOfRange,
    UnknownSpellingToken,
    NoMatchingOption,
    AmbiguousChoice,
    InvalidCalendarDate,
    TimeOutOfRange,
    UnsupportedTemporalForm
}
