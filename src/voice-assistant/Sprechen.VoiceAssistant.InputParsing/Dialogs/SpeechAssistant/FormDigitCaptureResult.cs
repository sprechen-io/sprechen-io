namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet das Ergebnis der Ziffernerfassung mit dem Hinweis, ob ein einzelnes
/// Transkriptwort mehrere Ziffern beigetragen hat. Die Jahreskomponente verwendet
/// diesen Hinweis als Unsicherheitssignal.
/// <see cref="Outcome"/> bleibt das Ergebnis für den allgemeinen Ziffernparser.
/// </summary>
internal readonly record struct FormDigitCaptureResult(
    FormTranscriptParseOutcome Outcome,
    bool ContainsMultiDigitToken)
{
    public bool Success => Outcome.Success;

    public string? Value => Outcome.CanonicalValue;

    public static FormDigitCaptureResult NotRecognized()
        => new(FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized), false);

    public static FormDigitCaptureResult Captured(string value, bool containsMultiDigitToken)
        => new(FormTranscriptParseOutcome.Succeeded(value), containsMultiDigitToken);
}
