using Sprechen.Common.Cqrs;

namespace Sprechen.Common.Cqrs.Tests;

internal sealed class AnotherQueryHandler : IQueryHandler<AnotherQuery, int>
{
    public Task<int> HandleAsync(AnotherQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(query.Value * query.Value);
    }
}


