namespace Sprechen.Speech.State;

/// <summary>
/// Stellt den Dialoghost-Vertrag und Aufrufüberladungen bereit und delegiert
/// Stapelverwaltung, Ereigniszustellung und Freigabe an seine Dialogsitzung.
/// </summary>
internal sealed class DialogHost<TEvent> : IDialogHost<TEvent> where TEvent : notnull
{
    private readonly DialogHostSession<TEvent> _session = new();

    internal DialogHost()
    {
    }

    internal int Depth => _session.Depth;

    internal ValueTask StartAsync(IDialogFlow<TEvent> root) => _session.StartAsync(root, CancellationToken.None);

    internal ValueTask StartAsync(IDialogFlow<TEvent> root, CancellationToken cancellationToken)
        => _session.StartAsync(root, cancellationToken);

    internal ValueTask DispatchToActiveAsync(TEvent @event)
        => _session.DispatchToActiveAsync(@event, DispatchMode.Enqueue, CancellationToken.None);

    internal ValueTask DispatchToActiveAsync(TEvent @event, DispatchMode mode)
        => _session.DispatchToActiveAsync(@event, mode, CancellationToken.None);

    internal ValueTask DispatchToActiveAsync(TEvent @event, CancellationToken cancellationToken)
        => _session.DispatchToActiveAsync(@event, DispatchMode.Enqueue, cancellationToken);

    internal ValueTask DispatchToActiveAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken)
        => _session.DispatchToActiveAsync(@event, mode, cancellationToken);

    internal ValueTask BroadcastAsync(TEvent @event)
        => _session.BroadcastAsync(@event, DispatchMode.Enqueue, CancellationToken.None);

    internal ValueTask BroadcastAsync(TEvent @event, DispatchMode mode)
        => _session.BroadcastAsync(@event, mode, CancellationToken.None);

    internal ValueTask BroadcastAsync(TEvent @event, CancellationToken cancellationToken)
        => _session.BroadcastAsync(@event, DispatchMode.Enqueue, cancellationToken);

    internal ValueTask BroadcastAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken)
        => _session.BroadcastAsync(@event, mode, cancellationToken);

    internal ValueTask PushAsync(IDialogFlow<TEvent> module)
        => _session.PushAsync(module, CancellationToken.None);

    internal ValueTask PushAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
        => _session.PushAsync(module, cancellationToken);

    internal ValueTask ReplaceTopAsync(IDialogFlow<TEvent> module)
        => _session.ReplaceTopAsync(module, CancellationToken.None);

    internal ValueTask ReplaceTopAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
        => _session.ReplaceTopAsync(module, cancellationToken);

    internal ValueTask PopAsync()
        => _session.PopAsync(hasResumeEvent: false, resumeEvent: default!, DispatchMode.Enqueue, CancellationToken.None);

    internal ValueTask PopAsync(TEvent resumeEvent)
        => _session.PopAsync(hasResumeEvent: true, resumeEvent, DispatchMode.Enqueue, CancellationToken.None);

    internal ValueTask PopAsync(TEvent resumeEvent, DispatchMode resumeMode)
        => _session.PopAsync(hasResumeEvent: true, resumeEvent, resumeMode, CancellationToken.None);

    internal ValueTask PopAsync(TEvent resumeEvent, CancellationToken cancellationToken)
        => _session.PopAsync(hasResumeEvent: true, resumeEvent, DispatchMode.Enqueue, cancellationToken);

    internal ValueTask PopAsync(CancellationToken cancellationToken)
        => _session.PopAsync(hasResumeEvent: false, resumeEvent: default!, DispatchMode.Enqueue, cancellationToken);

    internal ValueTask PopAsync(TEvent resumeEvent, DispatchMode resumeMode, CancellationToken cancellationToken)
        => _session.PopAsync(hasResumeEvent: true, resumeEvent, resumeMode, cancellationToken);

    internal ValueTask DisposeAsync() => _session.DisposeAsync();

    int IDialogHost<TEvent>.Depth => Depth;

    ValueTask IDialogHost<TEvent>.StartAsync(IDialogFlow<TEvent> root, CancellationToken cancellationToken)
        => StartAsync(root, cancellationToken);

    ValueTask IDialogHost<TEvent>.DispatchToActiveAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken)
        => DispatchToActiveAsync(@event, mode, cancellationToken);

    ValueTask IDialogHost<TEvent>.BroadcastAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken)
        => BroadcastAsync(@event, mode, cancellationToken);

    ValueTask IDialogHost<TEvent>.PushAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
        => PushAsync(module, cancellationToken);

    ValueTask IDialogHost<TEvent>.ReplaceTopAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
        => ReplaceTopAsync(module, cancellationToken);

    ValueTask IDialogHost<TEvent>.PopAsync(CancellationToken cancellationToken)
        => PopAsync(cancellationToken);

    ValueTask IDialogHost<TEvent>.PopAsync(TEvent resumeEvent, DispatchMode resumeMode, CancellationToken cancellationToken)
        => PopAsync(resumeEvent, resumeMode, cancellationToken);

    ValueTask IAsyncDisposable.DisposeAsync() => DisposeAsync();
}
