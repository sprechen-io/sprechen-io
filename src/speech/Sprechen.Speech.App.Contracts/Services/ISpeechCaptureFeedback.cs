
namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Verteilt Aktivierungszustände und Aufnahmefehler für die Rückmeldung des Sprachassistenten.
/// </summary>
public interface ISpeechCaptureFeedback
{
    SpeechAssistantActivationState ActivationState { get; }

    ValueTask SetActivationStateAsync(SpeechAssistantActivationState activationState);

    ValueTask ReportFailureAsync(string message);

    ValueTask ReportFailureAsync(SpeechErrorCode errorCode, string message);

    IDisposable SubscribeToActivationStateChanged(Func<SpeechAssistantActivationStateChanged, ValueTask> handler);

    IDisposable SubscribeToFailures(Func<SpeechCaptureFailed, ValueTask> handler);
}
