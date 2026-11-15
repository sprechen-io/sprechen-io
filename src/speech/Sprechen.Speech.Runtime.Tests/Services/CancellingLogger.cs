using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class CancellingLogger<T>(CancellationTokenSource cancellation) : ILogger<T>
{
    private readonly CancellationTokenSource _cancellation = cancellation;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => _cancellation.Cancel();
}
