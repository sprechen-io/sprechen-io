namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Hält Transkript, abgetrennten Befehl und Aktivierungsstatus zusammen, damit Folgeentscheidungen dieselbe lexikalische Auswertung verwenden.
/// </summary>
public sealed record WakeWordActivationEvaluation(
    WakeWordActivationStatus Status,
    string TranscriptText,
    string CommandText,
    int? ConfidencePercent)
{
    public bool HasRecognizedSpeech => !string.IsNullOrWhiteSpace(TranscriptText);

    public bool IsActivated => Status == WakeWordActivationStatus.LeadingWakeWordDetected;
}
