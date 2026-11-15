using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.Runtime.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Ansage, Aufnahme und Erkennung im Auswahldialog. Getrennte Abläufe
/// ermöglichen die Aufnahme entweder nach der Ansage oder bereits während ihrer
/// Wiedergabe, wenn eine Unterbrechung erlaubt ist.
/// </summary>
public sealed class SelectionDialogSpeechTurnRunner(
    ISpeechCaptureRecordingIndicator recordingIndicator,
    ISpeechCaptureClient speechCaptureClient,
    IModeAwareSpeechCaptureClient modeAwareSpeechCaptureClient,
    SpeechRecognitionWorker speechRecognitionWorker,
    ITextToSpeechPlayer textToSpeechPlayer,
    ILogger<SelectionDialogSpeechTurnRunner> logger)
{
    private readonly SelectionDialogPromptSpeaker _promptSpeaker = new(
        textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer)),
        logger ?? throw new ArgumentNullException(nameof(logger)));
    private readonly SelectionDialogListeningTurn _listeningTurn = CreateListeningTurn(
        recordingIndicator ?? throw new ArgumentNullException(nameof(recordingIndicator)),
        speechCaptureClient ?? throw new ArgumentNullException(nameof(speechCaptureClient)),
        modeAwareSpeechCaptureClient ?? throw new ArgumentNullException(nameof(modeAwareSpeechCaptureClient)),
        speechRecognitionWorker ?? throw new ArgumentNullException(nameof(speechRecognitionWorker)),
        logger ?? throw new ArgumentNullException(nameof(logger)));
    private readonly SelectionDialogBargeInTurn _bargeInTurn = CreateBargeInTurn(
        recordingIndicator ?? throw new ArgumentNullException(nameof(recordingIndicator)),
        modeAwareSpeechCaptureClient ?? throw new ArgumentNullException(nameof(modeAwareSpeechCaptureClient)),
        speechRecognitionWorker ?? throw new ArgumentNullException(nameof(speechRecognitionWorker)),
        textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer)),
        logger ?? throw new ArgumentNullException(nameof(logger)));

    public async Task<SpeechCaptureResult> ListenAsync(
        string isoCode,
        int noInputTimeoutMs,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null,
        SpeechCaptureMode captureMode = SpeechCaptureMode.Listening)
    {
        return await _listeningTurn.ListenAsync(
                isoCode,
                noInputTimeoutMs,
                cancellationToken,
                keyterms,
                captureMode)
            .ConfigureAwait(false);
    }

    public async Task<SpeechCaptureResult> SpeakAndListenAsync(
        SelectionDialogPrompt prompt,
        string isoCode,
        SelectionDialogInteractionSettings interactionSettings,
        int noInputTimeoutMs,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null,
        SpeechCaptureMode postPromptCaptureMode = SpeechCaptureMode.Listening)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(interactionSettings);

        if (!interactionSettings.AllowBargeIn)
        {
            // Beim sequenziellen Ablauf den gewünschten Aufnahmemodus nach der Ansage weiterreichen,
            // damit eine Bestätigung ihre vorgesehenen Aufnahmeregeln behält. Der
            // Unterbrechungsablauf verwendet stattdessen seinen eigenen Modus während der
            // Wiedergabe.
            await SpeakAsync(prompt, isoCode, interactionSettings, cancellationToken).ConfigureAwait(false);
            return await ListenAsync(isoCode, noInputTimeoutMs, cancellationToken, keyterms, postPromptCaptureMode).ConfigureAwait(false);
        }

        return await _bargeInTurn
            .SpeakAndListenAsync(prompt, isoCode, interactionSettings, noInputTimeoutMs, cancellationToken, keyterms)
            .ConfigureAwait(false);
    }

    public async Task SpeakAsync(
        SelectionDialogPrompt prompt,
        string isoCode,
        SelectionDialogInteractionSettings interactionSettings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(interactionSettings);

        await _promptSpeaker.SpeakAsync(prompt, isoCode, interactionSettings, cancellationToken).ConfigureAwait(false);
    }

    public async Task SpeakAsync(string text, string isoCode, CancellationToken cancellationToken)
    {
        await _promptSpeaker.SpeakAsync(text, isoCode, cancellationToken).ConfigureAwait(false);
    }

    private static SelectionDialogListeningTurn CreateListeningTurn(
        ISpeechCaptureRecordingIndicator recordingIndicator,
        ISpeechCaptureClient speechCaptureClient,
        IModeAwareSpeechCaptureClient modeAwareSpeechCaptureClient,
        SpeechRecognitionWorker speechRecognitionWorker,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(speechCaptureClient);
        var transcriber = new SelectionDialogSpeechTranscriber(speechRecognitionWorker);
        var failureMapper = new SelectionDialogRecoverableSpeechFailureMapper(logger);
        return new SelectionDialogListeningTurn(
            recordingIndicator,
            modeAwareSpeechCaptureClient,
            transcriber,
            failureMapper,
            logger);
    }

    private static SelectionDialogBargeInTurn CreateBargeInTurn(
        ISpeechCaptureRecordingIndicator recordingIndicator,
        IModeAwareSpeechCaptureClient modeAwareSpeechCaptureClient,
        SpeechRecognitionWorker speechRecognitionWorker,
        ITextToSpeechPlayer textToSpeechPlayer,
        ILogger logger)
    {
        var transcriber = new SelectionDialogSpeechTranscriber(speechRecognitionWorker);
        var failureMapper = new SelectionDialogRecoverableSpeechFailureMapper(logger);
        var promptSpeaker = new SelectionDialogPromptSpeaker(textToSpeechPlayer, logger);
        return new SelectionDialogBargeInTurn(
            recordingIndicator,
            modeAwareSpeechCaptureClient,
            promptSpeaker,
            transcriber,
            failureMapper,
            logger);
    }
}
