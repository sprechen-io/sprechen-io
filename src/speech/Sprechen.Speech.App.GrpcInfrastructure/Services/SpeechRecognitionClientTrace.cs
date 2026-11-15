using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Protokolliert Beginn, Ergebnismerkmale und Fehler der Spracherkennung
/// mit ihrer Korrelationskennung, ohne den Transkripttext auszugeben.
/// </summary>
internal sealed class SpeechRecognitionClientTrace
{
    private readonly ISpeechClientTrace _trace;

    public SpeechRecognitionClientTrace(ISpeechClientTrace trace)
    {
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
    }

    public ValueTask SpeechToTextStartedAsync(string correlationId, int chunkCount, string? languageCode)
        => WriteAsync(
            LogLevel.Information,
            correlationId,
            $"speech-to-text start chunks={chunkCount} languageCode={languageCode ?? "<null>"}");

    public ValueTask SpeechToTextResultAsync(string correlationId, SpeechTranscript transcript)
        => WriteAsync(
            LogLevel.Information,
            correlationId,
            $"speech-to-text result hasText={!string.IsNullOrWhiteSpace(transcript.Text)} confidence={FormatConfidenceForDebug(transcript.ConfidencePercent)}");

    public ValueTask SpeechToTextFailedAsync(string correlationId, SpeechErrorCode errorCode, StatusCode statusCode)
        => WriteAsync(LogLevel.Error, correlationId, $"speech-to-text failed errorCode={errorCode} statusCode={statusCode}");

    private ValueTask WriteAsync(LogLevel level, string correlationId, string message)
        => _trace.WriteAsync("speech-recognition", level, correlationId, message, CancellationToken.None);

    private static string FormatConfidenceForDebug(int? confidencePercent)
        => confidencePercent.HasValue
            ? confidencePercent.Value.ToString()
            : "<null>";
}
