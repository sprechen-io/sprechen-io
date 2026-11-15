namespace Sprechen.Speech.State;

/// <summary>
/// Beschreibt ein Modul auf dem Stapel des <see cref="IDialogHost{TEvent}"/>
/// mit Aktivierungs- und Deaktivierungsaufrufen, Ereigniszustellung und asynchroner Freigabe.
/// Ein Reducer-Modul verarbeitet Ereignisse über seine Zustands- und Effektkette.
/// </summary>
public interface IDialogFlow<TEvent> : IAsyncDisposable where TEvent : notnull
{
    ValueTask OnActivatedAsync(CancellationToken cancellationToken);

    ValueTask OnDeactivatedAsync(CancellationToken cancellationToken);

    ValueTask DispatchAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken);
}

