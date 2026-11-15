namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis einer Spracherfassung mit Transkript und optionalem ASR-Konfidenzwert
/// in Prozent (0 bis 100).
/// </summary>
/// <remarks>
/// <c>FailureCode</c> kennzeichnet einen technischen Erfassungsfehler, etwa eine volle
/// Erkennungswarteschlange, ausgeschöpfte Wiederholungen oder eine unerwartete
/// Unterbrechung. Bei einem Fehler bleibt das Transkript null. Aufrufer müssen
/// deshalb zuerst den Fehlercode prüfen: Ein fehlendes Transkript allein belegt
/// nicht, dass die Nutzerin nichts gesagt hat.
/// <c>Recoverable</c> wird derzeit nur protokolliert. Die Fehlerbehandlung richtet sich
/// nach <c>FailureCode</c>.
/// </remarks>
public sealed record SpeechCaptureResult(
    string? Transcript,
    int? SttConfidencePercent,
    string? FailureCode = null,
    bool? Recoverable = null)
{
    /// <summary>
    /// Die erfassten Audiodaten wurden zur Erkennung gesendet, lieferten aber keinen
    /// Text. Das unterscheidet ein leeres Erkennungsergebnis von einem Timeout vor
    /// dem Sprachbeginn. Zuhörabläufe können dadurch erneut aufnehmen, ohne eine
    /// Ansage zu erzeugen, die selbst eine weitere Erfassung auslösen könnte.
    /// Die Vorgabe ist <c>false</c>.
    /// </summary>
    public bool RecognizedAsEmpty { get; init; }
}
