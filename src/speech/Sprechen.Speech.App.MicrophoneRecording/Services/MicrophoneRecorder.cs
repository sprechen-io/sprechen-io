using MicroSharp;
using MicroSharp.Browser;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.MicrophoneRecording.Services;

/// <summary>
/// Application facade over <see cref="BrowserCaptureController"/>.
/// The 16 kHz capture format, the open-after-cancel source value and speech policy stay here.
/// Acquisition, reuse, release and queue ownership live in MicroSharp.
/// </summary>
internal sealed class MicrophoneRecorder(
    IJSRuntime jsRuntime,
    ILogger<MicrophoneRecorder>? logger = null,
    TimeProvider? timeProvider = null,
    TimeSpan? releaseProgressTimeout = null)
    : IMicrophoneRecorder, ICaptureTrackProcessingReader, IMicGateBrowserBridge, IMicrophoneLifecyclePolicyOwner
{
    private const int RequiredSampleRateHz = 16_000;
    private const int DefaultChunkMilliseconds = 20;

    private readonly BrowserCaptureController _controller = new(
        jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime)),
        new BrowserCaptureControllerOptions
        {
            Format = new PcmFormat(RequiredSampleRateHz, TimeSpan.FromMilliseconds(DefaultChunkMilliseconds)),
            QueueCapacityChunks = 50,
            EchoCancellation = true,
            NoiseSuppression = true,
            AutoGainControl = true,
            KeepAudioContextAlive = true,
            ReleaseProgressTimeout = releaseProgressTimeout is { } configured && configured > TimeSpan.Zero
                ? configured
                : TimeSpan.FromSeconds(5),
            SourceEnabledAfterCancellation = true,
            TimeProvider = timeProvider,
            Diagnostic = (exception, message) => logger?.LogDebug(exception, "{Message}", message),
            SourceReclaimed = milliseconds => logger?.LogInformation(
                "Microphone device reclaimed after {FullyReleasedMilliseconds} ms fully released.",
                milliseconds),
        });

    internal int SourceOwnershipGeneration => _controller.SourceOwnershipGeneration;

    public async Task<IMicrophoneRecording> StartAsync(CancellationToken cancellationToken)
        => await StartRecordingAsync(DefaultChunkMilliseconds, cancellationToken).ConfigureAwait(false);

    internal Task<MicrophoneRecording> StartRecordingAsync(int timesliceMilliseconds, CancellationToken cancellationToken)
    {
        if (timesliceMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timesliceMilliseconds),
                timesliceMilliseconds,
                "Timeslice must be positive.");
        }

        var format = new PcmFormat(RequiredSampleRateHz, TimeSpan.FromMilliseconds(timesliceMilliseconds));
        return InvokeAsync(async () => new MicrophoneRecording(await _controller.StartAsync(format, cancellationToken).ConfigureAwait(false)));
    }

    public Task RequestAccessAsync(CancellationToken cancellationToken)
        => InvokeAsync(() => _controller.RequestAccessAsync(cancellationToken));

    public Task ReleaseAsync(CancellationToken cancellationToken)
        => InvokeAsync(() => _controller.ReleaseAsync(cancellationToken));

    public Task SetMicGateAsync(bool enabled, CancellationToken cancellationToken)
        => InvokeAsync(() => _controller.SetSourceEnabledAsync(enabled, cancellationToken));

    public async ValueTask<CaptureTrackProcessing> ReadAsync(CancellationToken cancellationToken)
    {
        var reported = await _controller.ReadTrackProcessingAsync(cancellationToken).ConfigureAwait(false);
        return new CaptureTrackProcessing(reported.EchoCancellation, reported.NoiseSuppression, reported.AutoGainControl);
    }

    internal Task StopAsync(CancellationToken cancellationToken)
        => InvokeAsync(() => _controller.StopAsync(cancellationToken));

    private async Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw Map(exception);
        }
    }

    private async Task InvokeAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw Map(exception);
        }
    }

    private static Exception Map(Exception exception)
    {
        if (exception is BrowserSourceOwnershipException ownership)
        {
            return ownership.InnerException is null
                ? new MicrophoneAcquisitionException(MapKind(ownership.Kind))
                : new MicrophoneAcquisitionException(MapKind(ownership.Kind), ownership.InnerException);
        }

        if (exception is not AggregateException aggregate)
        {
            return exception;
        }

        var mapped = new Exception[aggregate.InnerExceptions.Count];
        var changed = false;
        for (var index = 0; index < mapped.Length; index++)
        {
            mapped[index] = Map(aggregate.InnerExceptions[index]);
            changed |= !ReferenceEquals(mapped[index], aggregate.InnerExceptions[index]);
        }

        return changed ? new AggregateException(aggregate.Message, mapped) : aggregate;
    }

    private static MicrophoneAcquisitionFailureCode MapKind(BrowserSourceFailureKind kind)
        => kind switch
        {
            BrowserSourceFailureKind.DeviceNotFound => MicrophoneAcquisitionFailureCode.DeviceNotFound,
            BrowserSourceFailureKind.PermissionDenied => MicrophoneAcquisitionFailureCode.PermissionDenied,
            _ => MicrophoneAcquisitionFailureCode.Unavailable,
        };
}
