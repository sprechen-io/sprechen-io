using Microsoft.Extensions.Logging;

namespace Sprechen.Forms.Tests.Services;

internal sealed class FakeLogger<T> : ILogger<T>
{
    public List<string> LogEntries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        LogEntries.Add(formatter(state, exception));
    }
}
