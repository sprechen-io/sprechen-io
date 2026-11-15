using Sprechen.Proto;
using Microsoft.Extensions.Logging;
using Sprechen.Web.HostSupport.Grpc.Adapter;

namespace Sprechen.Web.HostSupport.Grpc.Proxy;

/// <summary>
/// Protokolliert Erkennungsabschlüsse mit Status, Textlänge, Audiozählern und Korrelationskennungen.
/// Die Aufrufsteuerung entscheidet, welcher Abschluss geschrieben wird.
/// </summary>
internal sealed class SpeechProxySpeechToTextCompletionLogger
{
    // Eine gemeinsame Vorlage erleichtert den Abgleich aller Abschlussarten mit den Startmeldungen.
    private const string TerminalTemplate =
        "Proxy SpeechToText terminal (terminal={Terminal}, transcriptLength={TranscriptLength}, "
        + "chunks={Chunks}, bytes={Bytes}, "
        + "clientCorrelationId={ClientCorrelationId}, sessionId={SessionId}, traceId={TraceId}).";

    private readonly ILogger _logger;

    public SpeechProxySpeechToTextCompletionLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogCompleted(
        SpeechTranscript response,
        SpeechProxyForwardedAudio forwardedAudio,
        SpeechProxyCallContext callContext)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(forwardedAudio);
        ArgumentNullException.ThrowIfNull(callContext);

        var transcriptLength = response.Text?.Length ?? 0;
        var terminal = transcriptLength == 0
            ? SpeechProxyRecognitionTerminal.Empty
            : SpeechProxyRecognitionTerminal.Transcript;

        // Erfolgreiche Abschlüsse einschließlich leerer Ergebnisse bleiben wie die Startmeldung auf Information sichtbar.
        Log(LogLevel.Information, terminal, transcriptLength, forwardedAudio, callContext, exception: null);
    }

    /// <summary>
    /// Protokolliert einen angeforderten Aufruferabbruch mit den bis dahin erfassten Audiozählern.
    /// Information hält diesen Abschluss bei derselben Mindestlogstufe wie den Beginn sichtbar.
    /// </summary>
    public void LogCallerGone(SpeechProxyForwardedAudio forwardedAudio, SpeechProxyCallContext callContext)
    {
        ArgumentNullException.ThrowIfNull(forwardedAudio);
        ArgumentNullException.ThrowIfNull(callContext);
        Log(LogLevel.Information, SpeechProxyRecognitionTerminal.CallerGone, transcriptLength: 0, forwardedAudio, callContext, exception: null);
    }

    public void LogFailed(
        Exception exception,
        SpeechProxyForwardedAudio forwardedAudio,
        SpeechProxyCallContext callContext)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(forwardedAudio);
        ArgumentNullException.ThrowIfNull(callContext);
        Log(LogLevel.Warning, SpeechProxyRecognitionTerminal.Failed, transcriptLength: 0, forwardedAudio, callContext, exception);
    }

    private void Log(
        LogLevel level,
        SpeechProxyRecognitionTerminal terminal,
        int transcriptLength,
        SpeechProxyForwardedAudio forwardedAudio,
        SpeechProxyCallContext callContext,
        Exception? exception)
    {
        _logger.Log(
            level,
            default,
            exception,
            TerminalTemplate,
            terminal,
            transcriptLength,
            forwardedAudio.Chunks,
            forwardedAudio.Bytes,
            callContext.ClientCorrelationId,
            callContext.SessionId,
            callContext.TraceId);
    }
}
