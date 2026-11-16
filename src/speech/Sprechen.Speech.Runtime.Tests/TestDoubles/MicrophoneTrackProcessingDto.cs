namespace Sprechen.Speech.App.MicrophoneRecording.Services;

/// <summary>
/// Scripted browser track-processing flags. A missing value stays distinct from false.
/// Production capture reads the library track report.
/// </summary>
internal sealed class MicrophoneTrackProcessingDto
{
    public bool? EchoCancellation { get; set; }

    public bool? NoiseSuppression { get; set; }

    public bool? AutoGainControl { get; set; }
}
