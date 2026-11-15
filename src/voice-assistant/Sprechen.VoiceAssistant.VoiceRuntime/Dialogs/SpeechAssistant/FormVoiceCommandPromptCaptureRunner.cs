using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Evaluation;

namespace Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Koordiniert Ansage und Aufnahme je Darstellungs- und Unterbrechungsmodus,
/// damit Hörbeginn und Annahmehinweis an den passenden Stellen ausgelöst werden.
/// </summary>
internal sealed class FormVoiceCommandPromptCaptureRunner
{
    private readonly IEvaluationSessionTelemetry _evaluationTelemetry;
    private readonly ISpeechCaptureRecordingIndicator _recordingIndicator;
    private readonly ISpeechBargeInSignals _bargeInSignals;
    private readonly IFormVoiceCommandRecordingService _recordingService;
    private readonly FormVoiceCommandPromptPlayback _promptPlayback;

    public FormVoiceCommandPromptCaptureRunner(
        IEvaluationSessionTelemetry evaluationTelemetry,
        ISpeechCaptureRecordingIndicator recordingIndicator,
        ISpeechBargeInSignals bargeInSignals,
        IFormVoiceCommandRecordingService recordingService,
        FormVoiceCommandPromptPlayback promptPlayback)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _recordingIndicator = recordingIndicator ?? throw new ArgumentNullException(nameof(recordingIndicator));
        _bargeInSignals = bargeInSignals ?? throw new ArgumentNullException(nameof(bargeInSignals));
        _recordingService = recordingService ?? throw new ArgumentNullException(nameof(recordingService));
        _promptPlayback = promptPlayback ?? throw new ArgumentNullException(nameof(promptPlayback));
    }

    public async Task<CapturedSpeechAudio> CaptureAsync(
        FormVoiceCommandCaptureRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var voiceSettings = request.CaptureSettings.VoiceSettings;
        if (request.CaptureSettings.PromptPresentation == SpeechCapturePromptPresentation.ListeningCueOnly)
        {
            return await CaptureWithoutPromptAsync(request, voiceSettings, cancellationToken).ConfigureAwait(false);
        }

        if (request.CaptureSettings.PromptPresentation != SpeechCapturePromptPresentation.Spoken)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.CaptureSettings.PromptPresentation,
                "Unsupported capture prompt presentation.");
        }

        if (voiceSettings.GetAllowBargeIn())
        {
            return await CaptureWithBargeInAsync(request, voiceSettings, cancellationToken).ConfigureAwait(false);
        }

        return await CaptureAfterPromptAsync(request, voiceSettings, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CapturedSpeechAudio> CaptureWithoutPromptAsync(
        FormVoiceCommandCaptureRequest request,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        await using var recordingScope = await _recordingIndicator.BeginRecordingAsync().ConfigureAwait(false);
        request.OnStartListening();
        var utterance = request.Attempt is { } attempt
            ? await _recordingService
                .RecordSingleAttemptAsync(
                    request.PostPromptCaptureMode,
                    voiceSettings,
                    startCursor: null,
                    attempt,
                    cancellationToken)
                .ConfigureAwait(false)
            : await _recordingService
                .RecordUntilSpeechAsync(
                    request.PostPromptCaptureMode,
                    voiceSettings,
                    startCursor: null,
                    cancellationToken)
                .ConfigureAwait(false);
        await PlayInputAcceptedFeedbackAsync(request, cancellationToken).ConfigureAwait(false);
        return utterance;
    }

    private async Task<CapturedSpeechAudio> CaptureWithBargeInAsync(
        FormVoiceCommandCaptureRequest request,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        await using var recordingScope = await _recordingIndicator.BeginRecordingAsync().ConfigureAwait(false);
        using var promptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var promptInterruptedBySpeech = false;
        Task promptTask = Task.CompletedTask;
        using var bargeInSubscription = _bargeInSignals.SubscribeToBargeIn(() =>
        {
            promptInterruptedBySpeech |= !promptTask.IsCompleted;
            CancelPrompt(promptCts);
            return ValueTask.CompletedTask;
        });

        promptTask = _promptPlayback.StartAsync(
            request.Prompt,
            request.IsoCode,
            voiceSettings.GetSpeechRate(),
            promptCts.Token);

        CapturedSpeechAudio utterance;
        try
        {
            request.OnStartListening();
            utterance = request.Attempt is { } attempt
                ? await _recordingService
                    .RecordSingleAttemptAsync(
                        SpeechCaptureMode.PromptInterruption,
                        voiceSettings,
                        startCursor: null,
                        attempt,
                        cancellationToken)
                    .ConfigureAwait(false)
                : await _recordingService
                    .RecordUntilSpeechAsync(
                        SpeechCaptureMode.PromptInterruption,
                        voiceSettings,
                        cancellationToken)
                    .ConfigureAwait(false);
        }
        finally
        {
            CancelPrompt(promptCts);
        }

        await _promptPlayback.ObserveCompletionAsync(promptTask, cancellationToken).ConfigureAwait(false);

        if (promptInterruptedBySpeech && request.TelemetryContext is not null)
        {
            _evaluationTelemetry.TrackBargeIn(
                request.TelemetryContext.Session,
                request.TelemetryContext.InterruptedPromptId);
        }

        await PlayInputAcceptedFeedbackAsync(request, cancellationToken).ConfigureAwait(false);
        return utterance;
    }

    private async Task<CapturedSpeechAudio> CaptureAfterPromptAsync(
        FormVoiceCommandCaptureRequest request,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        await _promptPlayback
            .ObserveCompletionAsync(
                _promptPlayback.StartAsync(
                    request.Prompt,
                    request.IsoCode,
                    voiceSettings.GetSpeechRate(),
                    cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        await DelayBeforeListeningAsync(request.CaptureSettings, cancellationToken).ConfigureAwait(false);
        await using var recordingScope = await _recordingIndicator.BeginRecordingAsync().ConfigureAwait(false);

        // Ohne expliziten Cursor kann die Aufnahmeschicht den zeitlichen Rückblick
        // des Wiedergabekoordinators nutzen. Dessen Ausgangspunkt kann bereits am
        // Ende der Sprachausgabe liegen, während der Hinweiston noch klingt.
        // Den passenden Wiedergabekontext bestimmt deshalb die Aufnahmeschicht.
        request.OnStartListening();
        var utterance = request.Attempt is { } attempt
            ? await _recordingService
                .RecordSingleAttemptAsync(
                    request.PostPromptCaptureMode,
                    voiceSettings,
                    startCursor: null,
                    attempt,
                    cancellationToken)
                .ConfigureAwait(false)
            : await _recordingService
                .RecordUntilSpeechAsync(
                    request.PostPromptCaptureMode,
                    voiceSettings,
                    startCursor: null,
                    cancellationToken)
                .ConfigureAwait(false);
        await PlayInputAcceptedFeedbackAsync(request, cancellationToken).ConfigureAwait(false);
        return utterance;
    }

    private ValueTask PlayInputAcceptedFeedbackAsync(
        FormVoiceCommandCaptureRequest request,
        CancellationToken cancellationToken)
        => request.CaptureSettings.InputAcceptanceFeedback switch
        {
            SpeechInputAcceptanceFeedback.Standard =>
                _recordingIndicator.PlayInputAcceptedCueAsync(cancellationToken),
            SpeechInputAcceptanceFeedback.PostInterpretation or SpeechInputAcceptanceFeedback.CueFree =>
                ValueTask.CompletedTask,
            _ => throw new ArgumentOutOfRangeException(
                nameof(request),
                request.CaptureSettings.InputAcceptanceFeedback,
                "Unsupported input acceptance feedback."),
        };

    private static void CancelPrompt(CancellationTokenSource promptCts)
    {
        try
        {
            promptCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static Task DelayBeforeListeningAsync(
        FormVoiceCommandCaptureSettings captureSettings,
        CancellationToken cancellationToken)
    {
        if (captureSettings.PauseAfterPromptMs == 0)
        {
            return Task.CompletedTask;
        }

        return Task.Delay(captureSettings.PauseAfterPromptMs, cancellationToken);
    }
}
