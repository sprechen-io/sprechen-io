namespace Sprechen.Common.Cqrs;

/// <summary>
/// Leitet Befehle an den für ihren Typ registrierten Handler weiter,
/// damit Aufrufer dessen Auswahl nicht selbst übernehmen müssen.
/// </summary>
public sealed class CommandDispatcher : CachedDelegateDispatcherBase, ICommandDispatcher
{
    public CommandDispatcher()
        : base("command")
    {
    }

    public void Register<TCommand, TResult>(ICommandHandler<TCommand, TResult> handler)
        where TCommand : ICommand<TResult>
    {
        ArgumentNullException.ThrowIfNull(handler);
        RegisterCore<TCommand, TResult>(typeof(TCommand), handler.HandleAsync);
    }

    public async Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        return await DispatchCoreAsync<TResult>(command, cancellationToken).ConfigureAwait(false);
    }
}


