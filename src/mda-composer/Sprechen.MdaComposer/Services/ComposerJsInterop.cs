using Microsoft.JSInterop;

namespace Sprechen.MdaComposer.Services;

/// <summary>
/// Bündelt JavaScript-Aufrufe für Browserfunktionen des Composers, etwa Zwischenablage, Downloads und lokalen Speicher.
/// </summary>
public sealed class ComposerJsInterop
{
    private const string Ns = "composerInterop";

    private readonly IJSRuntime _js;

    public ComposerJsInterop(IJSRuntime js)
    {
        _js = js;
    }

    public ValueTask CopyToClipboardAsync(string text)
        => _js.InvokeVoidAsync($"{Ns}.copyToClipboard", text);

    public ValueTask DownloadJsonAsync(string filename, string content)
        => _js.InvokeVoidAsync($"{Ns}.downloadJson", filename, content);

    public ValueTask RegisterKeyboardShortcutsAsync<T>(DotNetObjectReference<T> dotNetRef) where T : class
        => _js.InvokeVoidAsync($"{Ns}.registerKeyboardShortcuts", dotNetRef);

    public ValueTask UnregisterKeyboardShortcutsAsync()
        => _js.InvokeVoidAsync($"{Ns}.unregisterKeyboardShortcuts");

    public ValueTask TriggerFileInputAsync(string inputId)
        => _js.InvokeVoidAsync($"{Ns}.triggerFileInput", inputId);

    public ValueTask SaveStateAsync(string key, string value)
        => _js.InvokeVoidAsync($"{Ns}.saveState", key, value);

    public ValueTask<string?> LoadStateAsync(string key)
        => _js.InvokeAsync<string?>($"{Ns}.loadState", key);

    public ValueTask RemoveStateAsync(string key)
        => _js.InvokeVoidAsync($"{Ns}.removeState", key);

    public ValueTask SetDirtyFlagAsync(bool isDirty)
        => _js.InvokeVoidAsync($"{Ns}.setDirtyFlag", isDirty);
}
