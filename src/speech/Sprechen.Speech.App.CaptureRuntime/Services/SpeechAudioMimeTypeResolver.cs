namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Ordnet die Formatangabe des Mikrofons der unterstützten PCM-Verarbeitung zu,
/// damit fehlende oder nicht unterstützte Angaben vor der Segmentierung auffallen.
/// </summary>
public sealed class SpeechAudioMimeTypeResolver
{
    public SpeechAudioMimeType Resolve(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new InvalidOperationException("Microphone mime type is missing.");
        }

        var normalized = mimeType.Trim().ToLowerInvariant();
        if (normalized.Contains("pcm", StringComparison.Ordinal))
        {
            return SpeechAudioMimeType.Pcm;
        }

        throw new InvalidOperationException($"Unsupported microphone mime type '{mimeType}'. Expected PCM (audio/pcm;rate=16000).");
    }
}
