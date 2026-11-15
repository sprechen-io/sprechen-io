using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Sprechen.Service.Deepgram.Tests.TestDoubles;

internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<(
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> Properties)> _entries = new();

    public IReadOnlyCollection<(
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> Properties)> Entries
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
        _entries.Enqueue((logLevel, eventId, formatter(state, exception), exception, CaptureProperties(state)));
    }

    private static IReadOnlyDictionary<string, object?> CaptureProperties<TState>(TState state)
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> structuredState)
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal);
        }

        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in structuredState)
        {
            properties[property.Key] = property.Value;
        }

        return properties;
    }
}
