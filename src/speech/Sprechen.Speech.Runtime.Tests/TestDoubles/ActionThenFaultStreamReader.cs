using Grpc.Core;
using Sprechen.Proto;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class ActionThenFaultStreamReader : IAsyncStreamReader<AudioResponse>
{
    private readonly Action _onMoveNext;
    private readonly Exception _fault;
    private bool _thrown;

    public ActionThenFaultStreamReader(Action onMoveNext, Exception fault)
    {
        _onMoveNext = onMoveNext;
        _fault = fault;
    }

    public AudioResponse Current { get; private set; } = default!;

    public Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        if (_thrown)
        {
            return Task.FromResult(false);
        }

        _onMoveNext();
        _thrown = true;
        throw _fault;
    }
}
