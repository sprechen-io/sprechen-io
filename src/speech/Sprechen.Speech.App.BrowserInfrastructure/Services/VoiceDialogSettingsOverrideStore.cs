using Microsoft.JSInterop;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Kapselt optionale Dialogüberschreibungen pro Gerät und Anwendung,
/// damit fehlende Browserpersistenz als fehlende Präferenz behandelt werden kann.
/// </summary>
internal sealed class VoiceDialogSettingsOverrideStore(IJSRuntime jsRuntime) : IVoiceDialogSettingsOverrideStore
{
    private const string GetDeviceAllowBargeInOverrideIdentifier = "voiceDialogSettingsOverrides.getDeviceAllowBargeInOverride";
    private const string SetDeviceAllowBargeInOverrideIdentifier = "voiceDialogSettingsOverrides.setDeviceAllowBargeInOverride";
    private const string GetApplicationAllowBargeInOverridesIdentifier = "voiceDialogSettingsOverrides.getApplicationAllowBargeInOverrides";
    private const string GetApplicationAllowBargeInOverrideIdentifier = "voiceDialogSettingsOverrides.getApplicationAllowBargeInOverride";
    private const string SetApplicationAllowBargeInOverrideIdentifier = "voiceDialogSettingsOverrides.setApplicationAllowBargeInOverride";
    private const string GetDeviceAutoAdvanceAfterCommitOverrideIdentifier = "voiceDialogSettingsOverrides.getDeviceAutoAdvanceAfterCommitOverride";
    private const string SetDeviceAutoAdvanceAfterCommitOverrideIdentifier = "voiceDialogSettingsOverrides.setDeviceAutoAdvanceAfterCommitOverride";
    private const string GetApplicationAutoAdvanceAfterCommitOverridesIdentifier = "voiceDialogSettingsOverrides.getApplicationAutoAdvanceAfterCommitOverrides";
    private const string GetApplicationAutoAdvanceAfterCommitOverrideIdentifier = "voiceDialogSettingsOverrides.getApplicationAutoAdvanceAfterCommitOverride";
    private const string SetApplicationAutoAdvanceAfterCommitOverrideIdentifier = "voiceDialogSettingsOverrides.setApplicationAutoAdvanceAfterCommitOverride";

    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    public ValueTask<bool?> GetDeviceAllowBargeInOverrideAsync(CancellationToken cancellationToken)
        => TryInvokeAsync(GetDeviceAllowBargeInOverrideIdentifier, default(bool?), cancellationToken);

    public ValueTask SetDeviceAllowBargeInOverrideAsync(bool? allowBargeIn, CancellationToken cancellationToken)
        => TryInvokeVoidAsync(SetDeviceAllowBargeInOverrideIdentifier, cancellationToken, allowBargeIn);

    public async ValueTask<IReadOnlyDictionary<string, bool>> GetApplicationAllowBargeInOverridesAsync(CancellationToken cancellationToken)
    {
        var overrides = await TryInvokeAsync<Dictionary<string, bool>?>(
                GetApplicationAllowBargeInOverridesIdentifier,
                default(Dictionary<string, bool>?),
                cancellationToken)
            .ConfigureAwait(false);

        return overrides is null
            ? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, bool>(overrides, StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<bool?> GetApplicationAllowBargeInOverrideAsync(string applicationId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);

        return TryInvokeAsync(
            GetApplicationAllowBargeInOverrideIdentifier,
            default(bool?),
            cancellationToken,
            applicationId.Trim());
    }

    public ValueTask SetApplicationAllowBargeInOverrideAsync(
        string applicationId,
        bool? allowBargeIn,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);

        return TryInvokeVoidAsync(
            SetApplicationAllowBargeInOverrideIdentifier,
            cancellationToken,
            applicationId.Trim(),
            allowBargeIn);
    }

    public ValueTask<bool?> GetDeviceAutoAdvanceAfterCommitOverrideAsync(CancellationToken cancellationToken)
        => TryInvokeAsync(GetDeviceAutoAdvanceAfterCommitOverrideIdentifier, default(bool?), cancellationToken);

    public ValueTask SetDeviceAutoAdvanceAfterCommitOverrideAsync(bool? autoAdvanceAfterCommit, CancellationToken cancellationToken)
        => TryInvokeVoidAsync(SetDeviceAutoAdvanceAfterCommitOverrideIdentifier, cancellationToken, autoAdvanceAfterCommit);

    public async ValueTask<IReadOnlyDictionary<string, bool>> GetApplicationAutoAdvanceAfterCommitOverridesAsync(CancellationToken cancellationToken)
    {
        var overrides = await TryInvokeAsync<Dictionary<string, bool>?>(
                GetApplicationAutoAdvanceAfterCommitOverridesIdentifier,
                default(Dictionary<string, bool>?),
                cancellationToken)
            .ConfigureAwait(false);

        return overrides is null
            ? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, bool>(overrides, StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<bool?> GetApplicationAutoAdvanceAfterCommitOverrideAsync(string applicationId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);

        return TryInvokeAsync(
            GetApplicationAutoAdvanceAfterCommitOverrideIdentifier,
            default(bool?),
            cancellationToken,
            applicationId.Trim());
    }

    public ValueTask SetApplicationAutoAdvanceAfterCommitOverrideAsync(
        string applicationId,
        bool? autoAdvanceAfterCommit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);

        return TryInvokeVoidAsync(
            SetApplicationAutoAdvanceAfterCommitOverrideIdentifier,
            cancellationToken,
            applicationId.Trim(),
            autoAdvanceAfterCommit);
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
            // Fehlende JavaScript-Speicherfunktionen dürfen den Dialog nicht unterbrechen.
        }
    }
}

