using Grpc.Core;

namespace Sprechen.Service.Tests.TestDoubles;

/// <summary>
/// Stellt für gRPC-Tests einen sofort beendeten Anfragestrom bereit.
/// </summary>
internal sealed class NoOpAsyncStreamReader<T> : IAsyncStreamReader<T>
{
    public T Current => default!;

    public Task<bool> MoveNext(CancellationToken cancellationToken) => Task.FromResult(false);
}
