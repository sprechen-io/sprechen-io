namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Unterscheidet unterstützte Audioformatkennungen für die Übergabe von Sprachaudio zwischen Komponenten.
/// </summary>
public enum SpeechAudioMimeType
{
    Unspecified = 0,
    Mp3 = 1,
    Wav = 2,
    OggOpus = 3,
    Flac = 4,
    WebmOpus = 5,
    Pcm = 6
}
