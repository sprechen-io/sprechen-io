using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Erlaubt Aufrufe für Aufnahmeansprüche und Aufnahmestopp direkt am gebündelten Signalobjekt. Die Ausführung bleibt beim explizit implementierten Aufnahmeindikator.
/// </summary>
internal static class ClientMessageBusSpeechInteractionSignalsRecordingExtensions
{
    public static ValueTask<IAsyncDisposable> BeginRecordingAsync(
        this ClientMessageBusSpeechInteractionSignals signals)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechCaptureRecordingIndicator)signals).BeginRecordingAsync();
    }

    public static ValueTask EnsureStoppedAsync(this ClientMessageBusSpeechInteractionSignals signals)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechCaptureRecordingIndicator)signals).EnsureStoppedAsync();
    }

    public static IDisposable SubscribeToRecordingStateChanged(
        this ClientMessageBusSpeechInteractionSignals signals,
        Func<SpeechAssistantRecordingStateChanged, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechCaptureRecordingIndicator)signals).SubscribeToRecordingStateChanged(handler);
    }
}
