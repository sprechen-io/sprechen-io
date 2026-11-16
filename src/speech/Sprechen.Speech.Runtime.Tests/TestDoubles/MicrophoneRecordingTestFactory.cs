using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal static class MicrophoneRecordingTestFactory
{
    public static Task<MicrophoneRecording> StartAsync(
        IJSRuntime jsRuntime,
        int timesliceMilliseconds,
        CancellationToken cancellationToken = default)
    {
        return MicrophoneTestRecorder.Create(jsRuntime)
            .StartRecordingAsync(timesliceMilliseconds, cancellationToken);
    }
}
