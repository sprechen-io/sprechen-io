namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält das Ergebnis der Erfassung eines Datumsbestandteils fest: den Einzelwert
/// oder ein stattdessen vollständig erkanntes Datum samt Vertrauenswert.
/// </summary>
internal readonly record struct DateComponentSlotCollectionResult(
    DateComponentSlotOutcomeKind Kind,
    int Value,
    string? EscapedIso,
    int? SttConfidencePercent)
{
    public static DateComponentSlotCollectionResult Collected(int value, int? sttConfidencePercent)
        => new(DateComponentSlotOutcomeKind.Collected, value, null, sttConfidencePercent);

    public static DateComponentSlotCollectionResult Aborted()
        => new(DateComponentSlotOutcomeKind.Aborted, 0, null, null);

    public static DateComponentSlotCollectionResult Escaped(string isoDate, int? sttConfidencePercent)
        => new(DateComponentSlotOutcomeKind.Escaped, 0, isoDate, sttConfidencePercent);
}
