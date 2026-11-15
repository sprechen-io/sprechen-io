namespace Sprechen.Speech.State;

/// <summary>
/// Gibt die aufgenommenen Dialogmodule in umgekehrter Stapelreihenfolge frei
/// und versucht zuvor die Deaktivierung des vormals aktiven Moduls.
/// </summary>
internal sealed class DialogHostTeardown<TEvent> where TEvent : notnull
{
    private readonly DialogHostModuleLifecycle<TEvent> _lifecycle;

    internal DialogHostTeardown(DialogHostModuleLifecycle<TEvent> lifecycle)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
    }

    internal async ValueTask DisposeAsync(
        DialogHostTeardownSnapshot<TEvent> snapshot,
        CancellationToken cancellationToken)
    {
        foreach (var module in snapshot.Modules.Reverse())
        {
            if (ReferenceEquals(module, snapshot.ActiveModule))
            {
                await _lifecycle.BestEffortDeactivateAsync(module, cancellationToken).ConfigureAwait(false);
            }

            await _lifecycle.BestEffortDisposeAsync(module).ConfigureAwait(false);
        }
    }
}
