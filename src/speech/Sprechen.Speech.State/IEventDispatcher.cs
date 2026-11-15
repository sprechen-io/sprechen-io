namespace Sprechen.Speech.State;

/// <summary>
/// Beschreibt die Ereigniszustellung für <see cref="IStateEffect{TEvent,TContext}"/>,
/// damit Effekte Folgeereignisse in die Reducer-Verarbeitung geben können.
/// Der Modus bestimmt, ob beim Einreihen laufenden Effekten ein Abbruch signalisiert wird.
/// </summary>
public interface IEventDispatcher<TEvent>
{
    ValueTask DispatchAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken);
}

