using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Führt einzelne Aufnahmen und begrenzte Wiederholungen mit gemeinsamem
/// Versuchsbeobachter aus. Modus und Stillegrenze gehen an den Aufnahmedienst.
/// </summary>
internal sealed class FormVoiceCommandRecordingService : IFormVoiceCommandRecordingService
{
    private readonly ISpeechCaptureClient _speechCaptureClient;
    private readonly IModeAwareSpeechCaptureClient _modeAwareSpeechCaptureClient;
    private readonly IFormVoiceCommandAttemptObserver? _attemptObserver;

    public FormVoiceCommandRecordingService(
        ISpeechCaptureClient speechCaptureClient,
        IModeAwareSpeechCaptureClient modeAwareSpeechCaptureClient,
        IFormVoiceCommandAttemptObserver? attemptObserver = null)
    {
        _speechCaptureClient = speechCaptureClient ?? throw new ArgumentNullException(nameof(speechCaptureClient));
        _modeAwareSpeechCaptureClient = modeAwareSpeechCaptureClient
            ?? throw new ArgumentNullException(nameof(modeAwareSpeechCaptureClient));
        _attemptObserver = attemptObserver;
    }

    public SampleCursor? SnapshotLiveCaptureCursor()
        => _modeAwareSpeechCaptureClient?.SnapshotLiveCaptureCursor();

    public Task<CapturedSpeechAudio> RecordUntilSpeechAsync(
        SpeechCaptureMode captureMode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
        => RecordUntilSpeechAsync(captureMode, voiceSettings, startCursor: null, cancellationToken);

    public async Task<CapturedSpeechAudio> RecordUntilSpeechAsync(
        SpeechCaptureMode captureMode,
        VoiceDialogSettings voiceSettings,
        SampleCursor? startCursor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(voiceSettings);

        var maxRetries = Math.Max(0, voiceSettings.GetMaxRetries());
        var budget = maxRetries + 1;
        var precedingFailure = FormVoiceCommandAttemptFailure.None;

        for (var attemptNumber = 1; ; attemptNumber++)
        {
            try
            {
                return await RecordSingleAttemptAsync(
                        captureMode,
                        voiceSettings,
                        startCursor,
                        new FormVoiceCommandAttempt(attemptNumber, budget, precedingFailure),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (NoSpeechDetectedException) when (attemptNumber <= maxRetries)
            {
                // Den expliziten Cursor nur beim ersten Versuch verwenden. Ein weiterer
                // Versuch überlässt die Startposition wieder der Aufnahmeschicht.
                startCursor = null;
                precedingFailure = FormVoiceCommandAttemptFailure.NoInputTimeout;
            }
        }
    }

    public async Task<CapturedSpeechAudio> RecordSingleAttemptAsync(
        SpeechCaptureMode captureMode,
        VoiceDialogSettings voiceSettings,
        SampleCursor? startCursor,
        FormVoiceCommandAttempt attempt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(voiceSettings);

        if (!Enum.IsDefined(captureMode))
        {
            throw new ArgumentOutOfRangeException(nameof(captureMode), captureMode, "Unsupported form-dialog capture mode.");
        }

        // Eine Standardinstanz der Struktur umgeht den Konstruktor. Deshalb
        // den einsbasierten Versuch innerhalb seines Budgets nochmals prüfen,
        // bevor der Beobachter ihn erhält.
        if (attempt.AttemptBudget < 1 || attempt.AttemptNumber < 1 || attempt.AttemptNumber > attempt.AttemptBudget)
        {
            throw new ArgumentException(
                $"Attempt {attempt.AttemptNumber} of {attempt.AttemptBudget} is not a valid 1-based attempt within its budget.",
                nameof(attempt));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (_attemptObserver is not null)
        {
            await _attemptObserver.OnAttemptStartingAsync(attempt, cancellationToken).ConfigureAwait(false);
        }

        // Die Eingabegrenze betrifft die anfängliche Stille. Eine zusätzliche
        // Zeitbegrenzung um den ganzen Versuch würde auch eine bereits bestätigte
        // Äußerung abschneiden. Deren Höchstdauer begrenzt der Segmentierer gesondert.
        var noInputTimeout = TimeSpan.FromMilliseconds(Math.Max(1, voiceSettings.GetNoInputTimeoutMs()));
        return await RecordAsync(captureMode, noInputTimeout, startCursor, cancellationToken)
            .ConfigureAwait(false);
    }

    private Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        SampleCursor? startCursor,
        CancellationToken cancellationToken)
    {
        if (startCursor is { } cursor)
        {
            return RecordModeAwareFromCursorAsync(captureMode, initialSilenceTimeout, cursor, cancellationToken);
        }

        // Die einfache Aufnahmeschnittstelle übernimmt keine Grenze für anfängliche
        // Stille. Der Konstruktor verlangt bereits die modusfähige Schnittstelle.
        if (captureMode == SpeechCaptureMode.Listening && _modeAwareSpeechCaptureClient is null)
        {
            return _speechCaptureClient.RecordAsync(cancellationToken);
        }

        return RecordModeAwareAsync(captureMode, initialSilenceTimeout, cancellationToken);
    }

    private Task<CapturedSpeechAudio> RecordModeAwareAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        CancellationToken cancellationToken)
        => RequireModeAwareClient(captureMode).RecordAsync(captureMode, initialSilenceTimeout, cancellationToken);

    private Task<CapturedSpeechAudio> RecordModeAwareFromCursorAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        SampleCursor startCursor,
        CancellationToken cancellationToken)
        => RequireModeAwareClient(captureMode)
            .RecordAsync(captureMode, initialSilenceTimeout, startCursor, cancellationToken);

    private IModeAwareSpeechCaptureClient RequireModeAwareClient(SpeechCaptureMode captureMode)
    {
        if (_modeAwareSpeechCaptureClient is null)
        {
            throw new InvalidOperationException(
                $"Form-dialog capture mode '{captureMode}' requires an IModeAwareSpeechCaptureClient owner.");
        }

        return _modeAwareSpeechCaptureClient;
    }
}

