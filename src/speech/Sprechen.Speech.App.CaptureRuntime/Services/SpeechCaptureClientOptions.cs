namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Einstellbare clientseitige Zeitgrenzen für Wiedergabeschutz, lokale Sitzung,
/// Herunterfahren und ausbleibende Audioblöcke. Die Werte werden über Options injiziert.
/// Modellierte Erfassungsprofile werden getrennt aus dem Katalog geladen.
/// </summary>
public sealed class SpeechCaptureClientOptions
{
    public int PlaybackLeakageCooldownMs { get; init; } = 750;
    public TimeSpan LocalSessionIdleTimeout { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan ShutdownTimeout { get; init; } = TimeSpan.FromSeconds(5);

    // Zeitgrenze für ausbleibende Audio-Chunks. Stille liefert weiterhin Chunks.
    // Fehlende Daten müssen daher unabhängig von der Stilleerkennung beendet werden.
    // Clientseitiger Standardwert zur Erkennung ausbleibender Daten, keine Zeitvorgabe des modellierten Profils.
    public TimeSpan ChunkArrivalLivenessTimeout { get; init; } = TimeSpan.FromSeconds(3);
    // Kürzere Sprachstart-Schutzfrist nach Wiedergabe für ShortConfirmation,
    // damit von kurzen Antworten weniger Audio verworfen wird. Die Dauer zur
    // Bestätigung eines Sprachbeginns bleibt davon unabhängig.
    public TimeSpan ShortConfirmationRecentPlaybackSpeechStartGuard { get; init; } = TimeSpan.FromMilliseconds(60);
}
