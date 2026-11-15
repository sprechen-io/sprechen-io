using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Versorgt Rückmeldungsempfänger mit Aktivierungszustand und Aufnahmefehlern, ohne die Aufnahme selbst zu steuern.
/// </summary>
internal sealed class ClientMessageBusSpeechCaptureFeedbackSignals(IClientMessageBus messageBus) : ISpeechCaptureFeedback
{
    private readonly IClientMessageBus _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
    private readonly object _gate = new();
    private SpeechAssistantActivationState _activationState = SpeechAssistantActivationState.NotStarted;

    public SpeechAssistantActivationState ActivationState
    {
        get
        {
            lock (_gate)
            {
                return _activationState;
            }
        }
    }

    public ValueTask SetActivationStateAsync(SpeechAssistantActivationState activationState)
    {
        lock (_gate)
        {
            if (_activationState == activationState)
            {
                return ValueTask.CompletedTask;
            }

            _activationState = activationState;
        }

        return _messageBus.PublishAsync(new SpeechAssistantActivationStateChanged(activationState));
    }

    public ValueTask ReportFailureAsync(string message)
        => ReportFailureAsync(SpeechErrorCode.Unspecified, message);

    public ValueTask ReportFailureAsync(SpeechErrorCode errorCode, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return _messageBus.PublishAsync(new SpeechCaptureFailed(errorCode, message));
    }

    public IDisposable SubscribeToActivationStateChanged(Func<SpeechAssistantActivationStateChanged, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _messageBus.Subscribe(handler);
    }

    public IDisposable SubscribeToFailures(Func<SpeechCaptureFailed, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _messageBus.Subscribe(handler);
    }
}
