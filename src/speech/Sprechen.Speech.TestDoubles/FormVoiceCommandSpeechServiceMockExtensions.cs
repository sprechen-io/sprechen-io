using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.TestDoubles;

public static class FormVoiceCommandSpeechServiceMockExtensions
{
    // Bestätigungen nutzen hier die vorbereiteten Antworten der allgemeinen Aufnahme.
    // Dabei wird das Gesprächsprofil verwendet; Schlüsselbegriffe werden nicht weitergegeben.
    public static Mock<IFormVoiceCommandSpeechService> ForwardConfirmationCaptureToValueCapture(
        this Mock<IFormVoiceCommandSpeechService> speechService)
    {
        ArgumentNullException.ThrowIfNull(speechService);

        speechService
            .Setup(s => s.CaptureConfirmationTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, SpeechKeyterms?, CancellationToken>(
                (prompt, isoCode, captureSettings, onStartListening, telemetryContext, _, cancellationToken) =>
                    speechService.Object.CaptureTranscriptAsync(
                        prompt,
                        isoCode,
                        captureSettings,
                        onStartListening,
                        telemetryContext,
                        RecognitionProfile.Conversational,
                        keyterms: null,
                        cancellationToken));

        return speechService;
    }

    public static Mock<IFormVoiceCommandSpeechService> ForwardSingleAttemptCaptureToValueCapture(
        this Mock<IFormVoiceCommandSpeechService> speechService)
    {
        ArgumentNullException.ThrowIfNull(speechService);

        speechService
            .Setup(s => s.CaptureTranscriptSingleAttemptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                (prompt, isoCode, captureSettings, onStartListening, telemetryContext, recognitionProfile, keyterms, cancellationToken) =>
                    speechService.Object.CaptureTranscriptAsync(
                        prompt,
                        isoCode,
                        captureSettings,
                        onStartListening,
                        telemetryContext,
                        recognitionProfile,
                        keyterms,
                        cancellationToken));

        return speechService;
    }

}
