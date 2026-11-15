using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal sealed class InMemoryEvaluationTelemetryOutboxStore : IEvaluationTelemetryOutboxStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, string> _checkpoints = new(StringComparer.Ordinal);

    internal int DeleteFailuresRemaining { get; set; }

    internal IReadOnlyDictionary<string, string> Checkpoints
    {
        get
        {
            lock (_gate)
            {
                return new Dictionary<string, string>(_checkpoints, StringComparer.Ordinal);
            }
        }
    }

    public IReadOnlyList<string> ListKeys()
    {
        lock (_gate)
        {
            return [.. _checkpoints.Keys.Order(StringComparer.Ordinal)];
        }
    }

    public string? Read(string key)
    {
        lock (_gate)
        {
            return _checkpoints.GetValueOrDefault(key);
        }
    }

    public void Write(string key, string checkpoint)
    {
        lock (_gate)
        {
            _checkpoints[key] = checkpoint;
        }
    }

    public void Delete(string key)
    {
        lock (_gate)
        {
            if (DeleteFailuresRemaining > 0)
            {
                DeleteFailuresRemaining--;
                throw new IOException("Simulated durable checkpoint delete failure.");
            }

            _checkpoints.Remove(key);
        }
    }
}
