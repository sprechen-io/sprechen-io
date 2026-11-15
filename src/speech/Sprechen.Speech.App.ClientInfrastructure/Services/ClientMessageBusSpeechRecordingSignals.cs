using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Zählt überlappende Aufnahmeansprüche und veröffentlicht Wechsel zwischen
/// aktiver und inaktiver Aufnahme. Ein erzwungener Stopp setzt den Zähler zurück.
/// </summary>
internal sealed class ClientMessageBusSpeechRecordingSignals(IClientMessageBus messageBus) : ISpeechCaptureRecordingIndicator
{
    private readonly IClientMessageBus _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
    private readonly object _gate = new();
    private int _recordingDepth;

    public bool IsRecording
    {
        get
        {
            lock (_gate)
            {
                return _recordingDepth > 0;
            }
        }
    }

    public async ValueTask<IAsyncDisposable> BeginRecordingAsync()
    {
        var publishStarted = false;

        lock (_gate)
        {
            checked
            {
                _recordingDepth++;
            }

            publishStarted = _recordingDepth == 1;
        }

        if (publishStarted)
        {
            await _messageBus.PublishAsync(new SpeechAssistantRecordingStateChanged(true)).ConfigureAwait(false);
        }

        return new ClientMessageBusSpeechInteractionSignalsRecordingLease(this);
    }

    public ValueTask EnsureStoppedAsync()
    {
        lock (_gate)
        {
            if (_recordingDepth == 0)
            {
                return ValueTask.CompletedTask;
            }

            _recordingDepth = 0;
        }

        return _messageBus.PublishAsync(new SpeechAssistantRecordingStateChanged(false));
    }

    public IDisposable SubscribeToRecordingStateChanged(Func<SpeechAssistantRecordingStateChanged, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _messageBus.Subscribe(handler);
    }

    internal ValueTask ReleaseRecordingAsync()
    {
        var publishStopped = false;

        lock (_gate)
        {
            if (_recordingDepth == 0)
            {
                return ValueTask.CompletedTask;
            }

            _recordingDepth--;
            publishStopped = _recordingDepth == 0;
        }

        return publishStopped
            ? _messageBus.PublishAsync(new SpeechAssistantRecordingStateChanged(false))
            : ValueTask.CompletedTask;
    }
}
