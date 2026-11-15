namespace Sprechen.Speech.State;

/// <summary>
/// Leitet Ereignisse an ein ausgewähltes Modul oder nacheinander an
/// eine Momentaufnahme weiter. Beim Rundruf werden inzwischen entfernte Module übersprungen.
/// </summary>
internal sealed class DialogHostEventDispatch<TEvent> where TEvent : notnull
{
    internal ValueTask DispatchToActiveAsync(
        IDialogFlow<TEvent> active,
        TEvent @event,
        DispatchMode mode,
        CancellationToken cancellationToken)
    {
        return active.DispatchAsync(@event, mode, cancellationToken);
    }

    internal async ValueTask BroadcastAsync(
        IEnumerable<IDialogFlow<TEvent>> snapshot,
        TEvent @event,
        DispatchMode mode,
        CancellationToken cancellationToken,
        Func<IDialogFlow<TEvent>, CancellationToken, ValueTask<bool>> isStillActiveAsync)
    {
        foreach (var module in snapshot)
        {
            if (!await isStillActiveAsync(module, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            await module.DispatchAsync(@event, mode, cancellationToken).ConfigureAwait(false);
        }
    }
}
