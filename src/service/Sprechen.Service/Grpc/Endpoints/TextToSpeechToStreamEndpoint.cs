using System.Diagnostics;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monstertacle.SharpQrs;
using Sprechen.Proto.Transport;
using Sprechen.Service.Application.Cqrs.SpeechSynthesis;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Validiert Syntheseanfragen und liefert Audioantworten.
/// Vom Aufrufer ausgelöste Abbrüche werden protokolliert und ohne weiteren Fehler beendet.
/// </summary>
internal sealed class TextToSpeechToStreamEndpoint(
    ICommandDispatcher commandDispatcher,
    SpeechRequestValidator requestValidator,
    ProviderExceptionToRpcMapper exceptionMapper,
    IOptions<SpeechProviderOptions> speechProviderOptions,
    TextToSpeechAudioStreamWriter audioStreamWriter,
    ILogger<SpeechGrpcService> logger)
{
    private readonly ICommandDispatcher _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
    private readonly SpeechRequestValidator _requestValidator = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
    private readonly ProviderExceptionToRpcMapper _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));
    private readonly IOptions<SpeechProviderOptions> _speechProviderOptions = speechProviderOptions ?? throw new ArgumentNullException(nameof(speechProviderOptions));
    private readonly TextToSpeechAudioStreamWriter _audioStreamWriter = audioStreamWriter ?? throw new ArgumentNullException(nameof(audioStreamWriter));
    private readonly ILogger<SpeechGrpcService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task HandleAsync(
        SprechenProto.TextRequest request,
        IServerStreamWriter<SprechenProto.AudioResponse> responseStream,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentNullException.ThrowIfNull(context);

        var ct = context.CancellationToken;
        var streamResult = new TextToSpeechAudioStreamResult();

        try
        {
            var validated = _requestValidator.ValidateTextToSpeechRequest(request);
            var provider = _speechProviderOptions.Value.TtsProvider;
            var traceId = Activity.Current?.TraceId.ToString();
            var pacePcm = _audioStreamWriter.ShouldPacePcm(request.DesiredMimeType);

            _logger.LogDebug(
                "TextToSpeechToStream request started (provider={Provider}, languageCode={LanguageCode}, desiredMimeType={DesiredMimeType}, textLength={TextLength}, speechRate={SpeechRate}, pacePcm={PacePcm}, traceId={TraceId}).",
                provider, validated.LanguageCode, request.DesiredMimeType, validated.Text.Length, validated.SpeechRate, pacePcm, traceId);

            var input = new SynthesizeSpeechInput(
                validated.Text,
                validated.LanguageCode,
                validated.DesiredMimeType,
                validated.SpeechRate,
                validated.PolitenessLevel);

            var chunks = await _commandDispatcher
                .DispatchAsync(new SynthesizeSpeechCommand(input), ct)
                .ConfigureAwait(false);

            streamResult = await _audioStreamWriter
                .WriteAsync(
                    chunks,
                    responseStream,
                    provider,
                    validated.LanguageCode,
                    request.DesiredMimeType,
                    validated.DesiredMimeType,
                    validated.Text,
                    validated.SpeechRate,
                    traceId,
                    ct)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "TextToSpeechToStream request completed (provider={Provider}, languageCode={LanguageCode}, desiredMimeType={DesiredMimeType}, speechRate={SpeechRate}, chunkCount={ChunkCount}, byteCount={ByteCount}, firstSampleRateHz={FirstSampleRateHz}, traceId={TraceId}).",
                provider, validated.LanguageCode, request.DesiredMimeType, validated.SpeechRate, streamResult.ChunkCount, streamResult.ByteCount, streamResult.FirstSampleRateHz, traceId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "TextToSpeechToStream request canceled after {ChunkCount} chunks / {ByteCount} bytes.",
                streamResult.ChunkCount, streamResult.ByteCount);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "TextToSpeechToStream request canceled by token after {ChunkCount} chunks / {ByteCount} bytes.",
                streamResult.ChunkCount, streamResult.ByteCount);
        }
        catch (SpeechProviderRateLimitedException ex)
        {
            throw _exceptionMapper.MapRateLimited(ex, "TextToSpeechToStream", "Text-to-speech", clientCorrelationId: null);
        }
        catch (RpcException ex) when (!SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out _))
        {
            throw _exceptionMapper.MapProviderFailure(ex, "TextToSpeechToStream", "Text-to-speech", clientCorrelationId: null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not RpcException)
        {
            throw _exceptionMapper.MapProviderFailure(ex, "TextToSpeechToStream", "Text-to-speech", clientCorrelationId: null);
        }
    }
}
