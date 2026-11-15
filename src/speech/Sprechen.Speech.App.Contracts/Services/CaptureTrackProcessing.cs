namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Vom Browser über <c>MediaStreamTrack.getSettings()</c> gemeldete Audioverarbeitungsoptionen.
/// Ein nicht lesbarer oder nicht verfügbarer Wert bleibt null.
/// </summary>
public sealed record CaptureTrackProcessing(
    bool? EchoCancellation,
    bool? NoiseSuppression,
    bool? AutoGainControl)
{
    public static CaptureTrackProcessing Unknown { get; } = new(null, null, null);
}
