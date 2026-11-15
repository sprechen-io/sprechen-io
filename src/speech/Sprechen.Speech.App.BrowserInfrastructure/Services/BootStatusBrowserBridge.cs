using Microsoft.JSInterop;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Meldet dem Browser die Betriebsbereitschaft der Anwendung,
/// damit er den Ladezustand beenden kann.
/// </summary>
public sealed class BootStatusBrowserBridge(IJSRuntime jsRuntime) : IBootStatusBrowserBridge
{
    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    public Task MarkApplicationReadyAsync()
    {
        return _jsRuntime.InvokeVoidAsync("sprechenIoWeb.boot.ready").AsTask();
    }
}
