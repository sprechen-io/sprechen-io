namespace Sprechen.Speech.App.MicrophoneRecording.Services;

/// <summary>
/// Überträgt die tatsächlichen Verarbeitungseinstellungen der Browserspur,
/// wobei fehlende Angaben von ausdrücklich deaktivierten Funktionen unterscheidbar bleiben.
/// </summary>
internal sealed class MicrophoneTrackProcessingDto
{
    public bool? EchoCancellation { get; set; }

    public bool? NoiseSuppression { get; set; }

    public bool? AutoGainControl { get; set; }
}
