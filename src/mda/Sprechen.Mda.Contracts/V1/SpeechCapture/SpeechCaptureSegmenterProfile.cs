using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.SpeechCapture;

/// <summary>
/// Einstellungen der Sprachaktivitätserkennung zur Aufteilung der PCM-Aufnahme in Segmente.
/// </summary>
public sealed class SpeechCaptureSegmenterProfile
{
    [JsonPropertyName("speechAmplitudeThreshold")]
    public int SpeechAmplitudeThreshold { get; set; }

    [JsonPropertyName("speechStartAdaptiveMargin")]
    public int SpeechStartAdaptiveMargin { get; set; }

    [JsonPropertyName("activePlaybackSpeechStartAdaptiveMargin")]
    public int ActivePlaybackSpeechStartAdaptiveMargin { get; set; }

    [JsonPropertyName("idleAdaptiveFloorMaxMargin")]
    public int IdleAdaptiveFloorMaxMargin { get; set; }

    [JsonPropertyName("playbackLeakageBypassAmplitudeThreshold")]
    public int PlaybackLeakageBypassAmplitudeThreshold { get; set; }

    [JsonPropertyName("activePlaybackCalibrationChunkCount")]
    public int ActivePlaybackCalibrationChunkCount { get; set; }

    [JsonPropertyName("recentPlaybackCalibrationChunkCount")]
    public int RecentPlaybackCalibrationChunkCount { get; set; }

    [JsonPropertyName("maxPreRollChunks")]
    public int MaxPreRollChunks { get; set; }

    [JsonPropertyName("tailSilenceMs")]
    public int TailSilenceMs { get; set; }

    [JsonPropertyName("optionListingInitialSilenceTimeoutMs")]
    public int OptionListingInitialSilenceTimeoutMs { get; set; }
}
