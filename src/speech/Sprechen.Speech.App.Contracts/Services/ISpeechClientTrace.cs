using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Erfasst Diagnosen des Sprachclients getrennt vom fachlichen Ablauf.
/// </summary>
public interface ISpeechClientTrace
{
    /// <summary>
    /// Schreibt ein Diagnoseereignis. Implementierungen isolieren Fehler der Aufzeichnung,
    /// prüfen aber das Abbruchtoken vor und nach dem Schreiben und geben einen
    /// Abbruch durch den Aufrufer weiter.
    /// </summary>
    ValueTask WriteAsync(
        string scope,
        LogLevel level,
        string? correlationId,
        string message,
        CancellationToken cancellationToken);
}
