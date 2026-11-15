namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Überträgt Erfolg und Fehlerdetails einer Browser-Wiedergabeoperation,
/// damit ein abgeschlossener JavaScript-Aufruf nicht allein als Wiedergabeerfolg gilt.
/// </summary>
internal sealed record PcmAudioPlayerInvocationResult(
    bool Succeeded,
    string? ErrorCode,
    string? DiagnosticDetail);
