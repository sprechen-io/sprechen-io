using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Versucht die lokale Segmentquelle beim Verlassen der Hörschleife zu beenden. Fehler beim
/// Aufräumen werden abgefangen und nicht an den Aufrufer weitergegeben.
/// </summary>
internal sealed class WakeListeningTeardown
{
    internal async ValueTask StopAsync(IContinuousSegmentStreamSource segmentStreamSource)
    {
        ArgumentNullException.ThrowIfNull(segmentStreamSource);

        try
        {
            await segmentStreamSource
                .StopListeningAsync(WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
        }
    }
}
