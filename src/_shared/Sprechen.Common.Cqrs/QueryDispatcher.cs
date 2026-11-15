namespace Sprechen.Common.Cqrs;

/// <summary>
/// Leitet Abfragen an den für ihren Typ registrierten Handler weiter,
/// damit Aufrufer dessen Auswahl nicht selbst übernehmen müssen.
/// </summary>
public sealed class QueryDispatcher : CachedDelegateDispatcherBase, IQueryDispatcher
{
    public QueryDispatcher()
        : base("query")
    {
    }

    public void Register<TQuery, TResult>(IQueryHandler<TQuery, TResult> handler)
        where TQuery : IQuery<TResult>
    {
        ArgumentNullException.ThrowIfNull(handler);
        RegisterCore<TQuery, TResult>(typeof(TQuery), handler.HandleAsync);
    }

    public async Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        return await DispatchCoreAsync<TResult>(query, cancellationToken).ConfigureAwait(false);
    }
}


