namespace Sprechen.Speech.State;

/// <summary>
/// Beschreibt die deterministische, seiteneffektfreie Ableitung eines Folgezustands
/// aus Zustand und Ereignis. <see cref="StateUpdate{TState,TEvent,TContext}"/>
/// bündelt das Ergebnis mit später auszuführenden Effekten. Ein- und Ausgabe
/// gehören in die zugehörigen <see cref="IStateEffect{TEvent,TContext}"/>-Instanzen.
/// </summary>
public interface IStateReducer<TState, TEvent, TContext>
{
    StateUpdate<TState, TEvent, TContext> Reduce(TState state, TEvent @event);
}

