using Sprechen.Common.Cqrs;

namespace Sprechen.Common.Cqrs.Tests;

internal sealed class AnotherCommandHandler : ICommandHandler<AnotherCommand, int>
{
    public Task<int> HandleAsync(AnotherCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(command.Value * 2);
    }
}


