using Sprechen.Common.Cqrs;

namespace Sprechen.Common.Cqrs.Tests;

internal sealed record AnotherQuery(int Value) : IQuery<int>;


