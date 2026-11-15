using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.Runtime.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant.Logging;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Startet Ansage und Aufnahme gemeinsam, wenn die Ansage unterbrochen werden darf.
/// Behandelt fehlende Sprache und vorübergehende Aufnahmefehler und beobachtet
/// in diesen Fällen auch den Abschluss der Wiedergabe.
/// </summary>
internal sealed class SelectionDialogBargeInTurn
{
    private readonly ISpeechCaptureRecordingIndicator _recordingIndicator;
    private readonly IModeAwareSpeechCaptureClient _modeAwareSpeechCaptureClient;
    private readonly SelectionDialogPromptSpeaker _promptSpeaker;
    private readonly SelectionDialogSpeechTranscriber _transcriber;
    private readonly SelectionDialogRecoverableSpeechFailureMapper _failureMapper;
    private readonly ILogger _logger;

    public SelectionDialogBargeInTurn(
        ISpeechCaptureRecordingIndicator recordingIndicator,
        IModeAwareSpeechCaptureClient modeAwareSpeechCaptureClient,
        SelectionDialogPromptSpeaker promptSpeaker,
        SelectionDialogSpeechTranscriber transcriber,
        SelectionDialogRecoverableSpeechFailureMapper failureMapper,
        ILogger logger)
    {
        _recordingIndicator = recordingIndicator ?? throw new ArgumentNullException(nameof(recordingIndicator));
        _modeAwareSpeechCaptureClient = modeAwareSpeechCaptureClient ?? throw new ArgumentNullException(nameof(modeAwareSpeechCaptureClient));
        _promptSpeaker = promptSpeaker ?? throw new ArgumentNullException(nameof(promptSpeaker));
        _transcriber = transcriber ?? throw new ArgumentNullException(nameof(transcriber));
        _failureMapper = failureMapper ?? throw new ArgumentNullException(nameof(failureMapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SpeechCaptureResult> SpeakAndListenAsync(
        SelectionDialogPrompt prompt,
        string isoCode,
        SelectionDialogInteractionSettings interactionSettings,
        int noInputTimeoutMs,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null)
    {
        await using var recordingScope = await _recordingIndicator.BeginRecordingAsync().ConfigureAwait(false);

        // Die Grenze fehlender Eingabe als anfängliches Stillefenster an die Aufnahme geben. Ein
        // zusätzlicher Zeitabbruch um die gesamte Aufnahme würde auch eine bereits begonnene
        // Äußerung abschneiden, bevor ihre Erkennung abgeschlossen ist.
        var initialSilenceTimeout = TimeSpan.FromMilliseconds(Math.Max(1, noInputTimeoutMs));

        var promptTask = _promptSpeaker.SpeakAsync(prompt, isoCode, interactionSettings, cancellationToken);
        try
        {
            var utterance = await _modeAwareSpeechCaptureClient
                .RecordAsync(SpeechCaptureMode.PromptInterruption, initialSilenceTimeout, cancellationToken)
                .ConfigureAwait(false);
            await _promptSpeaker.ObserveCompletionAsync(promptTask, cancellationToken).ConfigureAwait(false);
            await _recordingIndicator.PlayInputAcceptedCueAsync(cancellationToken).ConfigureAwait(false);
            return await _transcriber.TranscribeAsync(utterance, isoCode, cancellationToken, keyterms).ConfigureAwait(false);
        }
        catch (NoSpeechDetectedException)
        {
            await _promptSpeaker.ObserveCompletionAsync(promptTask, cancellationToken).ConfigureAwait(false);
            return NoTranscript();
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            await _promptSpeaker.ObserveCompletionAsync(promptTask, cancellationToken).ConfigureAwait(false);
            return RecoverableFailure(ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ObservePromptCompletionWithoutMaskingCallerCancellation(promptTask);
            throw;
        }
        catch (SpeechRecognitionQueueFullException ex)
        {
            await _promptSpeaker.ObserveCompletionAsync(promptTask, cancellationToken).ConfigureAwait(false);
            return RecoverableFailure(ex);
        }
        catch (SpeechRecognitionBackoffExhaustedException ex)
        {
            await _promptSpeaker.ObserveCompletionAsync(promptTask, cancellationToken).ConfigureAwait(false);
            return RecoverableFailure(ex);
        }
        catch (Exception ex) when (ex is not VoiceInputPausedException)
        {
            _logger.LogWarning(SelectionRuntimeLogEvents.SpeechRecognitionFailedDuringBargeIn, ex, "Speech recognition failed.");
            throw;
        }
    }

    private static SpeechCaptureResult NoTranscript() => new(null, null);

    private SpeechCaptureResult RecoverableFailure(SpeechRecognitionQueueFullException exception) => _failureMapper.Map(exception);

    private SpeechCaptureResult RecoverableFailure(SpeechRecognitionBackoffExhaustedException exception) => _failureMapper.Map(exception);

    private SpeechCaptureResult RecoverableFailure(OperationCanceledException exception) => _failureMapper.Map(exception);

    private static void ObservePromptCompletionWithoutMaskingCallerCancellation(Task promptTask)
    {
        if (promptTask.IsCompletedSuccessfully || promptTask.IsCanceled)
        {
            return;
        }

        // Den Abbruch durch den Aufrufer sofort weitergeben, auch wenn die Wiedergabe
        // verzögert endet. Einen gleichzeitigen Ansagefehler ohne zusätzliches Warten beobachten.
        _ = promptTask.ContinueWith(
            static completed =>
            {
                if (completed.IsFaulted)
                {
                    _ = completed.Exception;
                }
            },
            TaskScheduler.Default);
    }
}
