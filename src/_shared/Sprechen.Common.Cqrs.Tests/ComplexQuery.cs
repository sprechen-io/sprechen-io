using Sprechen.Common.Cqrs;

namespace Sprechen.Common.Cqrs.Tests;

internal sealed record ComplexQuery(string Name) : IQuery<ComplexResult>;


