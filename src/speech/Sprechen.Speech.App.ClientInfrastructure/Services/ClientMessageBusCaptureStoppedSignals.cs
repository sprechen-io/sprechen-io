using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Meldet den Abschluss einer Aufnahme an nachgelagerte Verbraucher und übergibt Sprachcode und optionales Abschlusstranskript.
/// </summary>
internal sealed class ClientMessageBusCaptureStoppedSignals(IClientMessageBus messageBus) : ISpeechCaptureStoppedSignals
{
    private readonly IClientMessageBus _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));

    public ValueTask PublishStoppedAsync(string languageCode, string? finalTranscript)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        return _messageBus.PublishAsync(new SpeechCaptureStopped(languageCode.Trim(), finalTranscript));
    }

    public IDisposable SubscribeToStopped(Func<SpeechCaptureStopped, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _messageBus.Subscribe(handler);
    }
}
