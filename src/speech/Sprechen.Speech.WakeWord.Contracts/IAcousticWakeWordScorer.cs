namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Liefert Detektorwerte mit Position im Audiostrom und Beobachtungszeit,
/// damit die Auslöseentscheidung vom akustischen Bewertungsverfahren unabhängig bleibt.
/// </summary>
public interface IAcousticWakeWordScorer
{
    IAsyncEnumerable<AcousticWakeWordScoreFrame> ScoreAsync(
        AcousticWakeWordDetectorConfiguration configuration,
        CancellationToken cancellationToken);
}
