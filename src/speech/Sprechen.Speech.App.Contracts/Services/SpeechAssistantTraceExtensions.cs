namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Schreibt nur bei vorhandener Senke, Korrelationskennung und Nachricht.
/// Fehler der Aufzeichnung werden abgefangen. Ein Abbruch durch den Aufrufer wird weitergegeben.
/// </summary>
public static class SpeechAssistantTraceExtensions
{
    public static async ValueTask WriteIfConfiguredAsync(
        this ISpeechAssistantTrace? trace,
        string? correlationId,
        SpeechAssistantTraceScope scope,
        SpeechAssistantTraceLevel level,
        string message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (trace is null || string.IsNullOrWhiteSpace(correlationId) || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            await trace
                .WriteAsync(
                    correlationId.Trim(),
                    scope,
                    level,
                    message.Trim(),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Ein Diagnosefehler darf den fachlichen Ablauf nicht unterbrechen.
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
