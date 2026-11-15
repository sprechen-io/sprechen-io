using Grpc.Core;

namespace Sprechen.Web.HostSupport.Benchmarks.Grpc;

internal sealed class SyntheticServerResponseStream<T> : IAsyncStreamReader<T>
    where T : class
{
    private readonly IReadOnlyList<T> _items;
    private int _index = -1;

    public SyntheticServerResponseStream(IReadOnlyList<T> items)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    public T Current { get; private set; } = null!;

    public Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        var nextIndex = _index + 1;
        if (nextIndex >= _items.Count)
        {
            return Task.FromResult(false);
        }

        _index = nextIndex;
        Current = _items[_index];
        return Task.FromResult(true);
    }
}
