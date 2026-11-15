namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Erfasst Diagnosen des Sprachassistenten getrennt vom fachlichen Ablauf.
/// </summary>
public interface ISpeechAssistantTrace
{
    /// <summary>
    /// Schreibt ein Diagnoseereignis. Implementierungen isolieren Fehler der Aufzeichnung,
    /// prüfen aber das Abbruchtoken vor und nach dem Schreiben und geben einen
    /// Abbruch durch den Aufrufer weiter.
    /// </summary>
    ValueTask WriteAsync(
        string correlationId,
        SpeechAssistantTraceScope scope,
        SpeechAssistantTraceLevel level,
        string message,
        CancellationToken cancellationToken);
}
