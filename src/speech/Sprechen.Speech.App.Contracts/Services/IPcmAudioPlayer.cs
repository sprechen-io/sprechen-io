namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Spielt PCM-Audio ab. Das Vorbereiten der Ausgabe vor der ersten Ansage
/// soll Verzögerungen beim Anlaufen des Audiogeräts verringern.
/// </summary>
public interface IPcmAudioPlayer
{
    /// <summary>
    /// Bereitet den Wiedergabe-AudioContext und den Keep-alive-Graph bei einer Nutzergeste vor.
    /// Wiederholte Aufrufe sind zulässig. Bei aktiviertem Keep-alive werden Vorbereitungsfehler
    /// weitergegeben, damit der Aufrufer einen erneuten Versuch veranlassen kann.
    /// </summary>
    ValueTask WarmupAsync(CancellationToken cancellationToken);

    ValueTask DrainAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Setzt den Lautstärkefaktor einer TTS-Äußerung vor Look-ahead-Limiter und Nutzerlautstärke.
    /// Nichtendliche oder nichtpositive Werte werden als Faktor 1 behandelt.
    /// </summary>
    ValueTask SetMakeupAsync(double makeup, CancellationToken cancellationToken);

    ValueTask EnqueuePcm16LeAsync(byte[] pcm16Le, CancellationToken cancellationToken);

    Task InitializeAsync(int sampleRate, CancellationToken cancellationToken);

    ValueTask ResetAsync(CancellationToken cancellationToken);
}

