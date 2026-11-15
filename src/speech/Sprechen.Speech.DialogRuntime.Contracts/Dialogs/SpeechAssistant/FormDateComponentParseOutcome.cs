namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis für einen ausdrücklich abgefragten Datumsbestandteil.
/// Der Kontext legt fest, ob etwa <c>08</c> Tag oder Monat meint.
/// Eine Jahresangabe muss vollständig vierstellig sein.
/// </summary>
public readonly record struct FormDateComponentParseOutcome(DateComponentParseStatus Status, int Value)
{
    public bool Success => Status == DateComponentParseStatus.Parsed;

    public static FormDateComponentParseOutcome Parsed(int value)
        => new(DateComponentParseStatus.Parsed, value);

    public static FormDateComponentParseOutcome NotRecognized()
        => new(DateComponentParseStatus.NotRecognized, 0);

    public static FormDateComponentParseOutcome OutOfRange()
        => new(DateComponentParseStatus.OutOfRange, 0);

    public static FormDateComponentParseOutcome YearNotFourDigits()
        => new(DateComponentParseStatus.YearNotFourDigits, 0);

    public static FormDateComponentParseOutcome YearUncertain()
        => new(DateComponentParseStatus.YearUncertain, 0);
}
