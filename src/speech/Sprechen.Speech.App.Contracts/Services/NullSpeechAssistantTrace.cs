namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Verwirft Diagnoseschreibvorgänge ohne Diagnosesenke, beachtet aber das Abbruchtoken,
/// damit deaktivierte Diagnose den Abbruchvertrag erhält.
/// </summary>
public sealed class NullSpeechAssistantTrace : ISpeechAssistantTrace
{
    public static NullSpeechAssistantTrace Instance { get; } = new();

    private NullSpeechAssistantTrace()
    {
    }

    public ValueTask WriteAsync(
        string correlationId,
        SpeechAssistantTraceScope scope,
        SpeechAssistantTraceLevel level,
        string message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}
