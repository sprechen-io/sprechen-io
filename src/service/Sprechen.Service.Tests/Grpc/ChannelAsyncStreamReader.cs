using Grpc.Core;
using System.Threading.Channels;

namespace Sprechen.Service.Tests.Grpc;

internal sealed class ChannelAsyncStreamReader<T> : IAsyncStreamReader<T>
{
    private readonly System.Threading.Channels.Channel<T> _channel = System.Threading.Channels.Channel.CreateUnbounded<T>();

    public T Current { get; private set; } = default!;

    public void Write(T item)
    {
        if (!_channel.Writer.TryWrite(item))
        {
            throw new InvalidOperationException("Unable to enqueue a stream item.");
        }
    }

    public void Complete()
    {
        _channel.Writer.TryComplete();
    }

    public async Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_channel.Reader.TryRead(out var item))
            {
                Current = item;
                return true;
            }
        }

        return false;
    }
}
