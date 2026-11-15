using Sprechen.Common.Cqrs;

namespace Sprechen.Common.Cqrs.Tests;

internal sealed record AnotherCommand(int Value) : ICommand<int>;


