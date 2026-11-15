namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Verdichtet Detektorwerte zu einer Aktivierung mit Übergabeposition, damit die Laufzeit keine eigene Schwellenentscheidung benötigt.
/// </summary>
public interface IAcousticWakeWordTriggerPolicy
{
    ValueTask<AcousticWakeWordEvidence> DetectAsync(
        AcousticWakeWordDetectorConfiguration configuration,
        IAsyncEnumerable<AcousticWakeWordScoreFrame> scoreFrames,
        CancellationToken cancellationToken);
}
