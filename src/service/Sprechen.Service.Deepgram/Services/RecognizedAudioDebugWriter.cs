using Microsoft.Extensions.Logging;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Speichert übergebene Audiodaten bei konfiguriertem Zielverzeichnis als Diagnosedatei.
/// Der Aufrufer muss die Diagnosefreigabe vor dem Aufruf prüfen.
/// </summary>
internal sealed class RecognizedAudioDebugWriter
{
    private readonly ILogger _logger;
    private readonly string? _dumpDirectory;

    internal RecognizedAudioDebugWriter(ILogger logger, string? dumpDirectory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dumpDirectory = string.IsNullOrWhiteSpace(dumpDirectory) ? null : dumpDirectory.Trim();
    }

    internal void TryWrite(byte[] payload, AudioMimeType mime, string? clientCorrelationId)
    {
        if (_dumpDirectory is null)
        {
            _logger.LogWarning(
                "The session marked this turn as diagnostic, but no recognized-audio dump directory is configured; the payload is not recorded.");
            return;
        }

        if (payload.Length == 0)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(_dumpDirectory);
            var fileName = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0:yyyyMMdd-HHmmss-fff}-{1:N}.{2}",
                DateTime.UtcNow,
                Guid.NewGuid(),
                ResolveExtension(mime));
            var path = Path.Combine(_dumpDirectory, fileName);
            File.WriteAllBytes(path, payload);
            // Geschriebene Aufnahmen auf Information protokollieren, damit sie in den üblichen Logs auffallen.
            _logger.LogInformation(
                "Recognized audio payload written for a diagnostic session (path={Path}, bytes={Bytes}, clientCorrelationId={ClientCorrelationId}).",
                path,
                payload.Length,
                clientCorrelationId);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                ex,
                "Could not write recognized audio debug payload to {Directory}.",
                _dumpDirectory);
        }
    }

    private static string ResolveExtension(AudioMimeType mime) => mime switch
    {
        // PCM wird vor dem Senden als WAV verpackt, deshalb ist der Dump abspielbares WAV.
        AudioMimeType.Pcm or AudioMimeType.Wav => "wav",
        AudioMimeType.Mp3 => "mp3",
        AudioMimeType.Flac => "flac",
        AudioMimeType.OggOpus => "ogg",
        AudioMimeType.WebmOpus => "webm",
        _ => "bin"
    };

}
