namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Hält Aktivität, Segmentfortschritt und gelesene Rückblickbytes zusammen,
/// damit ein Nachlesen zwischen beendetem Versuch und erschöpftem Puffer unterscheiden kann.
/// </summary>
internal readonly record struct ActiveCaptureBacklogReadResult(
    bool IsActive,
    PcmSpeechCaptureSegmenterUpdate Update,
    bool Advanced,
    int LookbackBytes)
{
    internal static ActiveCaptureBacklogReadResult Inactive { get; } = new(
        IsActive: false,
        Update: default,
        Advanced: false,
        LookbackBytes: 0);
}
