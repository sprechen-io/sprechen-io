using Grpc.Core;

namespace Sprechen.Service.Deepgram.Tests.Grpc;

internal sealed class NeverCompletingAsyncStreamReader<T> : IAsyncStreamReader<T>
{
    public T Current { get; private set; } = default!;

    public async Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        return false;
    }
}
