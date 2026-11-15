using Grpc.Core;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class SequenceAsyncStreamReader<T> : IAsyncStreamReader<T>
{
    private readonly IReadOnlyList<T> _items;
    private readonly Exception? _exceptionAfterItems;
    private int _index;
    private bool _exceptionThrown;

    public SequenceAsyncStreamReader(IReadOnlyList<T> items)
        : this(items, exceptionAfterItems: null)
    {
    }

    public SequenceAsyncStreamReader(IReadOnlyList<T> items, Exception? exceptionAfterItems)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _exceptionAfterItems = exceptionAfterItems;
    }

    public T Current { get; private set; } = default!;

    public Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_index >= _items.Count)
        {
            if (_exceptionAfterItems is not null && !_exceptionThrown)
            {
                _exceptionThrown = true;
                throw _exceptionAfterItems;
            }

            return Task.FromResult(false);
        }

        Current = _items[_index];
        _index++;
        return Task.FromResult(true);
    }
}


