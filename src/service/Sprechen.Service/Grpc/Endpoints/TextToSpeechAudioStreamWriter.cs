using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprechen.Service.Options;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Überträgt Syntheseaudio in den gRPC-Antwortstrom, optional mit Versand im PCM-Zeittakt.
/// Ein regulär beendeter Anbieterstrom ohne Audiodaten wird als Fehler gemeldet.
/// </summary>
internal sealed class TextToSpeechAudioStreamWriter(
    IOptions<AudioPacingOptions> audioPacingOptions,
    SpeechSynthesisDiagnosticsEmitter diagnosticsEmitter,
    ILogger<SpeechGrpcService> logger)
{
    private readonly IOptions<AudioPacingOptions> _audioPacingOptions = audioPacingOptions ?? throw new ArgumentNullException(nameof(audioPacingOptions));
    private readonly SpeechSynthesisDiagnosticsEmitter _diagnosticsEmitter = diagnosticsEmitter ?? throw new ArgumentNullException(nameof(diagnosticsEmitter));
    private readonly ILogger<SpeechGrpcService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public bool ShouldPacePcm(SprechenProto.AudioMimeType desiredMimeType)
        => _audioPacingOptions.Value.Enabled
           && desiredMimeType == SprechenProto.AudioMimeType.AudioPcm;

    public async Task<TextToSpeechAudioStreamResult> WriteAsync(
        IAsyncEnumerable<AudioSynthesisChunk> audioChunks,
        IServerStreamWriter<SprechenProto.AudioResponse> responseStream,
        SpeechProvider provider,
        string languageCode,
        SprechenProto.AudioMimeType desiredMimeType,
        AudioMimeType coreDesiredMimeType,
        string text,
        float speechRate,
        string? traceId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(audioChunks);
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentNullException.ThrowIfNull(text);

        _diagnosticsEmitter.LogSpeakRequested(
            languageCode,
            coreDesiredMimeType,
            text.Length,
            speechRate);

        var pacePcm = ShouldPacePcm(desiredMimeType);
        var audioWriter = new GrpcAudioResponseStreamWriter(pacePcm);
        var chunkCount = 0;
        long byteCount = 0;
        int? firstSampleRateHz = null;

        await foreach (var chunk in audioChunks.WithCancellation(ct).ConfigureAwait(false))
        {
            if (chunk.Data.Length == 0)
            {
                _logger.LogDebug(
                    "TextToSpeechToStream provider yielded an empty audio chunk (provider={Provider}, languageCode={LanguageCode}, desiredMimeType={DesiredMimeType}, traceId={TraceId}).",
                    provider,
                    languageCode,
                    desiredMimeType,
                    traceId);
                continue;
            }

            if (chunkCount == 0)
            {
                firstSampleRateHz = chunk.SampleRateHz > 0 ? chunk.SampleRateHz : null;
                _logger.LogDebug(
                    "TextToSpeechToStream first audio chunk received (provider={Provider}, mimeType={MimeType}, sampleRateHz={SampleRateHz}, bytes={ChunkBytes}, traceId={TraceId}).",
                    provider,
                    chunk.Mime,
                    chunk.SampleRateHz,
                    chunk.Data.Length,
                    traceId);
            }

            chunkCount++;
            byteCount += chunk.Data.Length;
            await audioWriter.WriteAsync(responseStream, chunk, ct).ConfigureAwait(false);
        }

        if (chunkCount == 0 || byteCount == 0)
        {
            _logger.LogError(
                "TextToSpeechToStream request produced no audio (provider={Provider}, languageCode={LanguageCode}, desiredMimeType={DesiredMimeType}, textLength={TextLength}, traceId={TraceId}).",
                provider,
                languageCode,
                desiredMimeType,
                text.Length,
                traceId);

            throw SprechenRpcExceptionFactory.Internal(
                SprechenProto.SprechenErrorCode.ProviderFailure,
                "Text-to-speech provider returned no audio.");
        }

        _diagnosticsEmitter.LogSpeakCompleted(
            languageCode,
            coreDesiredMimeType,
            speechRate,
            chunkCount,
            byteCount,
            firstSampleRateHz);

        return new TextToSpeechAudioStreamResult(chunkCount, byteCount, firstSampleRateHz);
    }
}
