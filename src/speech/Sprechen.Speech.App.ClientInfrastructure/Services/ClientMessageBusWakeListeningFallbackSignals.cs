using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Hält den Zustand des lexikalischen Ersatzpfads für Aktivierungswörter
/// und veröffentlicht Änderungen samt Begründung über den Clientnachrichtenbus.
/// </summary>
internal sealed class ClientMessageBusWakeListeningFallbackSignals(IClientMessageBus messageBus)
    : ISpeechWakeListeningFallbackSignals
{
    private readonly IClientMessageBus _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
    private readonly object _gate = new();
    private SpeechWakeListeningFallbackState _wakeListeningFallbackState = SpeechWakeListeningFallbackState.Inactive;

    public SpeechWakeListeningFallbackState WakeListeningFallbackState
    {
        get
        {
            lock (_gate)
            {
                return _wakeListeningFallbackState;
            }
        }
    }

    public ValueTask SetWakeListeningFallbackStateAsync(SpeechWakeListeningFallbackState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (_gate)
        {
            if (_wakeListeningFallbackState == state)
            {
                return ValueTask.CompletedTask;
            }

            _wakeListeningFallbackState = state;
        }

        return _messageBus.PublishAsync(
            new SpeechWakeListeningFallbackStateChanged(
                state.IsLexicalFallbackActive,
                state.ReasonCode,
                state.Reason));
    }

    public IDisposable SubscribeToWakeListeningFallbackStateChanged(
        Func<SpeechWakeListeningFallbackStateChanged, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _messageBus.Subscribe(handler);
    }
}
