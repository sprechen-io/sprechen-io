using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

/// <summary>
/// Marks a JavaScript runtime that already speaks the MicroSharp browser contract.
/// </summary>
internal interface IDirectPackageMicrophoneRuntime
{
}

/// <summary>
/// Builds the application recorder. Legacy browser doubles are adapted here, not in production.
/// </summary>
internal static class MicrophoneTestRecorder
{
    public static MicrophoneRecorder Create(
        IJSRuntime jsRuntime,
        ILogger<MicrophoneRecorder>? logger = null,
        TimeProvider? timeProvider = null,
        TimeSpan? releaseProgressTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        var runtime = jsRuntime is IDirectPackageMicrophoneRuntime or LegacyMicrophoneJsAdapter
            ? jsRuntime
            : new LegacyMicrophoneJsAdapter(jsRuntime);
        return new MicrophoneRecorder(runtime, logger, timeProvider, releaseProgressTimeout);
    }
}
