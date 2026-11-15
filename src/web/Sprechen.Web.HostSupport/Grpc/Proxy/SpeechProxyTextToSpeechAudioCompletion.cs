using Sprechen.Proto;
using Microsoft.Extensions.Logging;
using Sprechen.Web.HostSupport.Grpc.Adapter;

namespace Sprechen.Web.HostSupport.Grpc.Proxy;

/// <summary>
/// Protokolliert abgeschlossene Audio-Weiterleitungen und meldet fehlende Audiodaten als Anbieterfehler.
/// </summary>
internal sealed class SpeechProxyTextToSpeechAudioCompletion
{
    private readonly ILogger _logger;

    public SpeechProxyTextToSpeechAudioCompletion(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogOrThrow(
        string operation,
        TextRequest request,
        (int ChunkCount, long ByteCount, int? FirstSampleRateHz) summary,
        string traceId,
        LogLevel level)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        if (summary.ChunkCount == 0 || summary.ByteCount == 0)
        {
            _logger.LogError(
                "Proxy {Operation} completed without audio (languageCode={LanguageCode}, desiredMimeType={DesiredMimeType}, traceId={TraceId}).",
                operation,
                request.LanguageCode,
                request.DesiredMimeType,
                traceId);
            throw SpeechProxyProviderFailure.Create("Text-to-speech proxy returned no audio.");
        }

        // Der Aufrufer übergibt die Logstufe passend zur Startmeldung.
        // Chunk- und Bytezahlen dokumentieren die erfassten Audiodaten.
        _logger.Log(
            level,
            "Proxy {Operation} completed (languageCode={LanguageCode}, desiredMimeType={DesiredMimeType}, chunkCount={ChunkCount}, byteCount={ByteCount}, firstSampleRateHz={FirstSampleRateHz}, traceId={TraceId}).",
            operation,
            request.LanguageCode,
            request.DesiredMimeType,
            summary.ChunkCount,
            summary.ByteCount,
            summary.FirstSampleRateHz,
            traceId);
    }
}
