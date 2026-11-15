using Microsoft.JSInterop;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Speichert das Farbschema im Browser und erhält bei fehlender JavaScript-Persistenz
/// die Systemeinstellung als nutzbaren Ausgangspunkt.
/// </summary>
internal sealed class ThemeSettings(IJSRuntime jsRuntime) : IThemeSettings
{
    private const string GetIdentifier = "themeSettings.get";
    private const string SetIdentifier = "themeSettings.set";

    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    public async ValueTask<ThemeMode> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var storedValue = await _jsRuntime.InvokeAsync<string?>(GetIdentifier, cancellationToken).ConfigureAwait(false);
            return storedValue.ToThemeMode();
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
        {
            return ThemeMode.System;
        }
    }

    public async ValueTask SetAsync(ThemeMode mode, CancellationToken cancellationToken)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(SetIdentifier, cancellationToken, mode.ToStorageValue()).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
        {
            // Ein Fehler der optionalen Farbschema-Persistenz darf die Einstellungsanzeige nicht unterbrechen.
        }
    }
}
