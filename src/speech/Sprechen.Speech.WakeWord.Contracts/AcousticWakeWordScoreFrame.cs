namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Ordnet einen Detektorwert seiner Audioposition und Beobachtungszeit zu, damit Schwelle und Aufwärmphase gemeinsam geprüft werden können.
/// </summary>
public sealed record AcousticWakeWordScoreFrame(
    string DetectorId,
    double ConfidenceScore,
    SampleCursor Cursor,
    DateTimeOffset ObservedAtUtc);
