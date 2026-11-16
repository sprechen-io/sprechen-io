namespace Sprechen.Speech.App.MicrophoneRecording.Services;

/// <summary>
/// Session id or browser error name from a scripted microphone acquire result.
/// Production capture does not use this shape.
/// </summary>
internal sealed class MicrophoneAcquisitionDto
{
    public string? SessionId { get; set; }

    public string? ErrorName { get; set; }
}
