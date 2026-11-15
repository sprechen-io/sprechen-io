using Grpc.Core;

namespace Sprechen.Service.Tests.Grpc;

internal sealed class RecordingServerStreamWriter<T> : IServerStreamWriter<T>
{
    public List<T> Items { get; } = new();

    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(T message)
    {
        Items.Add(message);
        return Task.CompletedTask;
    }
}

