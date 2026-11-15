namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Unterscheidet erfolgreiche Detektion und ihre Ausfallgründe, damit die Telemetrie den lexikalischen Ersatzpfad erklären kann.
/// </summary>
public enum WakeWordAcousticDetectionPhase
{
    Detected = 0,
    DetectorUnavailable = 1,
    DetectorWarmup = 2,
    DetectorFailure = 3
}
