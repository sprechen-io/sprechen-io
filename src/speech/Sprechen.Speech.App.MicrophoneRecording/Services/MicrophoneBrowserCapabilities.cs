using MicroSharp;
using MicroSharp.Browser;
using Microsoft.JSInterop;

namespace Sprechen.Speech.App.MicrophoneRecording.Services;

/// <summary>
/// Öffnet die Browserquelle über MicroSharp und behält Sperre, Neustart und Freigabe
/// in der Anwendung. Die Bibliothek besitzt nur die rohe Aufnahme.
/// </summary>
internal sealed class MicrophoneBrowserCapabilities
{
    private const int RequiredSampleRateHz = 16_000;
    private const int QueueCapacityChunks = 50;

    private readonly object _gate = new();
    private readonly BrowserMicrophoneCapture _capture;
    private readonly Dictionary<string, BrowserMicrophoneHold> _holds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IMicrophoneSession> _sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Task> _releases = new(StringComparer.Ordinal);
    private readonly HashSet<string> _releaseDetached = new(StringComparer.Ordinal);

    public MicrophoneBrowserCapabilities(IJSRuntime jsRuntime)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        _capture = new BrowserMicrophoneCapture(
            jsRuntime,
            new BrowserMicrophoneOptions
            {
                EchoCancellation = true,
                NoiseSuppression = true,
                AutoGainControl = true,
                KeepAudioContextAlive = false,
            });
    }

    public async ValueTask<MicrophoneAcquisitionDto> AcquireAsync(
        bool keepAliveEnabled,
        CancellationToken cancellationToken)
    {
        var prepared = await _capture.PrepareAsync(keepAliveEnabled, cancellationToken).ConfigureAwait(false);
        if (prepared.Hold is not null)
        {
            lock (_gate)
            {
                _holds[prepared.Hold.SessionId] = prepared.Hold;
            }
        }

        return new MicrophoneAcquisitionDto
        {
            SessionId = prepared.Result.SessionId,
            ErrorName = prepared.Result.ErrorName,
        };
    }

    public async ValueTask<string> ConnectAsync(
        string sessionId,
        MicrophoneRecording recording,
        int timesliceMilliseconds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(recording);
        var hold = FindHold(sessionId) ?? throw new InvalidOperationException($"Microphone session '{sessionId}' is not open.");
        var format = new PcmFormat(RequiredSampleRateHz, TimeSpan.FromMilliseconds(timesliceMilliseconds));
        IMicrophoneSession session;
        try
        {
            session = await hold.StartAsync(
                new MicrophoneCaptureRequest
                {
                    Format = format,
                    Backpressure = CaptureBackpressure.DropOldest,
                    QueueCapacityChunks = QueueCapacityChunks,
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (MicrophoneException exception) when (TryReportedFormat(exception.Message, out var reported))
        {
            throw new InvalidOperationException(
                $"Microphone reported '{reported}'; capture requires rate={RequiredSampleRateHz}.");
        }

        lock (_gate)
        {
            _sessions[sessionId] = session;
        }

        recording.AttachCapture(session);
        return $"audio/pcm;rate={format.SampleRateHz}";
    }

    public async ValueTask DisconnectAsync(string sessionId, CancellationToken cancellationToken)
    {
        var hold = FindHold(sessionId);
        lock (_gate)
        {
            _sessions.Remove(sessionId);
        }

        if (hold is null)
        {
            return;
        }

        await hold.DisconnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public ValueTask ReleaseAsync(string sessionId, CancellationToken cancellationToken)
    {
        Task release;
        BrowserMicrophoneHold? starting = null;
        TaskCompletionSource? completion = null;
        lock (_gate)
        {
            _sessions.Remove(sessionId);
            if (!_releaseDetached.Contains(sessionId) && _releases.TryGetValue(sessionId, out var pending))
            {
                release = pending;
            }
            else if (!_releaseDetached.Contains(sessionId) && _holds.TryGetValue(sessionId, out var hold))
            {
                // Die Quelle bleibt auffindbar, bis die Freigabe gelingt, damit eine Sperre und
                // ein erneuter Versuch dieselbe Quelle treffen, waehrend der Browseraufruf laeuft.
                // Der Vorgang wird eingetragen, bevor er startet, sonst legt ein sofortiger Fehler
                // die schon beendete Aufgabe erst danach ab und blockiert den naechsten Versuch.
                completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _releases[sessionId] = completion.Task;
                release = completion.Task;
                starting = hold;
            }
            else
            {
                release = _capture.ReleaseBrowserSessionAsync(sessionId, cancellationToken).AsTask();
            }
        }

        if (starting is not null && completion is not null)
        {
            _ = FinishReleaseAsync(sessionId, starting, completion);
        }

        return new ValueTask(release);
    }

    private async Task FinishReleaseAsync(
        string sessionId,
        BrowserMicrophoneHold hold,
        TaskCompletionSource completion)
    {
        try
        {
            await hold.DisposeAsync().ConfigureAwait(false);
            lock (_gate)
            {
                if (_holds.TryGetValue(sessionId, out var current) && ReferenceEquals(current, hold))
                {
                    _holds.Remove(sessionId);
                    _releaseDetached.Remove(sessionId);
                }

                if (_releases.TryGetValue(sessionId, out var pending) && ReferenceEquals(pending, completion.Task))
                {
                    _releases.Remove(sessionId);
                }
            }

            completion.TrySetResult();
        }
        catch (Exception exception)
        {
            lock (_gate)
            {
                if (_releases.TryGetValue(sessionId, out var pending) && ReferenceEquals(pending, completion.Task))
                {
                    _releases.Remove(sessionId);
                }
            }

            completion.TrySetException(exception);
        }
    }

    public ValueTask SetMicGateAsync(string sessionId, bool enabled, CancellationToken cancellationToken)
    {
        var hold = FindHold(sessionId);
        if (hold is null)
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(hold.SetSourceEnabledAsync(enabled, cancellationToken).AsTask());
    }

    /// <summary>
    /// Der Aufrufer hat die Wartefrist aufgegeben. Der laufende Browseraufruf bleibt,
    /// aber der naechste Versuch erreicht den Browser erneut.
    /// </summary>
    internal void DetachInFlightRelease(string sessionId)
    {
        lock (_gate)
        {
            _releases.Remove(sessionId);
            _releaseDetached.Add(sessionId);
        }
    }

    public async ValueTask<bool> IsLiveAsync(string sessionId, CancellationToken cancellationToken)
    {
        var hold = FindHold(sessionId);
        if (hold is null)
        {
            return false;
        }

        return await hold.IsLiveAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<MicrophoneTrackProcessingDto> ReadTrackProcessingAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var hold = FindHold(sessionId);
        if (hold is null)
        {
            return new MicrophoneTrackProcessingDto();
        }

        var reported = await hold.ReadTrackProcessingAsync(cancellationToken).ConfigureAwait(false);
        return new MicrophoneTrackProcessingDto
        {
            EchoCancellation = reported.EchoCancellation,
            NoiseSuppression = reported.NoiseSuppression,
            AutoGainControl = reported.AutoGainControl,
        };
    }

    private BrowserMicrophoneHold? FindHold(string sessionId)
    {
        lock (_gate)
        {
            return _holds.TryGetValue(sessionId, out var hold) ? hold : null;
        }
    }

    private static bool TryReportedFormat(string message, out string reported)
    {
        const string marker = "reported=";
        var index = message.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            reported = string.Empty;
            return false;
        }

        reported = message[(index + marker.Length)..].Trim();
        return reported.Length > 0;
    }
}
