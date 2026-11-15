using Grpc.Core;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

/// <summary>
/// Hält einen Schreibaufruf bis zum Abbruch seines Tokens offen.
/// Damit lassen sich Stop und Dispose bei blockiertem Versand prüfen.
/// </summary>
internal sealed class BlockingClientStreamWriter<T> : IClientStreamWriter<T> where T : class
{
    private readonly TaskCompletionSource _firstWriteStarted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task FirstWriteStarted => _firstWriteStarted.Task;

    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(T message) => WriteAsync(message, CancellationToken.None);

    public async Task WriteAsync(T message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        _firstWriteStarted.TrySetResult();
        await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
    }

    public Task CompleteAsync() => Task.CompletedTask;
}
