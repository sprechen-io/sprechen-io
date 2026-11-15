using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Verbindet Ansage, Aufnahme und Erkennung für die Aktivierungseinrichtung,
/// damit offene Diktate und Ja/Nein-Antworten ihre passende Schlüsselwortauswahl erhalten.
/// </summary>
internal sealed class WakeWordSetupSpeechInteractor(
    ISpeechCaptureClient speechCaptureClient,
    SpeechRecognitionWorker speechRecognitionWorker,
    ITextToSpeechPlayer textToSpeechPlayer,
    IEarconPlayer earconPlayer,
    ISpeechCaptureRecordingIndicator recordingIndicator,
    ILogger<WakeWordSetupSpeechInteractor> logger) : IWakeWordSetupSpeechInteractor
{
    private readonly ISpeechCaptureClient _speechCaptureClient =
        speechCaptureClient ?? throw new ArgumentNullException(nameof(speechCaptureClient));

    private readonly SpeechRecognitionWorker _speechRecognitionWorker =
        speechRecognitionWorker ?? throw new ArgumentNullException(nameof(speechRecognitionWorker));

    private readonly ITextToSpeechPlayer _textToSpeechPlayer =
        textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));

    private readonly IEarconPlayer _earconPlayer = earconPlayer ?? throw new ArgumentNullException(nameof(earconPlayer));

    private readonly ISpeechCaptureRecordingIndicator _recordingIndicator =
        recordingIndicator ?? throw new ArgumentNullException(nameof(recordingIndicator));

    private readonly ILogger<WakeWordSetupSpeechInteractor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // Neue Aktivierungsformen sind frei gewählt. Ohne vorgegebene Schlüsselwörter
    // wird das offene Diktat nicht auf bereits bekannte Formen gewichtet.
    private static readonly SpeechKeyterms NoKeyterms = new([], KeytermComposition.WithoutServerBase);

    private static SpeechKeyterms YesNoKeyterms(string isoCode)
    {
        var yesNo = VoiceInterfaceModelCatalog.Lexicon.YesNo;
        return new SpeechKeyterms(
            [.. yesNo.GetYesTokens(isoCode), .. yesNo.GetNoTokens(isoCode)],
            KeytermComposition.WithoutServerBase);
    }

    public async Task<bool?> PromptYesNoAsync(string prompt, string isoCode, int maxAttempts, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Dieser Zug erwartet nur die modellierten Ja/Nein-Antworten.
            // Andere Gesprächswörter sollen die kurze Antwort nicht gewichten.
            var captureResult = await PromptSpeechToTextAsync(
                    prompt,
                    isoCode,
                    YesNoKeyterms(isoCode),
                    cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(captureResult.FailureCode))
            {
                // Ein technischer Aufnahmefehler führt zu einem weiteren Versuch. Nach ausgeschöpftem
                // Budget liefert die Methode jedoch wie bei einer ausbleibenden gültigen Antwort null.
                // Der Aufrufer kann diese Ursachen dann nicht mehr unterscheiden. Null belegt
                // weder eine Verneinung noch das Schweigen der teilnehmenden Person.
                continue;
            }

            var transcript = captureResult.Transcript;
            if (string.IsNullOrWhiteSpace(transcript))
            {
                continue;
            }

            if (YesNo.TryParseYesNo(transcript, isoCode, out var isYes))
            {
                return isYes;
            }
        }

        return null;
    }

    /// <summary>
    /// Nimmt frei gewählte Aktivierungsformen ohne Schlüsselwortgewichtung auf,
    /// damit unbekannte neue Formen nicht durch ein vorgegebenes Wortinventar verdrängt werden.
    /// </summary>
    public Task<SpeechCaptureResult> PromptSpeechToTextAsync(string prompt, string isoCode, CancellationToken cancellationToken)
        => PromptSpeechToTextAsync(prompt, isoCode, NoKeyterms, cancellationToken);

    public async Task<SpeechCaptureResult> PromptSpeechToTextAsync(
        string prompt,
        string isoCode,
        SpeechKeyterms keyterms,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(keyterms);

        try
        {
            await SpeakAsync(prompt, isoCode, cancellationToken).ConfigureAwait(false);

            CapturedSpeechAudio? utterance;
            try
            {
                await using var recordingScope = await _recordingIndicator.BeginRecordingAsync().ConfigureAwait(false);
                utterance = await _speechCaptureClient.RecordAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (NoSpeechDetectedException)
            {
                return NoTranscript();
            }

            if (utterance is null)
            {
                return NoTranscript();
            }

            await _recordingIndicator.PlayInputAcceptedCueAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var transcript = await _speechRecognitionWorker
                    .EnqueueAsync(utterance, isoCode, RecognitionProfile.Conversational, keyterms, cancellationToken)
                    .ConfigureAwait(false);
                var transcriptText = transcript.Text?.Trim();
                return string.IsNullOrWhiteSpace(transcriptText)
                    ? new SpeechCaptureResult(null, transcript.ConfidencePercent)
                    : new SpeechCaptureResult(transcriptText, transcript.ConfidencePercent);
            }
            catch (NoSpeechDetectedException)
            {
                return NoTranscript();
            }
            catch (SpeechRecognitionQueueFullException ex)
            {
                return RecoverableFailure(ex);
            }
            catch (SpeechRecognitionBackoffExhaustedException ex)
            {
                return RecoverableFailure(ex);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Ein Abbruch ohne angeforderten Aufruferabbruch wird als behebbarer Fehler
                // gemeldet, damit eine technische Unterbrechung nicht als fehlende Äußerung gilt.
                return RecoverableFailure(ex);
            }
        }
        finally
        {
            await StopAllEarconsSafeAsync(cancellationToken).ConfigureAwait(false);
            await _recordingIndicator.EnsureStoppedAsync().ConfigureAwait(false);
        }
    }

    public async Task SpeakAsync(string text, string isoCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        try
        {
            await _earconPlayer.PlayAsync(EarconId.FlowOutputStart, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Earcon playback failed: {Earcon}", EarconId.FlowOutputStart);
        }

        try
        {
            await _textToSpeechPlayer.SpeakAsync(text, isoCode, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Ein ausgabeseitiger Abbruch, etwa durch Dazwischensprechen, beendet nur diese Ansage.
        }
    }

    private async ValueTask StopAllEarconsSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _earconPlayer.StopAllAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Earcon stopAll failed.");
        }
    }

    private SpeechCaptureResult RecoverableFailure(SpeechRecognitionQueueFullException ex)
    {
        _logger.LogWarning(ex, "Wake-word setup transcription rejected (code={Code}).", ex.Code);
        return new SpeechCaptureResult(null, null, ex.Code, true);
    }

    private SpeechCaptureResult RecoverableFailure(SpeechRecognitionBackoffExhaustedException ex)
    {
        _logger.LogWarning(ex, "Wake-word setup transcription rejected (code={Code}).", ex.Code);
        return new SpeechCaptureResult(null, null, ex.Code, true);
    }

    private SpeechCaptureResult RecoverableFailure(OperationCanceledException ex)
    {
        _logger.LogWarning(ex, "Wake-word setup transcription rejected (code={Code}).", SpeechCaptureFailureCodes.CaptureInterrupted);
        return new SpeechCaptureResult(null, null, SpeechCaptureFailureCodes.CaptureInterrupted, true);
    }

    private static SpeechCaptureResult NoTranscript() => new(null, null);
}
