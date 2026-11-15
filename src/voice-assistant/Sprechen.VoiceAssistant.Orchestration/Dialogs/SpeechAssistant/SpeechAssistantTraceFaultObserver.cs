using Microsoft.Extensions.Logging;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Beobachtet asynchrone Hintergrundarbeit des Assistenten und protokolliert gewöhnliche Fehler, damit
/// deren Ergebnis nicht unbeachtet bleibt. Bereits erfolgreich abgeschlossene Vorgänge benötigen keine
/// weitere Beobachtung. Abbruchmeldungen werden ohne Fehlerprotokollierung aufgenommen.
/// </summary>
internal static class SpeechAssistantTraceFaultObserver
{
    public static void Observe(ValueTask operation, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (operation.IsCompletedSuccessfully)
        {
            return;
        }

        _ = ObserveAsync(operation, logger);

        static async Task ObserveAsync(ValueTask pending, ILogger logger)
        {
            try
            {
                await pending.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Einen Abbruch dieser Hintergrundarbeit ohne zusätzliche Fehlermeldung beobachten.
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Speech assistant background operation failed.");
            }
        }
    }
}
