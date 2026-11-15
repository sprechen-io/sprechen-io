namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class NoOpSpeechAssistantTrace : ISpeechAssistantTrace
{
    public ValueTask WriteAsync(
        string correlationId,
        SpeechAssistantTraceScope scope,
        SpeechAssistantTraceLevel level,
        string message,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}
