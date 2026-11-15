namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Bündelt modellierte Fehlerschwelle, Wartezeiten und Optionen für
/// Wiederholungen sowie die Ansage wiederhergestellter Mikrofonverfügbarkeit.
/// </summary>
public sealed record MicrophoneAvailabilityPolicySettings(
    int FailureThreshold,
    int CooldownSeconds,
    int MaxCooldownSeconds,
    bool AutomaticRetry,
    bool AnnounceRecovery);
