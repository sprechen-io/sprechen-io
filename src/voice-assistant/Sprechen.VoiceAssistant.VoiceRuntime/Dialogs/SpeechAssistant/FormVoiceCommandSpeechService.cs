using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Formularansagen, Aufnahme und Erkennung einschließlich
/// Bestätigungsversuchen und Wiederholungszielen für den Dialogablauf.
/// </summary>
internal sealed class FormVoiceCommandSpeechService : IFormVoiceCommandSpeechService, IDeclaredAnnouncementSpeaker
{
    private readonly FormVoiceCommandPromptPlayback _promptPlayback;
    private readonly FormVoiceCommandPromptCaptureRunner _captureRunner;
    private readonly FormVoiceCommandSpeechRecognitionRunner _speechRecognitionRunner;
    private readonly FormVoiceCommandNoInputFeedbackObserver? _noInputFeedback;
    private readonly IFormVoiceCommandSpokenOutputReplayBuffer? _replayBuffer;

    public FormVoiceCommandSpeechService(
        IEvaluationSessionTelemetry evaluationTelemetry,
        ISpeechCaptureRecordingIndicator recordingIndicator,
        ISpeechBargeInSignals bargeInSignals,
        SpeechRecognitionWorker speechRecognitionWorker,
        ITextToSpeechPlayer textToSpeechPlayer,
        IFormVoiceCommandRecordingService recordingService,
        ILogger<FormVoiceCommandSpeechService> logger,
        FormVoiceCommandNoInputFeedbackObserver? noInputFeedback = null,
        IFormVoiceCommandSpokenOutputReplayBuffer? replayBuffer = null)
    {
        ArgumentNullException.ThrowIfNull(evaluationTelemetry);
        ArgumentNullException.ThrowIfNull(recordingIndicator);
        ArgumentNullException.ThrowIfNull(bargeInSignals);
        ArgumentNullException.ThrowIfNull(speechRecognitionWorker);
        ArgumentNullException.ThrowIfNull(textToSpeechPlayer);
        ArgumentNullException.ThrowIfNull(recordingService);
        ArgumentNullException.ThrowIfNull(logger);

        _promptPlayback = new FormVoiceCommandPromptPlayback(textToSpeechPlayer, logger);
        _captureRunner = new FormVoiceCommandPromptCaptureRunner(
            evaluationTelemetry,
            recordingIndicator,
            bargeInSignals,
            recordingService,
            _promptPlayback);
        _speechRecognitionRunner = new FormVoiceCommandSpeechRecognitionRunner(speechRecognitionWorker, logger);
        _noInputFeedback = noInputFeedback;
        _replayBuffer = replayBuffer;
    }

    public Task<SpeechCaptureResult> CaptureTranscriptAsync(
        string prompt,
        string isoCode,
        FormVoiceCommandCaptureSettings captureSettings,
        Action onStartListening,
        SpeechCaptureTelemetryContext? telemetryContext,
        RecognitionProfile recognitionProfile,
        SpeechKeyterms? keyterms,
        CancellationToken cancellationToken)
        => CaptureTranscriptAsync(
            prompt,
            isoCode,
            captureSettings,
            onStartListening,
            telemetryContext,
            recognitionProfile,
            keyterms,
            attempt: null,
            SpeechCaptureMode.Listening,
            cancellationToken);

    public Task<SpeechCaptureResult> CaptureTranscriptSingleAttemptAsync(
        string prompt,
        string isoCode,
        FormVoiceCommandCaptureSettings captureSettings,
        Action onStartListening,
        SpeechCaptureTelemetryContext? telemetryContext,
        RecognitionProfile recognitionProfile,
        SpeechKeyterms? keyterms,
        CancellationToken cancellationToken)
        => CaptureTranscriptAsync(
            prompt,
            isoCode,
            captureSettings,
            onStartListening,
            telemetryContext,
            recognitionProfile,
            keyterms,
            new FormVoiceCommandAttempt(1, 1, FormVoiceCommandAttemptFailure.None),
            SpeechCaptureMode.Listening,
            cancellationToken);

    public Task<SpeechCaptureResult> CaptureConfirmationTranscriptAsync(
        string prompt,
        string isoCode,
        FormVoiceCommandCaptureSettings captureSettings,
        Action onStartListening,
        SpeechCaptureTelemetryContext? telemetryContext,
        SpeechKeyterms? keyterms,
        CancellationToken cancellationToken)
        => CaptureTranscriptAsync(
            prompt,
            isoCode,
            captureSettings,
            onStartListening,
            telemetryContext,
            RecognitionProfile.Conversational,
            keyterms,
            new FormVoiceCommandAttempt(1, 1, FormVoiceCommandAttemptFailure.None),
            // Bestätigungen verwenden ohne unterbrechbare Ansage den Kurzantwortmodus,
            // der den Schutz vor verfrüht erkanntem Sprachbeginn nach der Ausgabe anpasst.
            // Die Grammatik wird später ausgewertet. Ein einzelner Aufnahmeversuch lässt
            // die Entscheidung über eine erneute Rückfrage beim äußeren Ablauf.
            SpeechCaptureMode.ShortConfirmation,
            cancellationToken);

    private async Task<SpeechCaptureResult> CaptureTranscriptAsync(
        string prompt,
        string isoCode,
        FormVoiceCommandCaptureSettings captureSettings,
        Action onStartListening,
        SpeechCaptureTelemetryContext? telemetryContext,
        RecognitionProfile recognitionProfile,
        SpeechKeyterms? keyterms,
        FormVoiceCommandAttempt? attempt,
        SpeechCaptureMode postPromptCaptureMode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(captureSettings);
        ArgumentNullException.ThrowIfNull(onStartListening);

        var captureRequest = new FormVoiceCommandCaptureRequest(
            prompt,
            isoCode,
            captureSettings,
            onStartListening,
            telemetryContext,
            attempt,
            postPromptCaptureMode);

        // Bei internen Wiederholungen den Hinweis zwischen den Aufnahmeversuchen
        // aktivieren. Für einzeln übergebene Versuche fordert der äußere Ablauf
        // die erneute Ansage an.
        if (attempt is null)
        {
            _noInputFeedback?.SetActiveCapture(isoCode, captureSettings.VoiceSettings.GetSpeechRate(), captureSettings.VoiceSettings.GetPolitenessLevel());
        }

        try
        {
            var utterance = await _captureRunner.CaptureAsync(captureRequest, cancellationToken).ConfigureAwait(false);
            return await _speechRecognitionRunner.RecognizeAsync(utterance, isoCode, recognitionProfile, keyterms, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _noInputFeedback?.ClearActiveCapture();
        }
    }

    public async Task<bool?> ConfirmYesNoAsync(
        FormVoiceCommandCaptureSettings captureSettings,
        string prompt,
        string isoCode,
        SpeechCaptureTelemetryContext? telemetryContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captureSettings);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        // Die Bestätigung besitzt ihr eigenes Budget aus Wiederholungen plus
        // Erstversuch. Jeder Zug durchläuft erneut die Ansage gemäß Einstellung
        // und nimmt genau einmal auf.
        // Eine zusätzliche innere Wiederholung würde die Budgets multiplizieren.
        // Auch eine nicht deutbare Antwort erhält innerhalb des Budgets einen weiteren Zug.
        var maxRetries = Math.Max(0, captureSettings.VoiceSettings.GetMaxRetries());
        var budget = maxRetries + 1;
        var precedingFailure = FormVoiceCommandAttemptFailure.None;

        for (var attemptNumber = 1; attemptNumber <= budget; attemptNumber++)
        {
            string? transcript;
            try
            {
                var captureResult = await CaptureTranscriptAsync(
                        prompt,
                        isoCode,
                        captureSettings,
                        onStartListening: () => { },
                        telemetryContext,
                        RecognitionProfile.Conversational,
                        keyterms: null,
                        new FormVoiceCommandAttempt(attemptNumber, budget, precedingFailure),
                        // Bestätigungen verwenden nach der Ansage den Kurzantwortmodus.
                        // Feldwerte behalten den normalen Hörmodus.
                        SpeechCaptureMode.ShortConfirmation,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(captureResult.FailureCode))
                {
                    // Den Fehler getrennt kennzeichnen, statt mit leerem Transkript in den
                    // Fall einer nicht erkannten Antwort zu gelangen.
                    precedingFailure = FormVoiceCommandAttemptFailure.CaptureFailure;
                    continue;
                }

                transcript = captureResult.Transcript;
            }
            catch (NoSpeechDetectedException)
            {
                precedingFailure = FormVoiceCommandAttemptFailure.NoInputTimeout;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(transcript)
                && YesNo.TryParseYesNo(transcript, isoCode, out var isYes))
            {
                return isYes;
            }

            // Die Aufnahme ergab keine deutbare Ja-Nein-Antwort. Das unterscheidet
            // sich von der oben behandelten Eingabezeitüberschreitung.
            precedingFailure = FormVoiceCommandAttemptFailure.UnrecognizedAnswer;
        }

        return null;
    }

    public async Task SpeakSafeAsync(
        string text,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(voiceSettings);

        // Die Ansage vor dem Abspielen als Wiederholungsziel merken. Aufnahme-
        // und Bestätigungsfragen laufen über den getrennten Aufnahmeablauf.
        // Sie sollen dieses Ziel nicht überschreiben.
        _replayBuffer?.Record(text);
        await _promptPlayback.SpeakSafeAsync(text.Trim(), isoCode, voiceSettings, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fordert eine Ansage mit ausdrücklich angegebener Ereignisart an.
    /// Der Vertrag <see cref="IDeclaredAnnouncementSpeaker"/> stellt diese
    /// Fähigkeit getrennt von den allgemeinen Sprachdiensten bereit.
    /// </summary>
    public async Task SpeakAnnouncementAsync(
        string text,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        SpokenAnnouncementKind announcementKind,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(voiceSettings);

        _replayBuffer?.Record(text);
        await _promptPlayback
            .SpeakSafeAsync(text.Trim(), isoCode, voiceSettings, announcementKind, cancellationToken)
            .ConfigureAwait(false);
    }

    // Für Wiederholungen den Tempofaktor mit eigener Untergrenze anwenden,
    // ohne allgemeine Spracheinstellungen zu verändern. Liegt das ursprüngliche
    // Tempo bereits unter der Untergrenze, kann es dabei steigen.
    private const double ReplaySpeechRateFactor = 0.85;
    private const double MinReplaySpeechRate = 0.5;

    public async Task SpeakReplayAsync(
        string text,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(voiceSettings);

        // Die Wiederholung nicht erneut speichern, sonst würde die automatische
        // Wiederholung nochmals freigegeben. Die Tempoanpassung gilt nur für diese Ausgabe.
        var slowedSettings = VoiceDialogSettingsRuntimeOverrides.ApplySpeechRateOverride(
            voiceSettings,
            Math.Max(MinReplaySpeechRate, voiceSettings.GetSpeechRate() * ReplaySpeechRateFactor));
        await _promptPlayback.SpeakSafeAsync(text.Trim(), isoCode, slowedSettings, cancellationToken).ConfigureAwait(false);
    }
}
