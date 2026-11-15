using System.Diagnostics;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Sammelt die Beobachtungen eines Aufnahmeversuchs und setzt sie zu den passenden
/// Zeitpunkten zurück, damit Versuchsbeginn und Aufnahmebeginn getrennt erfasst werden.
/// </summary>
internal sealed class ActiveCaptureAttemptObservationState
{
    private long _armedTimestamp;
    private int _peakAmplitude;
    private int _estimatedFloor;
    private int _appliedOnsetThreshold;
    private int _observedFrames;
    private bool _startCursorHandedOff;
    private int _lookbackBytes;
    private SpeechCaptureMode _captureMode;
    private SpeechCaptureStartContext _startContext;

    internal SpeechCaptureMode CaptureMode => _captureMode;

    internal SpeechCaptureStartContext StartContext => _startContext;

    /// <summary>Höchste beobachtete Mittelamplitude seit dem Aktivieren der Aufnahme.</summary>
    internal int PeakAmplitude => _peakAmplitude;

    /// <summary>Zuletzt geschätzter Hintergrundpegel für die Erkennung des Sprachbeginns.</summary>
    internal int EstimatedFloor => _estimatedFloor;

    /// <summary>Zuletzt verwendete Schwelle zur Erkennung des Sprachbeginns.</summary>
    internal int AppliedOnsetThreshold => _appliedOnsetThreshold;

    /// <summary>
    /// Seit Aufnahmebeginn vom Segmentierer verarbeitete Frames.
    /// </summary>
    internal int ObservedFrames => _observedFrames;

    /// <summary>
    /// Setzt die Kennzeichen des neuen Versuchs, ohne die Zeitmessung zu starten.
    /// Bei einem Abbruch vor der Aufnahme wird so dessen eigener Modus mit Dauer null gemeldet.
    /// </summary>
    internal void MarkStarted(SpeechCaptureMode captureMode, SpeechCaptureStartContext captureStartContext)
    {
        _captureMode = captureMode;
        _startContext = captureStartContext;
        _armedTimestamp = 0;
    }

    internal void MarkCursorHandoff(bool startCursorHandedOff)
    {
        _startCursorHandedOff = startCursorHandedOff;
        _lookbackBytes = 0;
    }

    /// <summary>
    /// Startet die Zeitmessung und setzt die Beobachtungen der Aufnahme zurück.
    /// Modus und Startkontext bleiben erhalten, da sie seit Versuchsbeginn gelten.
    /// </summary>
    internal void MarkArmed()
    {
        _armedTimestamp = Stopwatch.GetTimestamp();
        _peakAmplitude = 0;
        _estimatedFloor = 0;
        _appliedOnsetThreshold = 0;
        _observedFrames = 0;
    }

    /// <summary>Übernimmt eine höhere beobachtete Mittelamplitude, bevor die Aufnahme freigegeben wird.</summary>
    internal void ObservePeakAmplitude(int peakAmplitude)
    {
        if (peakAmplitude > _peakAmplitude)
        {
            _peakAmplitude = peakAmplitude;
        }
    }

    /// <summary>Übernimmt den zuletzt geschätzten Hintergrundpegel und die verwendete Sprachbeginnschwelle.</summary>
    internal void ObserveOnsetDecision(int estimatedFloor, int appliedOnsetThreshold)
    {
        _estimatedFloor = estimatedFloor;
        _appliedOnsetThreshold = appliedOnsetThreshold;
    }

    internal void ObserveArrivedFrames(int observedFrames)
    {
        if (observedFrames > _observedFrames)
        {
            _observedFrames = observedFrames;
        }
    }

    /// <summary>Vermerkt die vor der Verarbeitung neuer Audiodaten nachgelesenen Ringpufferbytes.</summary>
    internal void ObserveLookbackBytes(int lookbackBytes)
    {
        _lookbackBytes = lookbackBytes;
    }

    internal CaptureWindowObservation ToWindowObservation()
        => new(_observedFrames, _startCursorHandedOff, _lookbackBytes);

    /// <summary>Dauer seit dem Aktivieren der Aufnahme in Millisekunden. Null, wenn sie nicht aktiviert wurde.</summary>
    internal double ResolveArmedToOutcomeMs()
        => _armedTimestamp == 0 ? 0 : Stopwatch.GetElapsedTime(_armedTimestamp).TotalMilliseconds;

    /// <summary>Erkennt digitale Stille an vorhandenen Frames mit Höchstamplitude null.</summary>
    internal string ResolveNoSpeechOutcome()
        => _observedFrames > 0 && _peakAmplitude == 0 ? "digital_silence" : "no_speech";
}
