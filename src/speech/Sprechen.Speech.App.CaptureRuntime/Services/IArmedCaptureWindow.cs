namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Stellt die Messwerte und die Freigabe einer aktivierten Aufnahme bereit,
/// damit beide Aufnahmewege denselben Abschlussablauf verwenden können.
/// </summary>
internal interface IArmedCaptureWindow : IDisposable
{
    /// <summary>Höchste pro Frame beobachtete Mittelamplitude dieser Aufnahme.</summary>
    int MaxAmplitudeObserved { get; }

    /// <summary>Wie viele Frames der Segmentierer in diesem Fenster verarbeitet hat.</summary>
    int ObservedFrameCount { get; }

    /// <summary>Zuletzt geschätzter Hintergrundpegel für die Erkennung des Sprachbeginns.</summary>
    int EstimatedFloor { get; }

    /// <summary>Zuletzt verwendete Schwelle zur Erkennung des Sprachbeginns.</summary>
    int AppliedOnsetThreshold { get; }
}
