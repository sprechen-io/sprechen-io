namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Bündelt die Zustands- und Rückmeldungssignale einer Sprachinteraktion für gemeinsam angebundene Beobachter.
/// </summary>
public interface ISpeechInteractionSignals :
    ISpeechCaptureFeedback,
    ISpeechCaptureRecordingIndicator,
    ISpeechBargeInSignals,
    IEarconFamilySignals,
    ISpeechWakeListeningFallbackSignals,
    ISpeechCaptureStoppedSignals
{
}
