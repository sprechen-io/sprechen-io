namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verbindet Gültigkeit und Lesefortschritt eines Segmentplatzes mit dessen Ergebnis,
/// damit das Nachlesen bei Ablösung oder erschöpftem Puffer enden kann.
/// </summary>
internal readonly record struct ActiveSegmentBacklogReadResult(
    bool IsActive,
    PcmSpeechCaptureSegmenterUpdate Update,
    bool Advanced)
{
    internal static ActiveSegmentBacklogReadResult Inactive { get; } = new(
        IsActive: false,
        Update: default,
        Advanced: false);
}
