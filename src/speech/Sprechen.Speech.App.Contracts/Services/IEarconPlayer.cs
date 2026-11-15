namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Spielt Hinweistöne und Hörproben ab. <see cref="StopLoopingAsync"/> beendet
/// nur die laufende Schleife. <see cref="StopAllAsync"/> beendet sämtliche Wiedergabe.
/// </summary>
public interface IEarconPlayer
{
    ValueTask WarmupAsync(CancellationToken cancellationToken);

    ValueTask PlayAsync(EarconId earcon, CancellationToken cancellationToken);

    ValueTask PreviewAsync(EarconId earcon, CancellationToken cancellationToken);

    ValueTask StopAllAsync(CancellationToken cancellationToken);

    // Beendet den FlowProcessing-Schleifenton und lässt einmalige Töne ausklingen,
    // etwa beim Übergang zur Sprachausgabe. StopAllAsync ist für Fälle vorgesehen,
    // in denen auch die übrigen Töne unterbrochen werden sollen.
    ValueTask StopLoopingAsync(CancellationToken cancellationToken);
}

