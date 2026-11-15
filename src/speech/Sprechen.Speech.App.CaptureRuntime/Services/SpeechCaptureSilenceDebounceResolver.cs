using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Bestimmt die Stille-Entprellung aus Nutzereinstellung und modelliertem Mindestwert.
/// Eine längere Nutzereinstellung bleibt dabei erhalten.
/// </summary>
internal sealed class SpeechCaptureSilenceDebounceResolver
{
    private readonly ISpeechCaptureSilenceDebounceSettings _silenceDebounceSettings;
    private readonly ILogger _logger;

    public SpeechCaptureSilenceDebounceResolver(
        ISpeechCaptureSilenceDebounceSettings silenceDebounceSettings,
        ILogger<SpeechCaptureSilenceDebounceResolver> logger)
    {
        _silenceDebounceSettings = silenceDebounceSettings ?? throw new ArgumentNullException(nameof(silenceDebounceSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Liest die Nutzereinstellung vor Anwendung des modusspezifischen Mindestwerts.
    /// Bei einem Lesefehler wird der Standardwert verwendet.
    /// </summary>
    private async Task<int> GetUserSettingMsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _silenceDebounceSettings.GetSilenceDebounceMsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load silence debounce settings. Falling back to model/default.");
            return ResolveDefault();
        }
    }

    /// <summary>
    /// Verwendet den größeren Wert aus Nutzereinstellung und modellierter Stille-Entprellung.
    /// Fehlt der Wert des Modus, wird die Vorgabe für Listening herangezogen.
    /// </summary>
    public async Task<int> GetSilenceDebounceMsAsync(SpeechCaptureMode mode, CancellationToken cancellationToken)
    {
        var userSetting = await GetUserSettingMsAsync(cancellationToken).ConfigureAwait(false);

        // Ohne modusspezifischen Wert gilt Listening. Fehlt auch dessen Vorgabe,
        // liefert ResolveDefault den Wert 0.
        var modeledFloor = SpeechCaptureProfileCatalogReader.GetSilenceDebounceMs(mode) ?? ResolveDefault();
        return Math.Max(userSetting, modeledFloor);
    }

    private int ResolveDefault()
        => SpeechCaptureProfileCatalogReader.GetSilenceDebounceMs(SpeechCaptureMode.Listening) ?? 0;
}
