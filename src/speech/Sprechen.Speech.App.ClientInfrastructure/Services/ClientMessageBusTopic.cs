using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Verwaltet die Abonnenten eines Nachrichtentyps und ruft bei Veröffentlichung
/// eine Momentaufnahme ihrer Handler nacheinander auf. Handlerfehler werden protokolliert.
/// </summary>
internal sealed class ClientMessageBusTopic
{
    private readonly object _gate = new();
    private readonly List<Func<object, ValueTask>> _handlers = new();

    public IDisposable Subscribe(Func<object, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_gate)
        {
            _handlers.Add(handler);
        }

        return new ClientMessageBusSubscription(this, handler);
    }

    public async ValueTask PublishAsync<TMessage>(TMessage message, ILogger logger) where TMessage : notnull
    {
        ArgumentNullException.ThrowIfNull(logger);

        Func<object, ValueTask>[] snapshot;
        lock (_gate)
        {
            snapshot = _handlers.ToArray();
        }

        foreach (var handler in snapshot)
        {
            try
            {
                await handler(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "ClientMessageBus subscriber failed for {MessageType}.", typeof(TMessage).Name);
            }
        }
    }

    internal void Unsubscribe(Func<object, ValueTask> handler)
    {
        lock (_gate)
        {
            _handlers.Remove(handler);
        }
    }
}

