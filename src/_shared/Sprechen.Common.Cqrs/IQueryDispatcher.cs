namespace Sprechen.Common.Cqrs;

/// <summary>
/// Leitet Leseabfragen an den zuständigen Handler weiter,
/// damit Aufrufer dessen Auswahl nicht selbst übernehmen müssen.
/// </summary>
public interface IQueryDispatcher
{
    Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}


