using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert Ansagen für die Zifferneingabe an und nimmt die nächste Antwort auf.
/// Unterscheidet fehlende Sprache von Erkennungsfehlern. Über den Abschluss
/// des Feldwerts entscheidet der gemeinsame Ablauf.
/// </summary>
internal sealed class DigitsWorkflowTranscriptCapture
{
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly DigitsKeytermVocabulary _keyterms = new();

    public DigitsWorkflowTranscriptCapture(IFormVoiceCommandSpeechService speechService)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public Task SpeakContinueOrDoneAsync(
        FormVoiceCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _speechService.SpeakSafeAsync(
            FormDigitsPromptBuilder.GetDigitsContinueOrDonePrompt(
                context.Contract,
                context.IsoCode,
                context.VoiceSettings.GetPolitenessLevel()),
            context.IsoCode,
            context.VoiceSettings,
            cancellationToken);
    }

    public Task SpeakRecognizerUnavailableAsync(
        FormVoiceCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CharacterAccumulationRecognizerUnavailableAnnouncement.SpeakAsync(
            _speechService,
            context,
            cancellationToken);
    }

    public async Task<CharacterAccumulationTurnCapture> CaptureWorkflowTranscriptAsync(
        FormVoiceCommandContext context,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        string capturedDigits,
        bool listeningCueOnly,
        CancellationToken cancellationToken)
    {
        try
        {
            // Nach der Nachfrage bei fehlender Sprache nur den Bereitschaftston
            // anfordern, damit nicht gleich eine weitere Eingabeaufforderung folgt.
            var captureSettings = listeningCueOnly
                ? context.CaptureSettings.WithPromptPresentation(SpeechCapturePromptPresentation.ListeningCueOnly)
                : context.CaptureSettings;
            var captureResult = await _speechService.CaptureTranscriptSingleAttemptAsync(
                    FormDigitsPromptBuilder.GetDigitsWorkflowPrompt(
                        context.Contract,
                        context.IsoCode,
                        capturedDigits,
                        context.VoiceSettings.GetPolitenessLevel()),
                    context.IsoCode,
                    captureSettings,
                    onStartListening: stateTracker.BeginListening,
                    new SpeechCaptureTelemetryContext(session),
                    RecognitionProfilePolicy.Resolve(InputMode.Digits),
                    // Zahlwörter und die unterstützten Ziffernbefehle als begrenzte
                    // Erkennungshinweise ohne allgemeines Servervokabular übergeben.
                    new SpeechKeyterms(
                        _keyterms.GetKeyterms(
                            context.Contract,
                            context.IsoCode,
                            context.VoiceSettings.GetPolitenessLevel()),
                        KeytermComposition.WithoutServerBase),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(captureResult.FailureCode))
            {
                // Ein als Ergebnis gemeldeter Aufnahmefehler erreicht keine catch-Klausel.
                // Als nicht verfügbare Erkennung zurückgeben, damit er nicht wie Stille
                // zum Abschluss eines Teilwerts führt.
                return CharacterAccumulationTurnCapture.RecognizerUnavailable;
            }

            return string.IsNullOrWhiteSpace(captureResult.Transcript)
                ? CharacterAccumulationTurnCapture.Silence
                : CharacterAccumulationTurnCapture.Transcribed(captureResult.Transcript, captureResult.SttConfidencePercent);
        }
        catch (NoSpeechDetectedException)
        {
            return CharacterAccumulationTurnCapture.Silence;
        }
        catch (Exception ex) when (RecoverableCaptureFault.IsTransientCaptureFault(ex))
        {
            // Vorübergehende Aufnahmefehler im Feldablauf behandeln. Sie dürfen nicht
            // als Stille gelten, die einen bereits gesammelten Teilwert abschließen könnte.
            return CharacterAccumulationTurnCapture.RecognizerUnavailable;
        }
    }
}
