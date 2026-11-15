using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Protokolliert Metadaten und Umfang von Syntheseanfragen und Audioantworten.
/// Der Synthesetext selbst wird nicht entgegengenommen.
/// </summary>
internal sealed class SpeechSynthesisDiagnosticsEmitter(
    IOptions<SpeechProviderOptions> speechProviderOptions,
    ILogger<SpeechGrpcService> logger)
{
    private readonly IOptions<SpeechProviderOptions> _speechProviderOptions = speechProviderOptions ?? throw new ArgumentNullException(nameof(speechProviderOptions));
    private readonly ILogger<SpeechGrpcService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public SpeechProvider TtsProvider => _speechProviderOptions.Value.TtsProvider;

    public void LogSpeakRequested(
        string languageCode,
        AudioMimeType desiredMimeType,
        int textLength,
        float speechRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentOutOfRangeException.ThrowIfNegative(textLength);

        _logger.LogDebug(
            "TextToSpeechToStream speak request (provider={Provider}, traceId={TraceId}, languageCode={LanguageCode}, desiredMimeType={DesiredMimeType}, speechRate={SpeechRate}, textLength={TextLength}).",
            TtsProvider,
            ResolveDiagnosticsTraceId(),
            languageCode,
            desiredMimeType,
            speechRate,
            textLength);
    }

    public void LogSpeakCompleted(
        string languageCode,
        AudioMimeType desiredMimeType,
        float speechRate,
        int chunkCount,
        long byteCount,
        int? firstSampleRateHz)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);

        _logger.LogDebug(
            "TextToSpeechToStream speak completed (provider={Provider}, traceId={TraceId}, languageCode={LanguageCode}, desiredMimeType={DesiredMimeType}, speechRate={SpeechRate}, chunkCount={ChunkCount}, byteCount={ByteCount}, firstSampleRateHz={FirstSampleRateHz}).",
            TtsProvider,
            ResolveDiagnosticsTraceId(),
            languageCode,
            desiredMimeType,
            speechRate,
            chunkCount,
            byteCount,
            firstSampleRateHz);
    }

    private static string? ResolveDiagnosticsTraceId()
        => Activity.Current?.TraceId.ToString();
}
