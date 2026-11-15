using Sprechen.Common.Cqrs;

namespace Sprechen.Common.Cqrs.Tests;

internal sealed record TestCommand(string Input) : ICommand<string>;


