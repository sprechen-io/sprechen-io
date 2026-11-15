using Sprechen.Proto;
using Microsoft.Extensions.Logging;
using Sprechen.Web.HostSupport.Grpc.Adapter;

namespace Sprechen.Web.HostSupport.Grpc.Proxy;

/// <summary>
/// Bündelt die Prüfung von Erkennungsantworten und die Protokollierung ihrer Abschlusszustände.
/// </summary>
internal sealed class SpeechProxyClientStreamingResponsePolicy
{
    private readonly SpeechProxySpeechToTextCompletionLogger _speechToTextCompletionLogger;
    private readonly SpeechProxySpeechTranscriptGuard _speechTranscriptGuard = new();

    public SpeechProxyClientStreamingResponsePolicy(ILogger logger)
    {
        _speechToTextCompletionLogger = new SpeechProxySpeechToTextCompletionLogger(logger);
    }

    public SpeechTranscript RequireSpeechTranscript(SpeechTranscript response)
    {
        return _speechTranscriptGuard.RequireResponse(response);
    }

    public void LogSpeechToTextCompletion(
        SpeechTranscript response,
        SpeechProxyForwardedAudio forwardedAudio,
        SpeechProxyCallContext callContext)
    {
        _speechToTextCompletionLogger.LogCompleted(response, forwardedAudio, callContext);
    }

    public void LogSpeechToTextCallerGone(
        SpeechProxyForwardedAudio forwardedAudio,
        SpeechProxyCallContext callContext)
    {
        _speechToTextCompletionLogger.LogCallerGone(forwardedAudio, callContext);
    }

    public void LogSpeechToTextFailed(
        Exception exception,
        SpeechProxyForwardedAudio forwardedAudio,
        SpeechProxyCallContext callContext)
    {
        _speechToTextCompletionLogger.LogFailed(exception, forwardedAudio, callContext);
    }
}
