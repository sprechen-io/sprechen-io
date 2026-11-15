using Grpc.Core;

namespace Sprechen.Service.Tests.TestDoubles;

/// <summary>
/// Verwirft sämtliche geschriebenen gRPC-Testantworten.
/// </summary>
internal sealed class NoOpServerStreamWriter<T> : IServerStreamWriter<T>
{
    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(T message) => Task.CompletedTask;
}
