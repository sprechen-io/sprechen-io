using System.Globalization;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält gesammelte Datumsbestandteile und deren Erkennungssicherheit zusammen,
/// bestimmt die Erfassungsreihenfolge und prüft beim Zusammensetzen die Kalendergültigkeit.
/// Der Tag kann unabhängig von Monat und Jahr entfernt werden.
/// </summary>
internal sealed class DateComponentWorkflowBuffer
{
    private readonly AccumulatedValueConfidence _accumulatedValueConfidence = new();
    private int? _day;
    private int? _month;
    private int? _year;
    private int? _dayConfidence;
    private int? _monthConfidence;
    private int? _yearConfidence;

    public DateComponentWorkflowBuffer(string? isoCode)
    {
        // Bei einem englischen Sprachcode zuerst Monat, dann Tag und Jahr sammeln.
        // Bei anderen Codes Tag, Monat und Jahr. Jeder Bestandteil wird eigens benannt.
        Order = IsEnglish(isoCode)
            ? [DateComponentSlot.Month, DateComponentSlot.Day, DateComponentSlot.Year]
            : [DateComponentSlot.Day, DateComponentSlot.Month, DateComponentSlot.Year];
    }

    public IReadOnlyList<DateComponentSlot> Order { get; }

    public bool IsCollected(DateComponentSlot slot)
        => slot switch
        {
            DateComponentSlot.Day => _day is not null,
            DateComponentSlot.Month => _month is not null,
            DateComponentSlot.Year => _year is not null,
            _ => false
        };

    public void Record(DateComponentSlot slot, int value, int? confidencePercent)
    {
        switch (slot)
        {
            case DateComponentSlot.Day:
                _day = value;
                _dayConfidence = confidencePercent;
                break;
            case DateComponentSlot.Month:
                _month = value;
                _monthConfidence = confidencePercent;
                break;
            case DateComponentSlot.Year:
                _year = value;
                _yearConfidence = confidencePercent;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown date component slot.");
        }
    }

    /// <summary>
    /// Setzt vollständige und kalendarisch gültige Teile im unterstützten Jahresbereich
    /// zu einem ISO-Datum zusammen. Fehlende oder ungültige Teile ergeben keinen Datumswert.
    /// </summary>
    public bool TryAssembleIsoDate(out string isoDate)
    {
        isoDate = string.Empty;
        if (_day is not { } day || _month is not { } month || _year is not { } year)
        {
            return false;
        }

        if (year is < 1000 or > 9999 || month is < 1 or > 12 || day is < 1 or > 31)
        {
            return false;
        }

        if (day > DateTime.DaysInMonth(year, month))
        {
            return false;
        }

        isoDate = new DateOnly(year, month, day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

    /// <summary>
    /// Entfernt den Tag und dessen Erkennungssicherheit aus dem Puffer.
    /// Monat und Jahr bleiben erhalten. Das erneute Erfragen führt der Aufrufer aus.
    /// </summary>
    public void DiscardDay()
    {
        _day = null;
        _dayConfidence = null;
    }

    /// <summary>
    /// Liefert die kleinste angegebene Erkennungssicherheit der verbleibenden Teile
    /// oder null, wenn keiner dieser Teile eine Angabe trägt.
    /// </summary>
    public int? GetSurvivingMinimumConfidencePercent()
        => _accumulatedValueConfidence.SurvivingMinimumPercent(new[] { _dayConfidence, _monthConfidence, _yearConfidence });

    private static bool IsEnglish(string? isoCode)
        => isoCode is not null && isoCode.Trim().StartsWith("en", StringComparison.OrdinalIgnoreCase);
}
