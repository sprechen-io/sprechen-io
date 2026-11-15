using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Aufnahme, Erkennung, Aktivierung und Rückmeldungen für die Effekte. Der gesonderte
/// Aufnahmeindikator erlaubt dabei die Komposition hörbarer Bereitschaftssignale.
/// </summary>
internal sealed class SpeechAssistantInteractionDependencies
{
    public SpeechAssistantInteractionDependencies(
        ISpeechInteractionSignals speechInteractionSignals,
        ISpeechCaptureRecordingIndicator speechCaptureRecordingIndicator,
        SpeechAssistantCaptureDependencies capture,
        SpeechAssistantWakeWordDependencies wakeWordServices,
        ITextToSpeechPlayer textToSpeechPlayer,
        IEarconPlayer earconPlayer,
        ISpeechAssistantTrace speechAssistantTrace,
        ILogger<ISpeechAssistantService> logger)
    {
        SpeechInteractionSignals = speechInteractionSignals ?? throw new ArgumentNullException(nameof(speechInteractionSignals));
        SpeechCaptureRecordingIndicator = speechCaptureRecordingIndicator ?? throw new ArgumentNullException(nameof(speechCaptureRecordingIndicator));
        Capture = capture ?? throw new ArgumentNullException(nameof(capture));
        WakeWordDependencies = wakeWordServices ?? throw new ArgumentNullException(nameof(wakeWordServices));
        TextToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
        EarconPlayer = earconPlayer ?? throw new ArgumentNullException(nameof(earconPlayer));
        SpeechAssistantTrace = speechAssistantTrace ?? throw new ArgumentNullException(nameof(speechAssistantTrace));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ISpeechInteractionSignals SpeechInteractionSignals { get; }

    /// <summary>
    /// Stellt den gesondert komponierten Aufnahmeindikator bereit. Die Clientregistrierung ergänzt
    /// diesen Port um Bereitschafts- und Verarbeitungstöne, während der Interaktionssignal-Port direkt
    /// auf den Nachrichtenbus zeigt. Die Trennung verhindert, dass der Fragezug die hörbare Ergänzung
    /// durch Wahl des anderen Ports umgeht. Sie garantiert keine erfolgreiche Wiedergabe.
    /// </summary>
    public ISpeechCaptureRecordingIndicator SpeechCaptureRecordingIndicator { get; }

    public SpeechAssistantCaptureDependencies Capture { get; }

    public ISpeechCaptureClient SpeechCaptureClient => Capture.SpeechCaptureClient;

    public ISpeechRecognitionClient SpeechRecognitionClient => Capture.SpeechRecognitionClient;

    public SpeechAssistantWakeWordDependencies WakeWordDependencies { get; }

    public SpeechAssistantWakeWordInteractionDependencies WakeWordInteractionDependencies => WakeWordDependencies.InteractionDependencies;

    public SpeechAssistantWakeWordRuntime WakeWordRuntime => WakeWordDependencies.Runtime;

    public ITextToSpeechPlayer TextToSpeechPlayer { get; }

    public IEarconPlayer EarconPlayer { get; }

    public ISpeechAssistantTrace SpeechAssistantTrace { get; }

    public ILogger<ISpeechAssistantService> Logger { get; }

    public SpeechRecognitionWorker SpeechRecognitionWorker => Capture.SpeechRecognitionWorker;
}
