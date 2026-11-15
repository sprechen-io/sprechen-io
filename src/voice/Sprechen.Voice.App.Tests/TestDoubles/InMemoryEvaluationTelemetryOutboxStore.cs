using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class InMemoryEvaluationTelemetryOutboxStore : IEvaluationTelemetryOutboxStore
{
    private readonly Dictionary<string, string> _checkpoints = new(StringComparer.Ordinal);

    public IReadOnlyList<string> ListKeys() => [.. _checkpoints.Keys];

    public string? Read(string key) => _checkpoints.GetValueOrDefault(key);

    public void Write(string key, string checkpoint) => _checkpoints[key] = checkpoint;

    public void Delete(string key) => _checkpoints.Remove(key);
}
