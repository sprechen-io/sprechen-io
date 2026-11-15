namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Meldet, dass während einer Erfassung innerhalb der vorgesehenen Zeit
/// keine neuen Mikrofon-Chunks eingetroffen sind. Auch Stille wird als Audio geliefert.
/// Die Zeitgrenze unterscheidet deshalb fehlende Daten von fehlender erkannter Sprache.
/// </summary>
public sealed class SpeechCaptureStreamStalledException : InvalidOperationException
{
    public SpeechCaptureStreamStalledException()
        : base("The microphone capture stream stopped delivering chunks before the capture completed.")
    {
    }
}
