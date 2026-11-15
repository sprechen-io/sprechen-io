using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Verbindet voneinander unabhängige Clientdienste über Nachrichtentypen. Jeder Themenkanal besitzt seine eigenen Empfänger.
/// </summary>
public sealed class ClientMessageBus(ILogger<ClientMessageBus> logger) : IClientMessageBus
{
    private readonly ILogger<ClientMessageBus> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ConcurrentDictionary<Type, ClientMessageBusTopic> _topics = new();

    public ValueTask PublishAsync<TMessage>(TMessage message) where TMessage : notnull
    {
        if (!_topics.TryGetValue(typeof(TMessage), out var topic))
        {
            return ValueTask.CompletedTask;
        }

        return topic.PublishAsync(message, _logger);
    }

    public IDisposable Subscribe<TMessage>(Func<TMessage, ValueTask> handler) where TMessage : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);

        Func<object, ValueTask> wrapper = message => handler((TMessage)message);
        var topic = _topics.GetOrAdd(typeof(TMessage), static _ => new ClientMessageBusTopic());
        return topic.Subscribe(wrapper);
    }
}

