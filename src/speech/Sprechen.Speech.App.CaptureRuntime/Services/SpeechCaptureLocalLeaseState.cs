namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Zählt lokale Sitzungsnutzungen innerhalb einer Generation,
/// damit verspätete Freigaben alter Nutzer keine neue Sitzung als unbenutzt markieren.
/// </summary>
internal sealed class SpeechCaptureLocalLeaseState
{
    private int _activeLeaseCount;
    private long _generation;

    internal bool IsIdle => _activeLeaseCount == 0;

    internal long Acquire()
    {
        _activeLeaseCount++;
        return _generation;
    }

    internal void Clear()
    {
        _generation++;
        _activeLeaseCount = 0;
    }

    internal bool ReleaseAndIsIdle(long generation)
    {
        if (generation != _generation || _activeLeaseCount == 0)
        {
            return false;
        }

        _activeLeaseCount--;
        return _activeLeaseCount == 0;
    }
}
