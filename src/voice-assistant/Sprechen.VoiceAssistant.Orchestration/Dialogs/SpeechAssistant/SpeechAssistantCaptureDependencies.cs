namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Aufnahmeclient, Erkennungsclient und die gemeinsame Verarbeitung
/// der Erkennungsaufträge für den Aufbau des Assistenten.
/// </summary>
internal sealed class SpeechAssistantCaptureDependencies
{
    public SpeechAssistantCaptureDependencies(
        ISpeechCaptureClient speechCaptureClient,
        ISpeechRecognitionClient speechRecognitionClient,
        SpeechRecognitionWorker? speechRecognitionWorker = null)
    {
        SpeechCaptureClient = speechCaptureClient ?? throw new ArgumentNullException(nameof(speechCaptureClient));
        SpeechRecognitionClient = speechRecognitionClient ?? throw new ArgumentNullException(nameof(speechRecognitionClient));
        // Ohne vorgegebene Auftragsverarbeitung eine Standardinstanz für den Client anlegen.
        // So nutzen auch vereinfachte Zusammenstellungen dieselbe Auftragsplanung.
        SpeechRecognitionWorker = speechRecognitionWorker ?? new SpeechRecognitionWorker(speechRecognitionClient);
    }

    public ISpeechCaptureClient SpeechCaptureClient { get; }

    public ISpeechRecognitionClient SpeechRecognitionClient { get; }

    /// <summary>
    /// Stellt die gemeinsame Verarbeitung der Erkennungsaufträge bereit.
    /// Die Effekte reichen dort ihre Aufträge ein.
    /// </summary>
    public SpeechRecognitionWorker SpeechRecognitionWorker { get; }
}
