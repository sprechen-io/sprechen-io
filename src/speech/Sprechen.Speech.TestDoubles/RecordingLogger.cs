using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.TestDoubles;

/// <summary>
/// Speichert formatierte Logmeldungen mit Level, Ereigniskennung und Ausnahme
/// für spätere Assertions im Test.
/// </summary>
public sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<RecordedLogEntry> _entries = [];
    private readonly object _gate = new();

    public IReadOnlyList<RecordedLogEntry> Entries
    {
        get
        {
            lock (_gate)
            {
                return _entries.ToArray();
            }
        }
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoOpDisposable.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        var message = formatter(state, exception);
        lock (_gate)
        {
            _entries.Add(new RecordedLogEntry(logLevel, eventId, message, exception));
        }
    }
}
