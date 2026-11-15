namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Erlaubt Aufrufe für Nutzereinwürfe direkt am gebündelten Signalobjekt. Die Ausführung bleibt beim explizit implementierten Einwurfssignal.
/// </summary>
internal static class ClientMessageBusSpeechInteractionSignalsBargeInExtensions
{
    public static ValueTask PublishBargeInAsync(this ClientMessageBusSpeechInteractionSignals signals)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechBargeInSignals)signals).PublishBargeInAsync();
    }

    public static IDisposable SubscribeToBargeIn(
        this ClientMessageBusSpeechInteractionSignals signals,
        Func<ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechBargeInSignals)signals).SubscribeToBargeIn(handler);
    }
}
