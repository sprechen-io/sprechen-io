using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Erlaubt Aufrufe für den lexikalischen Ersatzpfad für Aktivierungswörter direkt am gebündelten Signalobjekt. Die Ausführung bleibt beim explizit implementierten Aktivierungssignal.
/// </summary>
internal static class ClientMessageBusSpeechInteractionSignalsWakeFallbackExtensions
{
    public static ValueTask SetWakeListeningFallbackStateAsync(
        this ClientMessageBusSpeechInteractionSignals signals,
        SpeechWakeListeningFallbackState state)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechWakeListeningFallbackSignals)signals).SetWakeListeningFallbackStateAsync(state);
    }

    public static IDisposable SubscribeToWakeListeningFallbackStateChanged(
        this ClientMessageBusSpeechInteractionSignals signals,
        Func<SpeechWakeListeningFallbackStateChanged, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechWakeListeningFallbackSignals)signals).SubscribeToWakeListeningFallbackStateChanged(handler);
    }
}
