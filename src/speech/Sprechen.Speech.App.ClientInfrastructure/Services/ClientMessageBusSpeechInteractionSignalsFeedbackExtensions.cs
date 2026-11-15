using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Erlaubt Aufrufe für Aktivierungszustand und Aufnahmefehler direkt am gebündelten Signalobjekt. Die Ausführung bleibt beim explizit implementierten Rückmeldungsvertrag.
/// </summary>
public static class ClientMessageBusSpeechInteractionSignalsFeedbackExtensions
{
    public static ValueTask SetActivationStateAsync(
        this ClientMessageBusSpeechInteractionSignals signals,
        SpeechAssistantActivationState activationState)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechCaptureFeedback)signals).SetActivationStateAsync(activationState);
    }

    public static ValueTask ReportFailureAsync(this ClientMessageBusSpeechInteractionSignals signals, string message)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechCaptureFeedback)signals).ReportFailureAsync(message);
    }

    public static ValueTask ReportFailureAsync(
        this ClientMessageBusSpeechInteractionSignals signals,
        SpeechErrorCode errorCode,
        string message)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechCaptureFeedback)signals).ReportFailureAsync(errorCode, message);
    }

    public static IDisposable SubscribeToActivationStateChanged(
        this ClientMessageBusSpeechInteractionSignals signals,
        Func<SpeechAssistantActivationStateChanged, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechCaptureFeedback)signals).SubscribeToActivationStateChanged(handler);
    }

    public static IDisposable SubscribeToFailures(
        this ClientMessageBusSpeechInteractionSignals signals,
        Func<SpeechCaptureFailed, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return ((ISpeechCaptureFeedback)signals).SubscribeToFailures(handler);
    }
}
