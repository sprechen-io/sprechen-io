namespace Sprechen.Service.Abstractions;

/// <summary>
/// Konfiguration der Anbieter für Spracherkennung und Sprachsynthese.
/// </summary>
public sealed class SpeechProviderOptions
{
    public const string SectionName = "Language";

    public SpeechProvider SttProvider { get; set; } = SpeechProvider.Deepgram;

    public SpeechProvider TtsProvider { get; set; } = SpeechProvider.Deepgram;
}
