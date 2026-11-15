using Microsoft.JSInterop;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Hält die Browserlautstärke von Sprachausgabe und Earcons zusammen,
/// damit beide Ausgabewege dieselbe Einstellung verwenden.
/// </summary>
internal sealed class AudioOutputSettings(IJSRuntime jsRuntime) : IAudioOutputSettings
{
    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    public ValueTask<float> GetVolumeAsync(CancellationToken cancellationToken)
        => _jsRuntime.InvokeAsync<float>("pcmPlayer.getVolume", cancellationToken);

    public async ValueTask SetVolumeAsync(float volume, CancellationToken cancellationToken)
    {
        if (float.IsNaN(volume) || float.IsInfinity(volume))
        {
            throw new ArgumentOutOfRangeException(nameof(volume), volume, "Volume must be a finite number.");
        }

        if (volume < 0f || volume > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), volume, "Volume must be between 0 and 1.");
        }

        await _jsRuntime.InvokeVoidAsync("pcmPlayer.setVolume", cancellationToken, volume);
        await _jsRuntime.InvokeVoidAsync("earcons.setVolume", cancellationToken, volume);
    }
}
