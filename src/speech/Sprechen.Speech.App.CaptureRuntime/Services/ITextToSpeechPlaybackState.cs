namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Meldet laufende und gerade beendete Sprachausgaben,
/// damit die Aufnahme den hörbaren Nachlauf des Assistenten berücksichtigen kann.
/// </summary>
public interface ITextToSpeechPlaybackState
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

    void NotifyPlaybackStarted();

    void NotifyPlaybackCompleted();
}

