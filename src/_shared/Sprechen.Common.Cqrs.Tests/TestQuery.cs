using Sprechen.Common.Cqrs;

namespace Sprechen.Common.Cqrs.Tests;

internal sealed record TestQuery(string Input) : IQuery<string>;


