using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.LanguageContracts;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Logging;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Spielt eine Frage am Sitzungseinstieg ab, zeigt die Aufnahme an und lässt die Antwort aufnehmen und erkennen. Die Annahmeentscheidung liegt außerhalb dieses Ablaufs.
/// </summary>
internal sealed class SessionEntryQuestion
{
    private static readonly ILanguageCatalog Endonyms = VoiceInterfaceLanguageCatalog.CreateDefault();

    private readonly SessionEntryLanguageOfferDependencies _dependencies;
    private readonly IModeAwareSpeechCaptureClient _modeAwareSpeechCaptureClient;

    public SessionEntryQuestion(SessionEntryLanguageOfferDependencies dependencies)
    {
        _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        ArgumentNullException.ThrowIfNull(dependencies.SpeechCaptureClient);
        _modeAwareSpeechCaptureClient = dependencies.ModeAwareSpeechCaptureClient
            ?? throw new ArgumentNullException(nameof(dependencies.ModeAwareSpeechCaptureClient));
    }

    /// <summary>
    /// Spricht die Frage vor der Antwortaufnahme und liefert ein verwendbares Transkript samt Aufnahme
    /// zurück. Fehlende Sprache oder ein leerer bereinigter Text ergeben keine verwendbare Antwort.
    /// </summary>
    public async ValueTask<SessionEntryAnswer> AskAsync(
        IReadOnlyList<SpokenSegment> question,
        string answerIsoCode,
        SpeechKeyterms keyterms,
        TimeSpan answerWindow,
        CancellationToken cancellationToken)
    {
        // Die Aufnahme vor der Frage vorbereiten, damit eine mögliche Mikrofonfreigabe nicht
        // erst das Antwortfenster beansprucht. Bekannte dauerhafte Fehler beim Mikrofonzugriff
        // werden dabei weitergegeben.
        await WarmUpCaptureAsync(cancellationToken).ConfigureAwait(false);

        foreach (var segment in question)
        {
            try
            {
                await _dependencies.TextToSpeechPlayer
                    .SpeakAsync(segment.Text, segment.IsoCode, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException
                and not VoiceInputPausedException)
            {
                // Den Wiedergabefehler vor dem Aufnahmefenster eigens kennzeichnen, damit der Aufrufer
                // diesen Frageversuch wiederholen kann. Eine teilweise erfolgte Wiedergabe wird
                // dadurch nicht ausgeschlossen.
                throw new SessionEntryQuestionUnspokenException(segment.IsoCode, ex);
            }
        }

        CapturedSpeechAudio? utterance;
        try
        {
            await using var recordingScope = await _dependencies.RecordingIndicator
                .BeginRecordingAsync()
                .ConfigureAwait(false);
            utterance = await RecordAnswerAsync(answerWindow, cancellationToken).ConfigureAwait(false);
        }
        catch (NoSpeechDetectedException)
        {
            _dependencies.EvaluationSessionTelemetry.TrackNoSpeechDetected();
            return SessionEntryAnswer.Silence;
        }

        if (utterance is null)
        {
            return SessionEntryAnswer.Silence;
        }

        // Die erfasste Eingabe vor dem Erkennungsauftrag mit einem Ton bestätigen,
        // damit die folgende Verarbeitungszeit hörbar eingeordnet wird.
        await _dependencies.RecordingIndicator.PlayInputAcceptedCueAsync(cancellationToken).ConfigureAwait(false);

        var transcript = await RecognizeAsync(utterance, answerIsoCode, keyterms, cancellationToken).ConfigureAwait(false);
        return transcript is null ? SessionEntryAnswer.Silence : new SessionEntryAnswer(transcript, utterance);
    }

    /// <summary>
    /// Erkennt die vorhandene Aufnahme erneut unter der Sitzungssprache mit deren Abbruchwörtern. Die
    /// Wiederverwendung eröffnet kein neues Aufnahmefenster. Der Aufrufer prüft dieses Ergebnis
    /// ausschließlich auf Abbruch.
    /// </summary>
    public async ValueTask<string?> RecognizeAgainUnderSessionLanguageAsync(
        CapturedSpeechAudio utterance,
        string sessionIsoCode,
        IReadOnlyList<string> cancelUtterances,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(utterance);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIsoCode);
        ArgumentNullException.ThrowIfNull(cancelUtterances);

        return await RecognizeAsync(utterance, sessionIsoCode, BuildKeyterms(cancelUtterances), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Baut Erkennungshinweise aus dem modellierten Ja, Nein und Eigennamen der angebotenen Sprache.
    /// Die begrenzte Liste ergänzt keine Serverbasis und bestimmt nicht die Annahmeregel: Weitere
    /// modellierte Antworten können zulässig sein, während kollidierende Ja-Wörter trotz Hinweis nicht
    /// allein entscheiden.
    /// </summary>
    public static SpeechKeyterms BuildAcceptanceKeyterms(string isoCode)
        => BuildKeyterms(
        [
            YesNo.GetYesWord(isoCode),
            YesNo.GetNoWord(isoCode),
            Endonyms.GetEndonym(LanguageCode.ParseIso6391(isoCode)),
        ]);

    private static SpeechKeyterms BuildKeyterms(IEnumerable<string> terms)
        => new(
            [.. terms.Where(term => !string.IsNullOrWhiteSpace(term)).Distinct(StringComparer.OrdinalIgnoreCase)],
            KeytermComposition.WithoutServerBase);

    private async ValueTask<string?> RecognizeAsync(
        CapturedSpeechAudio utterance,
        string answerIsoCode,
        SpeechKeyterms keyterms,
        CancellationToken cancellationToken)
    {
        try
        {
            var transcript = await _dependencies.SpeechRecognitionWorker
                .EnqueueAsync(
                    utterance,
                    answerIsoCode,
                    RecognitionProfile.Conversational,
                    keyterms,
                    cancellationToken)
                .ConfigureAwait(false);

            var answer = SessionEntryActivationWordFilter.Strip(transcript.Text?.Trim());

            // Ohne Text nach dem Entfernen der Aktivierungsform bleibt keine verwendbare Antwort. Das
            // verhindert eine zweite Erkennung auf diesem Pfad.
            return string.IsNullOrWhiteSpace(answer) ? null : answer;
        }
        catch (NoSpeechDetectedException)
        {
            // Die gemeldete fehlende Sprache als solches Ereignis erfassen und ohne verwendbare
            // Antwort zurückkehren, unabhängig davon, ob Aufnahme oder Erkennung sie meldet.
            _dependencies.EvaluationSessionTelemetry.TrackNoSpeechDetected();
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // Ein nicht durch den Auftragsabbruch erklärter Erkennungsabbruch liefert keine
            // verwendbare Antwort. Daraus wird weder eine Sprachauswahl noch eine Aussage über den
            // gewünschten Ausgang abgeleitet.
            return null;
        }
    }

    /// <summary>
    /// Fordert die Antwortaufnahme im Modus für kurze Bestätigungen an. Dieser Modus trägt die Regel
    /// für kurze Antworten unmittelbar nach einer Wiedergabe, statt sie im Fragenablauf nachzubilden.
    /// </summary>
    private Task<CapturedSpeechAudio> RecordAnswerAsync(TimeSpan answerWindow, CancellationToken cancellationToken)
        => _modeAwareSpeechCaptureClient.RecordAsync(
            SpeechCaptureMode.ShortConfirmation, answerWindow, cancellationToken);

    private async Task WarmUpCaptureAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dependencies.SpeechCaptureClient
                .EnsureCaptureSessionReadyAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException
            and not VoiceInputPausedException
            and not MicrophoneAcquisitionException
            {
                Code: MicrophoneAcquisitionFailureCode.PermissionDenied or
                    MicrophoneAcquisitionFailureCode.DeviceNotFound
            })
        {
            // Nach einem gewöhnlichen Vorbereitungsfehler wird die Aufnahme regulär angefordert.
            // Verweigerte Berechtigung oder fehlendes Gerät werden dagegen an den Aufrufer
            // weitergegeben, damit der unbrauchbare Aufnahmeweg nicht als Frageversuch weiterläuft.
            _dependencies.Logger.LogWarning(
                OrchestrationLogEvents.SessionEntryCaptureWarmUpFailed,
                ex,
                "Session-entry capture warm-up failed; the answer window will acquire capture normally.");
        }
    }
}
