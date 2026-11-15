namespace Sprechen.Common.Cqrs;

/// <summary>
/// Bearbeitet Befehle ohne fachliches Ergebnis über den gemeinsamen Handlervertrag
/// mit Unit als Ergebnistyp.
/// </summary>
/// <typeparam name="TCommand">Der Befehlstyp.</typeparam>
public interface ICommandHandlerWithoutResult<in TCommand> : ICommandHandler<TCommand, Unit>
    where TCommand : ICommandWithoutResult
{
}


