namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class RecordingSpeechAssistantTrace : ISpeechAssistantTrace
{
    private readonly object _gate = new();
    private readonly List<RecordedSpeechAssistantTraceEntry> _entries = [];

    public IReadOnlyList<RecordedSpeechAssistantTraceEntry> Entries
    {
        get
        {
            lock (_gate)
            {
                return _entries.ToArray();
            }
        }
    }

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

        lock (_gate)
        {
            _entries.Add(new RecordedSpeechAssistantTraceEntry(
                correlationId.Trim(),
                scope,
                level,
                message.Trim()));
        }

        return ValueTask.CompletedTask;
    }
}
