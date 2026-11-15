using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal sealed class DisappearingAfterEnumerationStore(
    InMemoryEvaluationTelemetryOutboxStore inner,
    string disappearingKey) : IEvaluationTelemetryOutboxStore
{
    private int _readCount;

    public IReadOnlyList<string> ListKeys() => inner.ListKeys();

    public string? Read(string key)
    {
        if (string.Equals(key, disappearingKey, StringComparison.Ordinal)
            && Interlocked.Increment(ref _readCount) == 1)
        {
            inner.Delete(key);
            return null;
        }

        return inner.Read(key);
    }

    public void Write(string key, string checkpoint) => inner.Write(key, checkpoint);

    public void Delete(string key) => inner.Delete(key);
}
