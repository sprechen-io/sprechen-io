namespace Sprechen.Common.Cqrs;

/// <summary>
/// Kennzeichnet Befehle ohne fachlichen Rückgabewert, die den gemeinsamen
/// Handlervertrag mit Unit als Ergebnistyp verwenden.
/// </summary>
public interface ICommandWithoutResult : ICommand<Unit>
{
}


