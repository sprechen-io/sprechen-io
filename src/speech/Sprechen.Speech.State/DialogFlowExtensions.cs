namespace Sprechen.Speech.State;

/// <summary>
/// Erlaubt kurze Ereignisaufrufe an Dialogmodule. Ohne ausdrücklichen Modus gilt Einreihen, ohne Abbruchtoken besteht kein aufruferseitiger Abbruch.
/// </summary>
internal static class DialogFlowExtensions
{
    public static ValueTask DispatchAsync<TEvent>(
        this IDialogFlow<TEvent> flow,
        TEvent @event)
        where TEvent : notnull
    {
        ArgumentNullException.ThrowIfNull(flow);
        ArgumentNullException.ThrowIfNull(@event);
        return flow.DispatchAsync(@event, DispatchMode.Enqueue, CancellationToken.None);
    }

    public static ValueTask DispatchAsync<TEvent>(
        this IDialogFlow<TEvent> flow,
        TEvent @event,
        DispatchMode mode)
        where TEvent : notnull
    {
        ArgumentNullException.ThrowIfNull(flow);
        ArgumentNullException.ThrowIfNull(@event);
        return flow.DispatchAsync(@event, mode, CancellationToken.None);
    }

    public static ValueTask DispatchAsync<TEvent>(
        this IDialogFlow<TEvent> flow,
        TEvent @event,
        CancellationToken cancellationToken)
        where TEvent : notnull
    {
        ArgumentNullException.ThrowIfNull(flow);
        ArgumentNullException.ThrowIfNull(@event);
        return flow.DispatchAsync(@event, DispatchMode.Enqueue, cancellationToken);
    }
}

