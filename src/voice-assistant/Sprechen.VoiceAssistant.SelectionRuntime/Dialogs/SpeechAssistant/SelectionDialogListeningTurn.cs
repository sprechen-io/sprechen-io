using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.Runtime.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant.Logging;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Nimmt eine Antwort mit Aufnahmeindikator auf und lässt sie erkennen.
/// Bei leerem Erkennungsergebnis kann die Aufnahme begrenzt wiederholt werden,
/// ohne den Aufnahmeindikator erneut zu starten.
/// </summary>
internal sealed class SelectionDialogListeningTurn
{
    // Begrenzt die Aufnahmeversuche bei wiederholt leerer Texterkennung. Am Ende wird das markierte
    // Ergebnis an die Phase zurückgegeben, die die passende Rückmeldung bestimmt.
    private const int MaxCapturedButEmptyAttempts = 3;

    private readonly ISpeechCaptureRecordingIndicator _recordingIndicator;
    private readonly IModeAwareSpeechCaptureClient _modeAwareSpeechCaptureClient;
    private readonly SelectionDialogSpeechTranscriber _transcriber;
    private readonly SelectionDialogRecoverableSpeechFailureMapper _failureMapper;
    private readonly ILogger _logger;

    public SelectionDialogListeningTurn(
        ISpeechCaptureRecordingIndicator recordingIndicator,
        IModeAwareSpeechCaptureClient modeAwareSpeechCaptureClient,
        SelectionDialogSpeechTranscriber transcriber,
        SelectionDialogRecoverableSpeechFailureMapper failureMapper,
        ILogger logger)
    {
        _recordingIndicator = recordingIndicator ?? throw new ArgumentNullException(nameof(recordingIndicator));
        _modeAwareSpeechCaptureClient = modeAwareSpeechCaptureClient ?? throw new ArgumentNullException(nameof(modeAwareSpeechCaptureClient));
        _transcriber = transcriber ?? throw new ArgumentNullException(nameof(transcriber));
        _failureMapper = failureMapper ?? throw new ArgumentNullException(nameof(failureMapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SpeechCaptureResult> ListenAsync(
        string isoCode,
        int noInputTimeoutMs,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null,
        SpeechCaptureMode captureMode = SpeechCaptureMode.Listening)
    {
        await using var recordingScope = await _recordingIndicator.BeginRecordingAsync().ConfigureAwait(false);

        try
        {
            // Die Wartezeit auf den Sprachbeginn wird an die Aufnahme übergeben. Ein
            // zusätzlicher Zeitabbruch um Aufnahme und Erkennung würde auch eine rechtzeitig
            // begonnene Antwort abschneiden.
            var initialSilenceTimeout = TimeSpan.FromMilliseconds(Math.Max(1, noInputTimeoutMs));

            // Bei leerer Texterkennung innerhalb desselben Aufnahmeindikators erneut aufnehmen. So
            // wird nicht für jeden Versuch ein neuer Bereitschaftston angefordert. Nach der Grenze
            // entscheidet die Phase über die Rückmeldung.
            for (var attempt = 1; ; attempt++)
            {
                var utterance = await _modeAwareSpeechCaptureClient
                    .RecordAsync(captureMode, initialSilenceTimeout, cancellationToken)
                    .ConfigureAwait(false);
                await _recordingIndicator.PlayInputAcceptedCueAsync(cancellationToken).ConfigureAwait(false);
                var result = await _transcriber.TranscribeAsync(utterance, isoCode, cancellationToken, keyterms).ConfigureAwait(false);
                if (!result.RecognizedAsEmpty || attempt >= MaxCapturedButEmptyAttempts)
                {
                    if (result.RecognizedAsEmpty)
                    {
                        // Das Ausschöpfen der Versuche mit deren Zahl als Warnung protokollieren.
                        // Die einzelnen Wiederholungen bleiben Diagnosemeldungen auf Debug-Stufe.
                        _logger.LogWarning(
                            SelectionRuntimeLogEvents.CapturedButEmptyReArmExhausted,
                            "Captured utterance recognized as empty on every attempt ({Attempts}); the no-input prompt follows.",
                            attempt);
                    }

                    return result;
                }

                _logger.LogDebug(
                    SelectionRuntimeLogEvents.CapturedUtteranceRecognizedAsEmpty,
                    "Captured utterance recognized as empty (attempt {Attempt}); silently re-arming capture without a new listening cue.",
                    attempt);

                // Vor der erneuten Aufnahme den Verarbeitungston ausdrücklich stoppen. Der
                // Aufnahmeindikator wird nicht neu begonnen. Dessen üblicher Tonwechsel kann diese
                // Freigabe hier daher nicht übernehmen.
                await _recordingIndicator.StopProcessingCueAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (NoSpeechDetectedException)
        {
            return NoTranscript();
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return RecoverableFailure(ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SpeechRecognitionQueueFullException ex)
        {
            return RecoverableFailure(ex);
        }
        catch (SpeechRecognitionBackoffExhaustedException ex)
        {
            return RecoverableFailure(ex);
        }
        catch (Exception ex) when (ex is not VoiceInputPausedException)
        {
            _logger.LogWarning(SelectionRuntimeLogEvents.SpeechRecognitionFailedDuringListening, ex, "Speech recognition failed.");
            throw;
        }
    }

    private static SpeechCaptureResult NoTranscript() => new(null, null);

    private SpeechCaptureResult RecoverableFailure(SpeechRecognitionQueueFullException exception) => _failureMapper.Map(exception);

    private SpeechCaptureResult RecoverableFailure(SpeechRecognitionBackoffExhaustedException exception) => _failureMapper.Map(exception);

    private SpeechCaptureResult RecoverableFailure(OperationCanceledException exception) => _failureMapper.Map(exception);
}
