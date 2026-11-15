using Grpc.Core;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

internal sealed class ThrowingServerStreamWriter<T> : IServerStreamWriter<T>
{
    private readonly int _throwOnWrite;
    private int _writes;

    public ThrowingServerStreamWriter()
        : this(throwOnWrite: 1)
    {
    }

    public ThrowingServerStreamWriter(int throwOnWrite)
    {
        if (throwOnWrite <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(throwOnWrite), throwOnWrite, "Throw-on-write must be positive.");
        }

        _throwOnWrite = throwOnWrite;
    }

    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(T message)
    {
        _writes++;
        if (_writes >= _throwOnWrite)
        {
            throw new InvalidOperationException("Simulated stream write failure.");
        }

        return Task.CompletedTask;
    }
}


