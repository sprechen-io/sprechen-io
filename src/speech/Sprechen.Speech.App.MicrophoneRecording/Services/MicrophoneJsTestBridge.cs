using Microsoft.JSInterop;

namespace Sprechen.Speech.App.MicrophoneRecording.Services;

/// <summary>
/// Lässt Tests die bisherige Browserkennung ansprechen, ohne dass die Anwendung
/// selbst eine zweite Mikrofonimplementierung behält.
/// </summary>
internal static class MicrophoneJsTestBridge
{
    internal static Func<IJSRuntime, IJSRuntime>? Adapt { get; set; }

    internal static AsyncLocal<bool> Bypass { get; } = new();

    internal static AsyncLocal<DotNetObjectReference<MicrophoneRecording>?> PendingCallback { get; } = new();

    internal static IJSRuntime AdaptRuntime(IJSRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (Bypass.Value || Adapt is null)
        {
            return runtime;
        }

        return Adapt(runtime);
    }
}
