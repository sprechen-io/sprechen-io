namespace Sprechen.Service.Options;

/// <summary>
/// Konfiguriert den Versand von PCM entsprechend seiner Audiodauer.
/// </summary>
internal sealed class AudioPacingOptions
{
    public const string SectionName = "AudioPacing";

    public bool Enabled { get; set; }
}

