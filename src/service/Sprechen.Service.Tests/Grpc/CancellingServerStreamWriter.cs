using Grpc.Core;

namespace Sprechen.Service.Tests.Grpc;

internal sealed class CancellingServerStreamWriter<T> : IServerStreamWriter<T>
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly int _cancelAfterWrites;
    private int _writes;

    public CancellingServerStreamWriter(CancellationTokenSource cancellationTokenSource)
        : this(cancellationTokenSource, cancelAfterWrites: 1)
    {
    }

    public CancellingServerStreamWriter(CancellationTokenSource cancellationTokenSource, int cancelAfterWrites)
    {
        _cancellationTokenSource = cancellationTokenSource ?? throw new ArgumentNullException(nameof(cancellationTokenSource));

        if (cancelAfterWrites <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cancelAfterWrites), cancelAfterWrites, "Cancel-after-writes must be positive.");
        }

        _cancelAfterWrites = cancelAfterWrites;
    }

    public List<T> Items { get; } = new();

    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(T message)
    {
        Items.Add(message);

        _writes++;
        if (_writes >= _cancelAfterWrites && !_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }

        return Task.CompletedTask;
    }
}

