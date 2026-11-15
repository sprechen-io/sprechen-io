namespace Sprechen.Speech.State;

/// <summary>
/// Serialisiert Änderungen am Dialogstapel und koordiniert Lebenszyklus,
/// Ereigniszustellung und Abbau. Erneute Hostaufrufe aus Lebenszykluscallbacks
/// werden zurückgewiesen.
/// </summary>
internal sealed class DialogHostSession<TEvent> where TEvent : notnull
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly DialogHostTransitionScope _transitionScope = new();
    private readonly DialogHostStack<TEvent> _stack = new();
    private readonly DialogHostModuleLifecycle<TEvent> _lifecycle;
    private readonly DialogHostStackTransitions<TEvent> _stackTransitions;
    private readonly DialogHostEventDispatch<TEvent> _eventDispatch = new();
    private readonly DialogHostTeardown<TEvent> _teardown;

    private bool _disposed;

    internal DialogHostSession()
    {
        _lifecycle = new DialogHostModuleLifecycle<TEvent>(_transitionScope);
        _stackTransitions = new DialogHostStackTransitions<TEvent>(_stack, _lifecycle);
        _teardown = new DialogHostTeardown<TEvent>(_lifecycle);
    }

    internal int Depth => _stack.Depth;

    internal async ValueTask StartAsync(IDialogFlow<TEvent> root, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(root);
        _transitionScope.ThrowIfReentrant();

        await RunInGateAsync(
            async () =>
            {
                ThrowIfDisposed();

                if (!_stack.IsEmpty)
                {
                    throw new InvalidOperationException("Dialog host has already been started.");
                }

                await _stackTransitions.StartFirstModuleAsync(root, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    internal async ValueTask DispatchToActiveAsync(
        TEvent @event,
        DispatchMode mode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _transitionScope.ThrowIfReentrant();

        var active = await RunInGateAsync(
            () =>
            {
                ThrowIfDisposed();
                return _stack.ActiveOrThrow();
            },
            cancellationToken).ConfigureAwait(false);

        await _eventDispatch.DispatchToActiveAsync(active, @event, mode, cancellationToken).ConfigureAwait(false);
    }

    internal async ValueTask BroadcastAsync(
        TEvent @event,
        DispatchMode mode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _transitionScope.ThrowIfReentrant();

        var snapshot = await RunInGateAsync(
            () =>
            {
                ThrowIfDisposed();
                return _stack.Snapshot();
            },
            cancellationToken).ConfigureAwait(false);

        await _eventDispatch.BroadcastAsync(
            snapshot,
            @event,
            mode,
            cancellationToken,
            IsStillInStackAsync).ConfigureAwait(false);
    }

    internal async ValueTask PushAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(module);
        _transitionScope.ThrowIfReentrant();

        await RunInGateAsync(
            async () =>
            {
                ThrowIfDisposed();
                await _stackTransitions.PushAsync(module, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    internal async ValueTask ReplaceTopAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(module);
        _transitionScope.ThrowIfReentrant();

        await RunInGateAsync(
            async () =>
            {
                ThrowIfDisposed();
                await _stackTransitions.ReplaceTopAsync(module, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    internal async ValueTask PopAsync(
        bool hasResumeEvent,
        TEvent resumeEvent,
        DispatchMode resumeMode,
        CancellationToken cancellationToken)
    {
        if (hasResumeEvent)
        {
            ArgumentNullException.ThrowIfNull(resumeEvent);
        }

        _transitionScope.ThrowIfReentrant();

        var newActive = await RunInGateAsync(
            async () =>
            {
                ThrowIfDisposed();
                return await _stackTransitions.PopAsync(cancellationToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        if (hasResumeEvent && newActive is not null)
        {
            await _eventDispatch.DispatchToActiveAsync(newActive, resumeEvent, resumeMode, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    internal async ValueTask DisposeAsync()
    {
        _transitionScope.ThrowIfReentrant();

        var snapshot = await RunInGateAsync(
            () =>
            {
                if (_disposed)
                {
                    return DialogHostTeardownSnapshot<TEvent>.Empty;
                }

                _disposed = true;
                var modules = _stack.Snapshot();
                var activeModule = _stack.ActiveOrDefault();
                _stack.Clear();
                return new DialogHostTeardownSnapshot<TEvent>(modules, activeModule);
            },
            CancellationToken.None).ConfigureAwait(false);

        await _teardown.DisposeAsync(snapshot, CancellationToken.None).ConfigureAwait(false);
    }

    private ValueTask<bool> IsStillInStackAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
    {
        return RunInGateAsync(
            () =>
            {
                ThrowIfDisposed();
                return _stack.ContainsReference(module);
            },
            cancellationToken);
    }

    private async ValueTask RunInGateAsync(Func<ValueTask> operation, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await operation().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async ValueTask<TResult> RunInGateAsync<TResult>(
        Func<ValueTask<TResult>> operation,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await operation().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async ValueTask<TResult> RunInGateAsync<TResult>(
        Func<TResult> operation,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return operation();
        }
        finally
        {
            _gate.Release();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DialogHost<TEvent>));
        }
    }
}
