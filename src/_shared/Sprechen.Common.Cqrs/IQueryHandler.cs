namespace Sprechen.Common.Cqrs;

/// <summary>
/// Beantwortet genau einen Abfragetyp, damit der Dispatcher die Leseseite typsicher
/// halten kann.
/// </summary>
/// <typeparam name="TQuery">Der Abfragetyp.</typeparam>
/// <typeparam name="TResult">Das gelesene Ergebnis.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}


