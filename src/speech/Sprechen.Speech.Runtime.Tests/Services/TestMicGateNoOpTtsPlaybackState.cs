namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class TestMicGateNoOpTtsPlaybackState : ITextToSpeechPlaybackState
{
    public event Action? PlaybackStateChanged
    {
        add { }
        remove { }
    }

    public bool IsPlaying => false;

    public bool IsPlayingOrRecentlyCompleted(TimeSpan cooldown) => false;

    public void NotifyPlaybackStarted() { }

    public void NotifyPlaybackCompleted() { }
}
