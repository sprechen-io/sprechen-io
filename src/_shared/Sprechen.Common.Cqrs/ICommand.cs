namespace Sprechen.Common.Cqrs;

/// <summary>
/// Kennzeichnet eine Anweisung, die Zustand ändert, damit sie nicht als reine Abfrage
/// behandelt wird.
/// </summary>
/// <typeparam name="TResult">Das Ergebnis nach der Ausführung.</typeparam>
public interface ICommand<TResult>
{
}


