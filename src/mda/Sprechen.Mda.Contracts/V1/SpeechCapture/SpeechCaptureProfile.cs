using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.SpeechCapture;

/// <summary>
/// Zeitparameter für einen Sprachaufnahmemodus aus SpeechCaptureMode.
/// </summary>
public sealed class SpeechCaptureProfile
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;

    [JsonPropertyName("silenceDebounceMs")]
    public int SilenceDebounceMs { get; set; }

    [JsonPropertyName("maxUtteranceDurationMs")]
    public int? MaxUtteranceDurationMs { get; set; }

    [JsonPropertyName("maxTotalSegmentDurationMs")]
    public int? MaxTotalSegmentDurationMs { get; set; }

    [JsonPropertyName("speechStartConfirmationMs")]
    public int? SpeechStartConfirmationMs { get; set; }
}
