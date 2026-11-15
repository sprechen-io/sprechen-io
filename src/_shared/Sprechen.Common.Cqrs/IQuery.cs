namespace Sprechen.Common.Cqrs;

/// <summary>
/// Kennzeichnet eine reine Leseabfrage und ihren Ergebnistyp, damit der
/// Dispatcher sie einem Abfrage-Handler zuordnen kann.
/// </summary>
/// <typeparam name="TResult">Das gelesene Ergebnis.</typeparam>
public interface IQuery<TResult>
{
}


