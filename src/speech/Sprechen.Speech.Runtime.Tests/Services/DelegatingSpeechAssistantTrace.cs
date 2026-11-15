namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class DelegatingSpeechAssistantTrace(
    Func<CancellationToken, ValueTask> write) : ISpeechAssistantTrace
{
    private readonly Func<CancellationToken, ValueTask> _write = write;

    public int WriteCount { get; private set; }

    public ValueTask WriteAsync(
        string correlationId,
        SpeechAssistantTraceScope scope,
        SpeechAssistantTraceLevel level,
        string message,
        CancellationToken cancellationToken)
    {
        WriteCount++;
        return _write(cancellationToken);
    }
}
