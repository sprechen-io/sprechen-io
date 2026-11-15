namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Verbindet Detektorkennung, Konfidenz und Audioposition einer akustischen Aktivierung.
/// </summary>
public sealed record AcousticWakeWordEvidence(
    string DetectorId,
    double ConfidenceScore,
    SampleCursor HandoffCursor);
