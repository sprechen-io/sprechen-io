using Sprechen.Common.Cqrs;

namespace Sprechen.Common.Cqrs.Tests;

internal sealed class TestVoidCommandHandler : ICommandHandlerWithoutResult<TestVoidCommand>
{
    public bool WasCalled { get; private set; }

    public Task<Unit> HandleAsync(TestVoidCommand command, CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        return Task.FromResult(Unit.Value);
    }
}


