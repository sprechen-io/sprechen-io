using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Liest bevorzugte Browsersprachen für die Sitzungsauswahl und nutzt bei
/// fehlenden Browserangaben die aktuellen Kulturen als weitere Sprachquelle.
/// </summary>
public sealed class BrowserLanguageProvider(
    IJSRuntime jsRuntime,
    ILogger<BrowserLanguageProvider> logger) : IBrowserLanguageProvider
{
    private const string GetPreferredLanguageTagsIdentifier = "userLanguageSettings.getPreferredLanguageTags";

    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    private readonly ILogger<BrowserLanguageProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask<IReadOnlyList<string>> GetPreferredLanguageTagsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var browserTags = await _jsRuntime
                .InvokeAsync<string[]?>(GetPreferredLanguageTagsIdentifier, cancellationToken)
                .ConfigureAwait(false);
            var browserLanguageTags = NormalizeLanguageTags(
                browserTags ?? Array.Empty<string>());

            if (browserLanguageTags.Length > 0)
            {
                _logger.LogDebug(
                    "Resolved browser preferred language tags from JS interop: {LanguageTags}.",
                    string.Join(", ", browserLanguageTags));
                return browserLanguageTags;
            }
        }
        catch (JSException ex)
        {
            _logger.LogDebug(ex, "Reading browser preferred language tags via JS interop failed.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogDebug(ex, "Reading browser preferred language tags via JS interop is currently unavailable.");
        }

        var cultureFallbackTags = NormalizeLanguageTags(
            [
                CultureInfo.CurrentUICulture.Name,
                CultureInfo.CurrentCulture.Name
            ]);
        if (cultureFallbackTags.Length > 0)
        {
            _logger.LogDebug(
                "Falling back to current culture tags for browser language detection: {LanguageTags}.",
                string.Join(", ", cultureFallbackTags));
            return cultureFallbackTags;
        }

        _logger.LogDebug("No browser or culture language tags were available.");
        return Array.Empty<string>();
    }

    private static string[] NormalizeLanguageTags(IEnumerable<string?> languageTags)
    {
        ArgumentNullException.ThrowIfNull(languageTags);

        return languageTags
            .Where(languageTag => !string.IsNullOrWhiteSpace(languageTag))
            .Select(languageTag => languageTag!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
