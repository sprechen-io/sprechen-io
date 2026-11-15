namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class AsyncCleanup : IAsyncDisposable
{
    private readonly Func<ValueTask> _cleanup;

    public AsyncCleanup(Func<ValueTask> cleanup)
    {
        _cleanup = cleanup ?? throw new ArgumentNullException(nameof(cleanup));
    }

    public ValueTask DisposeAsync() => _cleanup();
}
