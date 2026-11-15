using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.TestDoubles;

public sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly List<CapturingLogEntry> _entries = [];

    public IReadOnlyList<CapturingLogEntry> Entries => _entries;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoOpDisposable.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> values)
        {
            throw new InvalidOperationException("Expected structured log values.");
        }

        var captured = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var pair in values)
        {
            captured[pair.Key] = pair.Value;
        }

        _entries.Add(new CapturingLogEntry(logLevel, captured));
    }
}

