using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Sprechen.Web.HostSupport.Tests.TestDoubles;

internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> _entries = new();

    public IReadOnlyCollection<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> Entries
        => _entries.ToArray();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
        => NoOpDisposable.Instance;

    public bool IsEnabled(LogLevel logLevel)
        => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        _entries.Enqueue((logLevel, eventId, formatter(state, exception), exception));
    }
}
