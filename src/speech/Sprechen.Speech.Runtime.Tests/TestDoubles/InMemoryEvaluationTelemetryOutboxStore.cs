using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class InMemoryEvaluationTelemetryOutboxStore : IEvaluationTelemetryOutboxStore
{
    private readonly Dictionary<string, string> _checkpoints = new(StringComparer.Ordinal);

    internal IReadOnlyDictionary<string, string> Checkpoints => _checkpoints;

    public IReadOnlyList<string> ListKeys() => [.. _checkpoints.Keys];

    public string? Read(string key) => _checkpoints.GetValueOrDefault(key);

    public void Write(string key, string checkpoint) => _checkpoints[key] = checkpoint;

    public void Delete(string key) => _checkpoints.Remove(key);
}
