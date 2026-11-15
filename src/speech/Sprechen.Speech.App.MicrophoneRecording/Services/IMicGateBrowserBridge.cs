namespace Sprechen.Speech.App.MicrophoneRecording.Services;

/// <summary>
/// Überträgt die Mikrofonsperre auf <c>track.enabled</c> der aktiven Browserspur.
/// Die eigene Schnittstelle erlaubt die Prüfung des Beobachters ohne JavaScript-Laufzeit.
/// </summary>
internal interface IMicGateBrowserBridge
{
    Task SetMicGateAsync(bool enabled, CancellationToken cancellationToken);
}
