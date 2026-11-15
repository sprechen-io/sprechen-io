using Microsoft.JSInterop;

namespace Sprechen.Speech.App.MicrophoneRecording.Services;

/// <summary>
/// Kapselt die JavaScript-Operationen einer Mikrofonsitzung, damit der C#-Lebenszyklus
/// Browserzugriffe auslösen kann, ohne deren Aufrufdetails zu verteilen.
/// </summary>
internal sealed class MicrophoneBrowserCapabilities(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    public ValueTask<MicrophoneAcquisitionDto> AcquireAsync(
        bool keepAliveEnabled,
        CancellationToken cancellationToken) =>
        _jsRuntime.InvokeAsync<MicrophoneAcquisitionDto>(
            "micRecorder.acquire",
            cancellationToken,
            keepAliveEnabled);

    public ValueTask<string> ConnectAsync(
        string sessionId,
        DotNetObjectReference<MicrophoneRecording> callbackReference,
        int timesliceMilliseconds,
        CancellationToken cancellationToken) =>
        _jsRuntime.InvokeAsync<string>(
            "micRecorder.connect",
            cancellationToken,
            sessionId,
            callbackReference,
            timesliceMilliseconds);

    public ValueTask DisconnectAsync(string sessionId, CancellationToken cancellationToken) =>
        _jsRuntime.InvokeVoidAsync("micRecorder.disconnect", cancellationToken, sessionId);

    public ValueTask SetMicGateAsync(
        string sessionId,
        bool enabled,
        CancellationToken cancellationToken) =>
        _jsRuntime.InvokeVoidAsync("micRecorder.setMicGate", cancellationToken, sessionId, enabled);

    public ValueTask ReleaseAsync(string sessionId, CancellationToken cancellationToken) =>
        _jsRuntime.InvokeVoidAsync("micRecorder.release", cancellationToken, sessionId);

    public ValueTask<bool> IsLiveAsync(string sessionId, CancellationToken cancellationToken) =>
        _jsRuntime.InvokeAsync<bool>("micRecorder.isLive", cancellationToken, sessionId);

    public ValueTask<MicrophoneTrackProcessingDto> ReadTrackProcessingAsync(
        string sessionId,
        CancellationToken cancellationToken) =>
        _jsRuntime.InvokeAsync<MicrophoneTrackProcessingDto>(
            "micRecorder.trackProcessing",
            cancellationToken,
            sessionId);
}
