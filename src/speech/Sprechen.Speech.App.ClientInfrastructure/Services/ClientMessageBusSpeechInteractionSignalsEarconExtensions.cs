using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Erlaubt Aufrufe für Hinweistonfamilien direkt am gebündelten Signalobjekt. Die Ausführung bleibt beim explizit implementierten Hinweistonsignal.
/// </summary>
internal static class ClientMessageBusSpeechInteractionSignalsEarconExtensions
{
    public static ValueTask SetActiveFamilyAsync(
        this ClientMessageBusSpeechInteractionSignals signals,
        EarconFamily? activeFamily)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((IEarconFamilySignals)signals).SetActiveFamilyAsync(activeFamily);
    }

    public static IDisposable SubscribeToActiveFamilyChanged(
        this ClientMessageBusSpeechInteractionSignals signals,
        Func<EarconFamilyChanged, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((IEarconFamilySignals)signals).SubscribeToActiveFamilyChanged(handler);
    }
}
