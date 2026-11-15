using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Erlaubt Aufrufe für Aufnahmeende und Abschlusstranskript direkt am gebündelten Signalobjekt. Die Ausführung bleibt beim explizit implementierten Abschlusssignal.
/// </summary>
internal static class ClientMessageBusSpeechInteractionSignalsStoppedExtensions
{
    public static ValueTask PublishStoppedAsync(
        this ClientMessageBusSpeechInteractionSignals signals,
        string languageCode,
        string? finalTranscript)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechCaptureStoppedSignals)signals).PublishStoppedAsync(languageCode, finalTranscript);
    }

    public static IDisposable SubscribeToStopped(
        this ClientMessageBusSpeechInteractionSignals signals,
        Func<SpeechCaptureStopped, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechCaptureStoppedSignals)signals).SubscribeToStopped(handler);
    }
}
