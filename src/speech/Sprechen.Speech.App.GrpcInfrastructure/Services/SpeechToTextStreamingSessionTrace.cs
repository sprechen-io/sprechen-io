using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Protokolliert Lebenszyklus, Empfangszähler und Fehler einer
/// gRPC-Gesprächssitzung unter ihrer gemeinsamen Korrelationskennung.
/// </summary>
internal sealed class SpeechToTextStreamingSessionTrace
{
    private const string TraceScope = "speech-conversation";

    private readonly ISpeechClientTrace _trace;
    private readonly string _correlationId;
    private readonly string? _languageCode;

    internal SpeechToTextStreamingSessionTrace(
        ISpeechClientTrace trace,
        string correlationId,
        string? languageCode)
    {
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        _correlationId = string.IsNullOrWhiteSpace(correlationId)
            ? throw new ArgumentException("Correlation id must not be blank.", nameof(correlationId))
            : correlationId.Trim();
        _languageCode = string.IsNullOrWhiteSpace(languageCode) ? null : languageCode.Trim();
    }

    internal ValueTask StartedAsync()
    {
        return WriteAsync(LogLevel.Information, $"speech-to-conversation-stream started languageCode={LanguageCodeForTrace}");
    }

    internal ValueTask StoppingAsync()
    {
        return WriteAsync(LogLevel.Information, $"speech-to-conversation-stream stopping languageCode={LanguageCodeForTrace}");
    }

    internal ValueTask StopCompletedAsync()
    {
        return WriteAsync(LogLevel.Information, $"speech-to-conversation-stream stop completed languageCode={LanguageCodeForTrace}");
    }

    internal ValueTask StopFailedAsync(StatusCode statusCode)
    {
        return WriteAsync(LogLevel.Error, $"speech-to-conversation-stream stop failed statusCode={statusCode}");
    }

    internal ValueTask DisposedAsync()
    {
        return WriteAsync(LogLevel.Debug, $"speech-to-conversation-stream disposed languageCode={LanguageCodeForTrace}");
    }

    internal ValueTask ReceiveCompletedAsync(int eventCount, int transcriptCount)
    {
        return WriteAsync(LogLevel.Information, $"speech-to-conversation-stream completed eventCount={eventCount} transcriptCount={transcriptCount}");
    }

    internal ValueTask ReceiveFailedEventAsync(int eventCount, int transcriptCount)
    {
        return WriteAsync(LogLevel.Error, $"speech-to-conversation-stream failed-event eventCount={eventCount} transcriptCount={transcriptCount}");
    }

    internal ValueTask ReceiveCanceledAsync(StatusCode statusCode, int eventCount, int transcriptCount)
    {
        return WriteAsync(LogLevel.Information, $"speech-to-conversation-stream canceled statusCode={statusCode} eventCount={eventCount} transcriptCount={transcriptCount}");
    }

    internal ValueTask ReceiveFailedRpcAsync(StatusCode statusCode, int eventCount, int transcriptCount)
    {
        return WriteAsync(LogLevel.Error, $"speech-to-conversation-stream failed statusCode={statusCode} eventCount={eventCount} transcriptCount={transcriptCount}");
    }

    internal ValueTask ReceiveCanceledAsync(int eventCount, int transcriptCount)
    {
        return WriteAsync(LogLevel.Information, $"speech-to-conversation-stream canceled eventCount={eventCount} transcriptCount={transcriptCount}");
    }

    internal ValueTask ReceiveFailedUnexpectedAsync(int eventCount, int transcriptCount, string errorType)
    {
        return WriteAsync(LogLevel.Error, $"speech-to-conversation-stream failed eventCount={eventCount} transcriptCount={transcriptCount} errorType={errorType}");
    }

    private string LanguageCodeForTrace => _languageCode ?? "<null>";

    private ValueTask WriteAsync(
        LogLevel level,
        string message)
    {
        return _trace.WriteAsync(
            TraceScope,
            level,
            _correlationId,
            message,
            CancellationToken.None);
    }
}
