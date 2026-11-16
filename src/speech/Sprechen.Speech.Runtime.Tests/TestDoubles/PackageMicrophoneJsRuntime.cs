using System.Collections;
using MicroSharp.Browser;
using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class PackageMicrophoneJsRuntime : IJSRuntime, IJSObjectReference, IDirectPackageMicrophoneRuntime
{
    private readonly List<string> _calls = [];
    private readonly List<BrowserMicrophoneSession> _sessions = [];
    private IDictionary? _acquirePreferences;
    private int _released;

    public string? AcquireErrorName { get; init; }

    public int ReportedSampleRateHz { get; init; } = 16_000;

    public IReadOnlyList<string> Calls
    {
        get
        {
            lock (_calls)
            {
                return [.. _calls];
            }
        }
    }

    public IReadOnlyList<BrowserMicrophoneSession> StartedSessions
    {
        get
        {
            lock (_sessions)
            {
                return [.. _sessions];
            }
        }
    }

    public object? LastAcquirePreference(string name)
        => _acquirePreferences is not null && _acquirePreferences.Contains(name)
            ? _acquirePreferences[name]
            : null;

    public ValueTask<bool> IsLiveAsync()
        => ValueTask.FromResult(Volatile.Read(ref _released) == 0 && _sessions.Count > 0);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        if (identifier.Contains("micRecorder", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Package bypass via '{identifier}'.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        lock (_calls)
        {
            _calls.Add(identifier);
        }

        return identifier switch
        {
            "import" => ValueTask.FromResult((TValue)(object)this),
            "describe" => ValueTask.FromResult((TValue)(object)new BrowserHostDescription
            {
                Media = true,
                Audio = true,
                Worklet = true,
            }),
            "acquire" => ValueTask.FromResult((TValue)(object)Acquire(args)),
            "start" => ValueTask.FromResult((TValue)(object)Start(args)),
            "isLive" => ValueTask.FromResult((TValue)(object)(Volatile.Read(ref _released) == 0)),
            "trackProcessing" => ValueTask.FromResult((TValue)(object)new BrowserTrackProcessing
            {
                EchoCancellation = true,
                NoiseSuppression = false,
                AutoGainControl = null,
            }),
            "disconnect" or "release" or "stop" or "setSourceEnabled" => CompleteVoid<TValue>(identifier),
            _ => throw new InvalidOperationException($"Unexpected JS invocation '{identifier}'."),
        };
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private BrowserAcquireResult Acquire(object?[]? args)
    {
        _acquirePreferences = args is { Length: > 0 } ? args[0] as IDictionary : null;
        if (!string.IsNullOrEmpty(AcquireErrorName))
        {
            return new BrowserAcquireResult { ErrorName = AcquireErrorName };
        }

        return new BrowserAcquireResult { SessionId = "pkg-1" };
    }

    private BrowserStartResult Start(object?[]? args)
    {
        var session = Assert.IsType<DotNetObjectReference<BrowserMicrophoneSession>>(args![1]).Value;
        lock (_sessions)
        {
            _sessions.Add(session);
        }

        return new BrowserStartResult
        {
            SampleRateHz = ReportedSampleRateHz,
            Channels = 1,
            ChunkMilliseconds = 20,
        };
    }

    private ValueTask<TValue> CompleteVoid<TValue>(string identifier)
    {
        if (identifier == "release")
        {
            Interlocked.Increment(ref _released);
        }

        return ValueTask.FromResult(default(TValue)!);
    }
}
