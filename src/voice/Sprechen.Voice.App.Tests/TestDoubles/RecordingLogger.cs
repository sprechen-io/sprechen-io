using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> _entries = new();

    public IReadOnlyCollection<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> Entries => _entries.ToArray();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (formatter is null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        _entries.Enqueue((logLevel, eventId, formatter(state, exception), exception));
    }
}


