namespace Sprechen.Speech.State;

/// <summary>
/// Erlaubt kurze Ereigniszustellungen aus Effekten. Ohne ausdrücklichen Modus gilt Einreihen, ohne Abbruchtoken besteht kein aufruferseitiger Abbruch.
/// </summary>
internal static class EventDispatcherExtensions
{
    public static ValueTask DispatchAsync<TEvent>(
        this IEventDispatcher<TEvent> dispatcher,
        TEvent @event)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(@event);
        return dispatcher.DispatchAsync(@event, DispatchMode.Enqueue, CancellationToken.None);
    }

    public static ValueTask DispatchAsync<TEvent>(
        this IEventDispatcher<TEvent> dispatcher,
        TEvent @event,
        DispatchMode mode)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(@event);
        return dispatcher.DispatchAsync(@event, mode, CancellationToken.None);
    }

    public static ValueTask DispatchAsync<TEvent>(
        this IEventDispatcher<TEvent> dispatcher,
        TEvent @event,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(@event);
        return dispatcher.DispatchAsync(@event, DispatchMode.Enqueue, cancellationToken);
    }
}

