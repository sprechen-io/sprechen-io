namespace Sprechen.Speech.State;

/// <summary>
/// Beschreibt einen vom <see cref="IStateReducer{TState,TEvent,TContext}"/>
/// zusammen mit dem Folgezustand ausgegebenen Effekt. Ein- und Ausgabe wie Wartezeiten,
/// Netzwerkaufrufe oder Telemetrie gehören hierher. Kontext und
/// <see cref="IEventDispatcher{TEvent}"/> erlauben die Ausführung und das Senden von Folgeereignissen.
/// </summary>
public interface IStateEffect<TEvent, TContext>
{
    ValueTask ExecuteAsync(TContext context, IEventDispatcher<TEvent> dispatcher, CancellationToken cancellationToken);
}

