using Grpc.Core;

namespace Sprechen.Web.HostSupport.Benchmarks.Grpc;

internal sealed class SyntheticClientStreamWriter<T> : IClientStreamWriter<T>
{
    public WriteOptions? WriteOptions { get; set; }

    public int MessageCount { get; private set; }

    public Task WriteAsync(T message)
    {
        ArgumentNullException.ThrowIfNull(message);

        MessageCount++;
        return Task.CompletedTask;
    }

    public Task CompleteAsync()
    {
        return Task.CompletedTask;
    }
}
