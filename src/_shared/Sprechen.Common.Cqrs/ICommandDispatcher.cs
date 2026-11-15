namespace Sprechen.Common.Cqrs;

/// <summary>
/// Nimmt Befehle entgegen und führt den passenden Handler aus, damit Aufrufer die
/// Registrierung nicht kennen müssen.
/// </summary>
public interface ICommandDispatcher
{
    Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}


