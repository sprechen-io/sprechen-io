using Sprechen.Common.Cqrs;

namespace Sprechen.Common.Cqrs.Tests;

internal sealed class ComplexQueryHandler : IQueryHandler<ComplexQuery, ComplexResult>
{
    public Task<ComplexResult> HandleAsync(ComplexQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ComplexResult(query.Name, 42));
    }
}


