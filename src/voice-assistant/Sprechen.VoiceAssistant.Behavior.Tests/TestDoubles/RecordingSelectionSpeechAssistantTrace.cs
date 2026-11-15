namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal sealed class RecordingSelectionSpeechAssistantTrace : ISpeechAssistantTrace
{
    private readonly List<RecordedSelectionSpeechAssistantTraceEntry> _entries = [];

    public IReadOnlyList<RecordedSelectionSpeechAssistantTraceEntry> Entries => _entries;

    public ValueTask WriteAsync(
        string correlationId,
        SpeechAssistantTraceScope scope,
        SpeechAssistantTraceLevel level,
        string message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _entries.Add(new RecordedSelectionSpeechAssistantTraceEntry(message.Trim()));
        return ValueTask.CompletedTask;
    }
}
