using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Stellt Beobachtern die aktive Hinweistonfamilie bereit, ohne ihnen Zugriff auf den Player zu geben. Zustand und Änderungen laufen über diesen Signalzugang.
/// </summary>
internal sealed class ClientMessageBusEarconFamilySignals(IClientMessageBus messageBus) : IEarconFamilySignals
{
    private readonly IClientMessageBus _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
    private readonly object _gate = new();
    private EarconFamily? _activeFamily;

    public EarconFamily? ActiveFamily
    {
        get
        {
            lock (_gate)
            {
                return _activeFamily;
            }
        }
    }

    public ValueTask SetActiveFamilyAsync(EarconFamily? activeFamily)
    {
        lock (_gate)
        {
            if (_activeFamily == activeFamily)
            {
                return ValueTask.CompletedTask;
            }

            _activeFamily = activeFamily;
        }

        return _messageBus.PublishAsync(new EarconFamilyChanged(activeFamily));
    }

    public IDisposable SubscribeToActiveFamilyChanged(Func<EarconFamilyChanged, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _messageBus.Subscribe(handler);
    }
}
