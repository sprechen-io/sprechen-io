using Grpc.Core;
using Sprechen.Proto.Transport;
using Sprechen.Proto;
using Microsoft.Extensions.Logging;
using Sprechen.Web.HostSupport.Grpc.Adapter;

namespace Sprechen.Web.HostSupport.Grpc.Proxy;

/// <summary>
/// Koordiniert eine Audio-Weiterleitung und protokolliert Beginn, Abschluss oder behandelten Abbruch.
/// </summary>
internal sealed class SpeechProxyTextToSpeechStreamRunner
{
    private readonly SpeechProxyTextToSpeechAudioCompletion _audioCompletion;
    private readonly SpeechProxyTextToSpeechForwardingAttempt _forwardingAttempt = new();
    private readonly ILogger _logger;
    private readonly SpeechProxyTraceIdResolver _traceIdResolver = new();

    public SpeechProxyTextToSpeechStreamRunner(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _audioCompletion = new SpeechProxyTextToSpeechAudioCompletion(_logger);
    }

    public async Task ForwardAsync(
        string operation,
        TextRequest request,
        IServerStreamWriter<AudioResponse> responseStream,
        ServerCallContext context,
        Func<CancellationToken, AsyncServerStreamingCall<AudioResponse>> startCall)
    {
        var traceId = _traceIdResolver.Resolve();
        var announcementKind = SprechenGrpcAnnouncementMetadata.ReadAnnouncementKind(context.RequestHeaders);
        var sessionId = SprechenGrpcSessionMetadata.ReadSessionId(context.RequestHeaders);

        // Hinweise auf fehlende Eingabe und behebbare Fehler bleiben auf Information sichtbar. Gewöhnliche Prompts verwenden Debug.
        var level = announcementKind is SprechenGrpcAnnouncementMetadata.NoInputKind
                or SprechenGrpcAnnouncementMetadata.RecoverableFailureKind
            ? LogLevel.Information
            : LogLevel.Debug;

        _logger.Log(
            level,
            "Proxy {Operation} started (languageCode={LanguageCode}, desiredMimeType={DesiredMimeType}, textLength={TextLength}, announcementKind={AnnouncementKind}, sessionId={SessionId}, traceId={TraceId}).",
            operation,
            request.LanguageCode,
            request.DesiredMimeType,
            request.Text?.Length ?? 0,
            announcementKind,
            sessionId,
            traceId);

        var outcome = await _forwardingAttempt.TryForwardAsync(startCall, responseStream, context.CancellationToken)
            .ConfigureAwait(false);

        if (outcome.Aborted)
        {
            // Abbrüche gesondert protokollieren: Eine leere Audiozählung bei Abbruch soll keinen zusätzlichen Anbieterfehler auslösen.
            _logger.Log(
                level,
                "Proxy {Operation} aborted (languageCode={LanguageCode}, announcementKind={AnnouncementKind}, chunkCount={ChunkCount}, byteCount={ByteCount}, sessionId={SessionId}, traceId={TraceId}).",
                operation,
                request.LanguageCode,
                announcementKind,
                outcome.ChunkCount,
                outcome.ByteCount,
                sessionId,
                traceId);
            return;
        }

        // Die Abschlussmeldung erhält dieselbe Logstufe wie der Beginn, damit beide zusammen sichtbar bleiben.
        _audioCompletion.LogOrThrow(
            operation,
            request,
            (outcome.ChunkCount, outcome.ByteCount, outcome.FirstSampleRateHz),
            traceId,
            level);
    }
}
