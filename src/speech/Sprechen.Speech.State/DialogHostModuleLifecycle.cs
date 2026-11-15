namespace Sprechen.Speech.State;

/// <summary>
/// Führt Aktivierung, Deaktivierung und Freigabe im Schutz vor erneuten Hostaufrufen aus.
/// Die BestEffort-Varianten unterdrücken Ausnahmen bei Wiederherstellung und Abbau,
/// ohne sie zu protokollieren.
/// </summary>
internal sealed class DialogHostModuleLifecycle<TEvent> where TEvent : notnull
{
    private readonly DialogHostTransitionScope _transitionScope;

    internal DialogHostModuleLifecycle(DialogHostTransitionScope transitionScope)
    {
        _transitionScope = transitionScope ?? throw new ArgumentNullException(nameof(transitionScope));
    }

    internal ValueTask ActivateAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(module);
        return _transitionScope.RunAsync(() => module.OnActivatedAsync(cancellationToken));
    }

    internal ValueTask DeactivateAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(module);
        return _transitionScope.RunAsync(() => module.OnDeactivatedAsync(cancellationToken));
    }

    internal ValueTask DisposeAsync(IDialogFlow<TEvent> module)
    {
        ArgumentNullException.ThrowIfNull(module);
        return _transitionScope.RunAsync(module.DisposeAsync);
    }

    internal async ValueTask BestEffortActivateAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
    {
        try
        {
            await ActivateAsync(module, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ein Fehler beim Wiederherstellen ersetzt den ursprünglichen Übergangsfehler nicht.
        }
    }

    internal async ValueTask BestEffortDeactivateAsync(IDialogFlow<TEvent> module, CancellationToken cancellationToken)
    {
        try
        {
            await DeactivateAsync(module, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ein Fehler beim Abbauen verhindert die weiteren Aufräumschritte nicht.
        }
    }

    internal async ValueTask BestEffortDisposeAsync(IDialogFlow<TEvent> module)
    {
        try
        {
            await DisposeAsync(module).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ein Fehler beim Abbauen verhindert die weiteren Aufräumschritte nicht.
        }
    }
}
