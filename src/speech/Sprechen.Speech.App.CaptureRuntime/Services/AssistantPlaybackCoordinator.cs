using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Bestimmt den Aufnahmestart anhand der laufenden und gerade beendeten Wiedergabe.
/// Ein früh gesetzter Lesecursor erhält schnelle Antworten im Ringpuffer.
/// </summary>
internal sealed class AssistantPlaybackCoordinator
{
    private const int PlaybackSettlePollIntervalMs = 25;

    private readonly IEarconPlaybackState _earconPlaybackState;
    private readonly ITextToSpeechPlaybackState _textToSpeechPlaybackState;
    private readonly SpeechCaptureClientOptions _captureOptions;
    private readonly SpeechSampleRing _sampleRing;
    private readonly IBrowserDebugConsole _browserDebugConsole;
    private readonly ILogger _logger;

    public AssistantPlaybackCoordinator(
        IEarconPlaybackState earconPlaybackState,
        ITextToSpeechPlaybackState textToSpeechPlaybackState,
        IOptions<SpeechCaptureClientOptions> captureOptions,
        SpeechSampleRing sampleRing,
        IBrowserDebugConsole browserDebugConsole,
        ILogger<AssistantPlaybackCoordinator> logger)
    {
        _earconPlaybackState = earconPlaybackState ?? throw new ArgumentNullException(nameof(earconPlaybackState));
        _textToSpeechPlaybackState = textToSpeechPlaybackState ?? throw new ArgumentNullException(nameof(textToSpeechPlaybackState));
        ArgumentNullException.ThrowIfNull(captureOptions);
        _captureOptions = captureOptions.Value;
        _sampleRing = sampleRing ?? throw new ArgumentNullException(nameof(sampleRing));
        _browserDebugConsole = browserDebugConsole ?? throw new ArgumentNullException(nameof(browserDebugConsole));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CaptureStartResolution> ResolveCaptureStartContextAsync(
        SpeechCaptureModeBehavior modeBehavior,
        CancellationToken cancellationToken)
    {
        // Den Lesecursor nach dem Ende der Sprachausgabe schon vor dem Warten auf Earcons setzen.
        // So bleiben Antworten erhalten, die während des noch laufenden Orientierungssignals beginnen.
        SampleCursor? speechEndCursor = null;
        if (!_textToSpeechPlaybackState.IsPlaying && IsAssistantPlaybackCurrentlyActive())
        {
            speechEndCursor = _sampleRing.OpenCursor(SampleCursorOrigin.Latest);
        }

        var waitedForEarconPlayback = await WaitForEarconPlaybackToFinishAsync(cancellationToken).ConfigureAwait(false);

        if (modeBehavior.RequiresActivePlaybackContext)
        {
            if (IsAssistantPlaybackCurrentlyActive())
            {
                return new CaptureStartResolution(SpeechCaptureStartContext.AssistantPlaybackActive, null);
            }

            if (waitedForEarconPlayback || IsAssistantPlaybackRecentlyCompleted())
            {
                return new CaptureStartResolution(
                    SpeechCaptureStartContext.AssistantPlaybackRecentlySettledContinuation,
                    null);
            }

            return new CaptureStartResolution(SpeechCaptureStartContext.CleanListening, null);
        }

        var settle = await WaitForRecentAssistantPlaybackToSettleAsync(speechEndCursor, cancellationToken).ConfigureAwait(false);

        if (!waitedForEarconPlayback && !settle.Waited)
        {
            return new CaptureStartResolution(SpeechCaptureStartContext.CleanListening, null);
        }

        if (modeBehavior.IsContinuationMode)
        {
            return new CaptureStartResolution(
                SpeechCaptureStartContext.AssistantPlaybackRecentlySettledContinuation,
                null);
        }

        // Für die nächste Aufnahme nach Wiedergabe den früh gesetzten Cursor weitergeben.
        // Dadurch können während der Schutzfrist eingetroffene Audiodaten nachgelesen werden.
        return new CaptureStartResolution(
            SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle,
            settle.SettleCursor);
    }

    public async Task WaitForAssistantPlaybackToSettleAsync(CancellationToken cancellationToken)
    {
        await WaitForEarconPlaybackToFinishAsync(cancellationToken).ConfigureAwait(false);
        await WaitForRecentAssistantPlaybackToSettleAsync(null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> WaitForEarconPlaybackToFinishAsync(CancellationToken cancellationToken)
    {
        if (!_earconPlaybackState.IsPlaying)
        {
            return false;
        }

        _logger.LogDebug("Delaying speech capture until earcon playback completes.");
        await _earconPlaybackState.WaitForIdleAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task<RecentPlaybackSettleOutcome> WaitForRecentAssistantPlaybackToSettleAsync(
        SampleCursor? speechEndCursor,
        CancellationToken cancellationToken)
    {
        var cooldown = TimeSpan.FromMilliseconds(Math.Max(0, _captureOptions.PlaybackLeakageCooldownMs));
        var waited = false;
        var settleCursor = speechEndCursor;
        while (IsAssistantPlaybackCurrentlyActive()
               || (cooldown > TimeSpan.Zero && IsAssistantPlaybackRecentlyCompleted(cooldown)))
        {
            // Nach dem Ende der Sprachausgabe die aktuelle Pufferposition festhalten,
            // bevor die Schutzfrist endet. So geht eine früh begonnene Antwort nicht verloren.
            if (settleCursor is null && !_textToSpeechPlaybackState.IsPlaying)
            {
                settleCursor = _sampleRing.OpenCursor(SampleCursorOrigin.Latest);
            }

            if (!waited)
            {
                waited = true;
                _browserDebugConsole.WriteOptional(
                    "speech-capture",
                    "waiting for recent assistant playback to settle");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(PlaybackSettlePollIntervalMs), cancellationToken).ConfigureAwait(false);
        }

        if (!waited)
        {
            // Einen bereits vor dem Earcon-Warten gesetzten Cursor auch ohne weitere Wartezeit erhalten.
            return new RecentPlaybackSettleOutcome(false, speechEndCursor);
        }

        _browserDebugConsole.WriteOptional(
            "speech-capture",
            "recent assistant playback settled");
        return new RecentPlaybackSettleOutcome(true, settleCursor);
    }

    private bool IsAssistantPlaybackCurrentlyActive()
    {
        return _earconPlaybackState.IsPlaying || _textToSpeechPlaybackState.IsPlaying;
    }

    private bool IsAssistantPlaybackRecentlyCompleted()
    {
        var cooldown = TimeSpan.FromMilliseconds(Math.Max(0, _captureOptions.PlaybackLeakageCooldownMs));
        return IsAssistantPlaybackRecentlyCompleted(cooldown);
    }

    private bool IsAssistantPlaybackRecentlyCompleted(TimeSpan cooldown)
    {
        var earconRecentlyCompleted = !_earconPlaybackState.IsPlaying
            && _earconPlaybackState.IsPlayingOrRecentlyCompleted(cooldown);
        var textToSpeechRecentlyCompleted = !_textToSpeechPlaybackState.IsPlaying
            && _textToSpeechPlaybackState.IsPlayingOrRecentlyCompleted(cooldown);

        return earconRecentlyCompleted || textToSpeechRecentlyCompleted;
    }
}
