namespace Sprechen.Speech.State;

/// <summary>
/// Verbindet Stapeländerungen mit Aktivierung und Deaktivierung der Dialogmodule
/// und versucht bei fehlgeschlagener Aktivierung den vorherigen Zustand wiederherzustellen.
/// </summary>
internal sealed class DialogHostStackTransitions<TEvent> where TEvent : notnull
{
    private readonly DialogHostStack<TEvent> _stack;
    private readonly DialogHostModuleLifecycle<TEvent> _lifecycle;

    internal DialogHostStackTransitions(DialogHostStack<TEvent> stack, DialogHostModuleLifecycle<TEvent> lifecycle)
    {
        _stack = stack ?? throw new ArgumentNullException(nameof(stack));
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
    }

    internal async ValueTask StartFirstModuleAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
    {
        _stack.Add(module);

        try
        {
            await _lifecycle.ActivateAsync(module, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _stack.RemoveTop();

            await _lifecycle.BestEffortDisposeAsync(module).ConfigureAwait(false);

            throw;
        }
    }

    internal async ValueTask PushAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
    {
        if (_stack.IsEmpty)
        {
            await StartFirstModuleAsync(module, cancellationToken).ConfigureAwait(false);
            return;
        }

        var previousActive = _stack.ActiveOrThrow();

        await _lifecycle.DeactivateAsync(previousActive, cancellationToken).ConfigureAwait(false);

        _stack.Add(module);

        try
        {
            await _lifecycle.ActivateAsync(module, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _stack.RemoveTop();

            await _lifecycle.BestEffortActivateAsync(previousActive, cancellationToken).ConfigureAwait(false);
            await _lifecycle.BestEffortDisposeAsync(module).ConfigureAwait(false);

            throw;
        }
    }

    internal async ValueTask ReplaceTopAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
    {
        if (_stack.IsEmpty)
        {
            await StartFirstModuleAsync(module, cancellationToken).ConfigureAwait(false);
            return;
        }

        var previousActive = _stack.ActiveOrThrow();

        await _lifecycle.DeactivateAsync(previousActive, cancellationToken).ConfigureAwait(false);

        _stack.ReplaceTop(module);

        try
        {
            await _lifecycle.ActivateAsync(module, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _stack.ReplaceTop(previousActive);

            await _lifecycle.BestEffortActivateAsync(previousActive, cancellationToken).ConfigureAwait(false);
            await _lifecycle.BestEffortDisposeAsync(module).ConfigureAwait(false);

            throw;
        }

        await _lifecycle.BestEffortDisposeAsync(previousActive).ConfigureAwait(false);
    }

    internal async ValueTask<IDialogFlow<TEvent>?> PopAsync(CancellationToken cancellationToken)
    {
        if (_stack.IsEmpty)
        {
            return null;
        }

        var removedModule = _stack.ActiveOrThrow();
        var newActive = _stack.PreviousActiveOrDefault();

        await _lifecycle.DeactivateAsync(removedModule, cancellationToken).ConfigureAwait(false);

        _stack.RemoveTop();

        try
        {
            if (newActive is not null)
            {
                await _lifecycle.ActivateAsync(newActive, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            _stack.Add(removedModule);

            await _lifecycle.BestEffortActivateAsync(removedModule, cancellationToken).ConfigureAwait(false);

            throw;
        }

        await _lifecycle.BestEffortDisposeAsync(removedModule).ConfigureAwait(false);
        return newActive;
    }
}
