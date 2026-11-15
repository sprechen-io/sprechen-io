namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet ein erkanntes Transkript mit dessen Vertrauenswert oder einem bereits abgeschlossenen Bearbeitungsergebnis für die nächste Ablaufentscheidung.
/// </summary>
internal sealed record FormVoiceCommandFieldTranscriptCaptureResult(
    FormVoiceCommandFieldOrchestrationResult? FinalResult,
    string? Transcript,
    int? SttConfidencePercent)
{
    public bool HasFinalResult => FinalResult is not null;

    public string RequiredTranscript
        => Transcript ?? throw new InvalidOperationException("Captured transcript is not available.");

    public static FormVoiceCommandFieldTranscriptCaptureResult Completed(FormVoiceCommandFieldHandlingResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new FormVoiceCommandFieldTranscriptCaptureResult(
            FormVoiceCommandFieldOrchestrationResult.Completed(result),
            Transcript: null,
            SttConfidencePercent: null);
    }

    public static FormVoiceCommandFieldTranscriptCaptureResult Captured(
        string transcript,
        int? sttConfidencePercent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        return new FormVoiceCommandFieldTranscriptCaptureResult(
            FinalResult: null,
            Transcript: transcript,
            sttConfidencePercent);
    }
}
