namespace Sprechen.Service.Abstractions;

/// <summary>
/// Unterstützte MIME-Formate für die Audioübertragung.
/// </summary>
public enum AudioMimeType
{
    Unspecified = 0,
    Mp3 = 1,
    Wav = 2,
    OggOpus = 3,
    Flac = 4,
    WebmOpus = 5,
    Pcm = 6
}
