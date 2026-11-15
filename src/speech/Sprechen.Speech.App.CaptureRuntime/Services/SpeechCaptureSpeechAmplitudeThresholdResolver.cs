using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Löst die Sprachpegelschwelle aus Modell und optionaler Nutzereinstellung auf,
/// damit ein fehlender oder unlesbarer Gerätewert die modellierte Vorgabe nicht verdrängt.
/// </summary>
internal sealed class SpeechCaptureSpeechAmplitudeThresholdResolver
{
    internal const int Minimum = 50;

    private readonly ISpeechCaptureAmplitudeThresholdSettings? _amplitudeThresholdSettings;
    private readonly ILogger? _logger;

    public SpeechCaptureSpeechAmplitudeThresholdResolver(
        ISpeechCaptureAmplitudeThresholdSettings? amplitudeThresholdSettings = null,
        ILogger<SpeechCaptureSpeechAmplitudeThresholdResolver>? logger = null)
    {
        _amplitudeThresholdSettings = amplitudeThresholdSettings;
        _logger = logger;
    }

    public async Task<int> GetSpeechAmplitudeThresholdAsync(CancellationToken cancellationToken)
    {
        var modeled = ResolveModeled();
        if (_amplitudeThresholdSettings is null)
        {
            return modeled;
        }

        int userSetting;
        try
        {
            userSetting = await _amplitudeThresholdSettings
                .GetSpeechAmplitudeThresholdAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to load speech amplitude threshold. Falling back to the modeled value.");
            return modeled;
        }

        if (userSetting <= 0)
        {
            return modeled;
        }

        return Math.Max(userSetting, Minimum);
    }

    private static int ResolveModeled()
        => SpeechCaptureProfileCatalogReader.GetSegmenterProfile()?.SpeechAmplitudeThreshold ?? 350;
}
