using System.Collections;
using MicroSharp.Browser;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

/// <summary>
/// Übersetzt die Paketschnittstelle auf die bisherigen Test-Doppel von micRecorder.
/// Die Anwendung selbst ruft micRecorder nicht auf.
/// </summary>
internal sealed class LegacyMicrophoneJsAdapter(IJSRuntime inner) : IJSRuntime, IJSObjectReference
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => identifier switch
        {
            "import" => ValueTask.FromResult((TValue)(object)this),
            "describe" => ValueTask.FromResult((TValue)(object)new BrowserHostDescription
            {
                Media = true,
                Audio = true,
                Worklet = true,
            }),
            "acquire" => AcquireAsync<TValue>(cancellationToken, args),
            "start" => StartAsync<TValue>(cancellationToken, args),
            "setSourceEnabled" => ForwardVoid<TValue>("micRecorder.setMicGate", cancellationToken, args),
            "disconnect" => ForwardVoid<TValue>("micRecorder.disconnect", cancellationToken, args),
            "stop" => ForwardVoid<TValue>("micRecorder.release", cancellationToken, args),
            "release" => ForwardVoid<TValue>("micRecorder.release", cancellationToken, args),
            "isLive" => inner.InvokeAsync<TValue>("micRecorder.isLive", cancellationToken, args),
            "trackProcessing" => TrackAsync<TValue>(cancellationToken, args),
            _ => throw new InvalidOperationException($"Unexpected JS invocation '{identifier}'."),
        };

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async ValueTask<TValue> AcquireAsync<TValue>(CancellationToken cancellationToken, object?[]? args)
    {
        var dto = await inner.InvokeAsync<MicrophoneAcquisitionDto>(
            "micRecorder.acquire",
            cancellationToken,
            [ReadKeepAlive(args)]).ConfigureAwait(false);
        if (dto is null)
        {
            throw new InvalidOperationException("The microphone acquisition result is malformed.");
        }

        return (TValue)(object)new BrowserAcquireResult
        {
            SessionId = dto.SessionId,
            ErrorName = dto.ErrorName,
        };
    }

    private async ValueTask<TValue> StartAsync<TValue>(CancellationToken cancellationToken, object?[]? args)
    {
        var sessionId = args is { Length: > 0 } ? args[0] : null;
        var callback = MicrophoneJsTestBridge.PendingCallback.Value
            ?? (args is { Length: > 1 } ? args[1] : null);
        var chunkMilliseconds = ReadChunkMilliseconds(args);
        var mime = await inner.InvokeAsync<string>(
            "micRecorder.connect",
            cancellationToken,
            [sessionId, callback, chunkMilliseconds]).ConfigureAwait(false);
        return (TValue)(object)new BrowserStartResult
        {
            SampleRateHz = ReadRate(mime),
            Channels = 1,
            ChunkMilliseconds = chunkMilliseconds,
        };
    }

    private async ValueTask<TValue> TrackAsync<TValue>(CancellationToken cancellationToken, object?[]? args)
    {
        var dto = await inner.InvokeAsync<MicrophoneTrackProcessingDto>(
            "micRecorder.trackProcessing",
            cancellationToken,
            args).ConfigureAwait(false);
        var reported = dto is null
            ? BrowserTrackProcessing.Unknown
            : new BrowserTrackProcessing
            {
                EchoCancellation = dto.EchoCancellation,
                NoiseSuppression = dto.NoiseSuppression,
                AutoGainControl = dto.AutoGainControl,
            };
        return (TValue)(object)reported;
    }

    private async ValueTask<TValue> ForwardVoid<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        var result = await inner.InvokeAsync<IJSVoidResult>(identifier, cancellationToken, args).ConfigureAwait(false);
        return (TValue)(object)result!;
    }

    private static bool ReadKeepAlive(object?[]? args)
    {
        if (args is not { Length: > 0 } || args[0] is not IDictionary preferences)
        {
            return false;
        }

        return preferences.Contains("keepAudioContextAlive") && preferences["keepAudioContextAlive"] is true;
    }

    private static int ReadChunkMilliseconds(object?[]? args)
    {
        if (args is { Length: >= 3 }
            && args[2] is IDictionary format
            && format.Contains("chunkMilliseconds")
            && format["chunkMilliseconds"] is int milliseconds)
        {
            return milliseconds;
        }

        return 20;
    }

    private static int ReadRate(string? mime)
    {
        foreach (var parameter in (mime ?? string.Empty).Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!parameter.StartsWith("rate=", StringComparison.Ordinal))
            {
                continue;
            }

            var value = parameter["rate=".Length..].Trim();
            if (int.TryParse(value, out var rate))
            {
                return rate;
            }
        }

        return 0;
    }
}
