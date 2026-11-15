using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Verbindet die Erkennung eines Nutzereinwurfs mit ihren Reaktionen über den Nachrichtenbus. Die Reaktion bleibt beim Empfänger.
/// </summary>
internal sealed class ClientMessageBusBargeInSignals(IClientMessageBus messageBus) : ISpeechBargeInSignals
{
    private readonly IClientMessageBus _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));

    public ValueTask PublishBargeInAsync()
    {
        return _messageBus.PublishAsync(new SpeechBargeInTriggered());
    }

    public IDisposable SubscribeToBargeIn(Func<ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _messageBus.Subscribe<SpeechBargeInTriggered>(_ => handler());
    }
}
