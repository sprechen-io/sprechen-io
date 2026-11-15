namespace Sprechen.Common.Cqrs;

/// <summary>
/// Führt genau einen Befehlstyp aus und liefert das Ergebnis, damit der Dispatcher
/// typsicher bleibt.
/// </summary>
/// <typeparam name="TCommand">Der Befehlstyp.</typeparam>
/// <typeparam name="TResult">Das Ergebnis.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}


