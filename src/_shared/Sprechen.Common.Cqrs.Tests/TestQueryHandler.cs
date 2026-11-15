using Sprechen.Common.Cqrs;

namespace Sprechen.Common.Cqrs.Tests;

internal sealed class TestQueryHandler : IQueryHandler<TestQuery, string>
{
    public Task<string> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"query result: {query.Input}");
    }
}


