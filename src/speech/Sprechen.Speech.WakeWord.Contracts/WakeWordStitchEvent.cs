namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Hält die Verknüpfung aufeinanderfolgender Äußerungen anhand ihrer Textlängen fest, damit die Telemetrie keinen Wortlaut benötigt.
/// </summary>
public sealed record WakeWordStitchEvent(
    string CorrelationId,
    WakeWordStitchPhase Phase,
    int TranscriptTextLength,
    int StitchedTranscriptLength,
    int CommandTextLength);
