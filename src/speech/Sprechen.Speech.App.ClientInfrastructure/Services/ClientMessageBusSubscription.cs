namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Bindet die Lebensdauer eines Nachrichtenhandlers an ein freigebbares Abonnement. Wiederholte Freigabe meldet ihn nicht erneut ab.
/// </summary>
internal sealed class ClientMessageBusSubscription(ClientMessageBusTopic owner, Func<object, ValueTask> handler) : IDisposable
{
    private readonly ClientMessageBusTopic _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    private readonly Func<object, ValueTask> _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _owner.Unsubscribe(_handler);
    }
}

