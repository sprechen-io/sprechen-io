using Grpc.Core;

namespace Sprechen.Service.Benchmarks.TestDoubles;

internal sealed class NullServerStreamWriter<T> : IServerStreamWriter<T>
{
    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(T message)
    {
        _ = message;
        return Task.CompletedTask;
    }
}

