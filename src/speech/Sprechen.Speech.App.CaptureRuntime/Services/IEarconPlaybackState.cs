namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Meldet laufende und gerade beendete Earcon-Wiedergaben,
/// damit die Aufnahme diese von neu beginnender Sprache unterscheiden kann.
/// </summary>
public interface IEarconPlaybackState
{
    /// <summary>
    /// Wird nach der Aktualisierung des Wiedergabezählers in
    /// <c>NotifyPlaybackStarted</c> oder <c>NotifyPlaybackCompleted</c> außerhalb
    /// der internen Sperre ausgelöst. Empfänger lesen <see cref="IsPlaying"/>
    /// erneut, um den aktuellen Zustand abzufragen.
    /// </summary>
    event Action? PlaybackStateChanged;

    bool IsPlaying { get; }

    bool IsPlayingOrRecentlyCompleted(TimeSpan cooldown);

    Task WaitForIdleAsync(CancellationToken cancellationToken);

    void NotifyPlaybackStarted();

    void NotifyPlaybackCompleted();
}




