namespace Sprechen.Speech.State;

/// <summary>
/// Beschreibt einen stapelbasierten Dialoghost mit <see cref="IDialogFlow{TEvent}"/>-Modulen.
/// Ereignisse gehen an die aktive Spitze oder per Rundruf an die Module.
/// Stapeln, Entfernen und Ersetzen verbinden Änderungen mit den betroffenen Lebenszyklusaufrufen.
/// </summary>
public interface IDialogHost<TEvent> : IAsyncDisposable where TEvent : notnull
{
    int Depth { get; }

    ValueTask StartAsync(IDialogFlow<TEvent> root, CancellationToken cancellationToken);

    ValueTask DispatchToActiveAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken);

    ValueTask BroadcastAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken);

    ValueTask PushAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken);

    ValueTask ReplaceTopAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken);

    ValueTask PopAsync(CancellationToken cancellationToken);

    ValueTask PopAsync(TEvent resumeEvent, DispatchMode resumeMode, CancellationToken cancellationToken);
}

