namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Entkoppelt Nachrichtensender von ihren Empfängern über typisierte Abonnements, deren Lebensdauer der Aufrufer durch Freigabe begrenzt.
/// </summary>
public interface IClientMessageBus
{
    ValueTask PublishAsync<TMessage>(TMessage message) where TMessage : notnull;

    IDisposable Subscribe<TMessage>(Func<TMessage, ValueTask> handler) where TMessage : notnull;
}

