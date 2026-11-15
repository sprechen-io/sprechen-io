namespace Sprechen.Speech.State;

/// <summary>
/// Markiert asynchrone Modul-Lebenszyklusaufrufe und weist erneute
/// Dialoghost-Operationen innerhalb dieses Übergangsbereichs zurück.
/// </summary>
internal sealed class DialogHostTransitionScope
{
    private readonly AsyncLocal<int> _depth = new();

    internal async ValueTask RunAsync(Func<ValueTask> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        Enter();
        try
        {
            await callback().ConfigureAwait(false);
        }
        finally
        {
            Exit();
        }
    }

    internal void ThrowIfReentrant()
    {
        if (_depth.Value > 0)
        {
            throw new InvalidOperationException("Dialog host operations must not be called from flow activation/deactivation callbacks.");
        }
    }

    private void Enter() => _depth.Value++;

    private void Exit() => _depth.Value--;
}
