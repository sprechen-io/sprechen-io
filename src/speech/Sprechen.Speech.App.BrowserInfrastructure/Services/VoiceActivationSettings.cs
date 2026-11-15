using Microsoft.JSInterop;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Speichert benutzerdefinierte Aktivierungsformen im Browser und normalisiert
/// Lese- und Schreibwerte einheitlich für die spätere Aktivierungsprüfung.
/// </summary>
internal sealed class VoiceActivationSettings(IJSRuntime jsRuntime) : IVoiceActivationSettings
{
    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    public async ValueTask<string[]> GetWakeWordActivationFormsOverrideAsync(CancellationToken cancellationToken)
    {
        var raw = await _jsRuntime.InvokeAsync<string[]>(
            "voiceActivationSettings.getWakeWordActivationFormsOverride",
            cancellationToken);

        return new WakeWordActivationFormCodec().Normalize(raw);
    }

    public ValueTask SetWakeWordActivationFormsOverrideAsync(string[] wakeWordActivationForms, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wakeWordActivationForms);
        var normalized = new WakeWordActivationFormCodec().Normalize(wakeWordActivationForms);
        return _jsRuntime.InvokeVoidAsync("voiceActivationSettings.setWakeWordActivationFormsOverride", cancellationToken, normalized);
    }

    public ValueTask ClearWakeWordActivationFormsOverrideAsync(CancellationToken cancellationToken)
        => _jsRuntime.InvokeVoidAsync("voiceActivationSettings.clearWakeWordActivationFormsOverride", cancellationToken);
}

