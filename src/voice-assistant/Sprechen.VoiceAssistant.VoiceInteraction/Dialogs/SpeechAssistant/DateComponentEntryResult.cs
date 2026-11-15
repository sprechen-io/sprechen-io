namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet den Ausgang des Datumsdialogs mit dem zusammengesetzten Datum
/// und dem übergebenen Vertrauenswert. Die Erzeugungsmethode verlangt
/// nichtleeren Text, prüft aber nicht dessen Datumsformat.
/// </summary>
internal readonly record struct DateComponentEntryResult(
    bool Aborted,
    string? CapturedIsoDate,
    int? AggregatedConfidencePercent)
{
    public bool HasCapturedValue => CapturedIsoDate is not null;

    public static DateComponentEntryResult Captured(string isoDate, int? aggregatedConfidencePercent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoDate);
        return new DateComponentEntryResult(Aborted: false, CapturedIsoDate: isoDate, aggregatedConfidencePercent);
    }

    public static DateComponentEntryResult AbortedToFieldEntry()
        => new(Aborted: true, CapturedIsoDate: null, AggregatedConfidencePercent: null);
}
