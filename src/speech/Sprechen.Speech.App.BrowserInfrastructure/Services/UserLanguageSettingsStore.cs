using Microsoft.JSInterop;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Kapselt die Browserpersistenz der Spracheinstellungen, damit eine ausgefallene
/// JavaScript-Speicheranbindung Sitzungsstart und Einstellungsanzeige nicht unterbricht.
/// </summary>
internal sealed class UserLanguageSettingsStore(IJSRuntime jsRuntime) : IUserLanguageSettingsStore
{
    private const string GetIdentifier = "userLanguageSettings.get";
    private const string SetIdentifier = "userLanguageSettings.set";
    private const string SetEntryOfferHeardIdentifier = "userLanguageSettings.setEntryOfferHeard";

    private static readonly UserLanguageSettings EmptySettings = new(null, null);

    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    public ValueTask<UserLanguageSettings> GetAsync(CancellationToken cancellationToken)
        => TryInvokeAsync(GetIdentifier, EmptySettings, cancellationToken);

    public ValueTask SetAsync(UserLanguageSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return TryInvokeVoidAsync(SetIdentifier, cancellationToken, settings);
    }

    public ValueTask SetEntryOfferHeardAsync(string isoCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        // Das einzelne Feld wird direkt im Browserdatensatz ergänzt. C# liest vorher nicht,
        // damit eine fehlgeschlagene Abfrage keine übrigen Einstellungen überschreiben kann.
        return TryInvokeVoidAsync(SetEntryOfferHeardIdentifier, cancellationToken, isoCode);
    }

    private async ValueTask<T> TryInvokeAsync<T>(
        string identifier,
        T fallbackValue,
        CancellationToken cancellationToken,
        params object?[] args)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<T>(identifier, cancellationToken, args).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
        {
            return fallbackValue;
        }
    }

    private async ValueTask TryInvokeVoidAsync(
        string identifier,
        CancellationToken cancellationToken,
        params object?[] args)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
        {
            // Ein Fehler der optionalen Sitzungspersistenz darf Start und Einstellungsanzeige nicht unterbrechen.
        }
    }
}
