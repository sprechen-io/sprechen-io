using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal sealed class AbandonWithValueThenFailVoiceCommandDialog(
    string variableId,
    string value) : IVoiceCommandDialog
{
    private readonly ConcurrentQueue<ImmutableDictionary<string, string>> _initialValuesByExecution = new();
    private int _executionCount;

    public int ExecutionCount => Volatile.Read(ref _executionCount);

    public IReadOnlyCollection<ImmutableDictionary<string, string>> InitialValuesByExecution =>
        _initialValuesByExecution.ToArray();

    public Task<VoiceCommandDialogResult> ExecuteAsync(
        string languageCode,
        string applicationId,
        string? formId,
        SpeechAssistantApplicationSessionState applicationSession,
        IReadOnlyDictionary<string, string> initialValuesByVariableId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentNullException.ThrowIfNull(initialValuesByVariableId);

        var initialValues = initialValuesByVariableId.ToImmutableDictionary(StringComparer.Ordinal);
        _initialValuesByExecution.Enqueue(initialValues);

        if (Interlocked.Increment(ref _executionCount) == 1)
        {
            return Task.FromResult(
                VoiceCommandDialogResult.Abandoned(
                    applicationId,
                    formId,
                    initialValues.SetItem(variableId, value)));
        }

        throw new InvalidOperationException("Forced voice command dialog failure.");
    }
}
