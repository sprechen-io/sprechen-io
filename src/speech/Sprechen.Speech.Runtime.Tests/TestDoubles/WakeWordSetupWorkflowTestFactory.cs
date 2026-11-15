using Microsoft.Extensions.Logging.Abstractions;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal static class WakeWordSetupWorkflowTestFactory
{
    public static WakeWordSetupWorkflow Create(
        IVoiceActivationSettings voiceActivationSettings,
        ISpeechInputClient speechInputClient,
        ITextToSpeechPlayer textToSpeechPlayer,
        IEarconPlayer earconPlayer,
        ISpeechCaptureRecordingIndicator recordingIndicator)
    {
        ArgumentNullException.ThrowIfNull(voiceActivationSettings);
        ArgumentNullException.ThrowIfNull(speechInputClient);
        ArgumentNullException.ThrowIfNull(textToSpeechPlayer);
        ArgumentNullException.ThrowIfNull(earconPlayer);
        ArgumentNullException.ThrowIfNull(recordingIndicator);

        var wakeWordProvider = new WakeWordProvider(
            voiceActivationSettings,
            NullLogger<WakeWordProvider>.Instance);
        var scenarioFactory = new WakeWordSetupScenarioFactory(wakeWordProvider);
        var speechInteractor = new WakeWordSetupSpeechInteractor(
            speechInputClient,
            new SpeechRecognitionWorker(speechInputClient),
            textToSpeechPlayer,
            earconPlayer,
            recordingIndicator,
            NullLogger<WakeWordSetupSpeechInteractor>.Instance);

        return new WakeWordSetupWorkflow(
            scenarioFactory,
            speechInteractor,
            voiceActivationSettings,
            NullLogger<WakeWordSetupWorkflow>.Instance);
    }
}
