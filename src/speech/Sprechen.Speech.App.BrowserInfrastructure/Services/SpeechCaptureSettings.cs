using Microsoft.JSInterop;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Bindet Einstellungen für Stilledauer und Sprachpegelschwelle an den Browser
/// und prüft Eingabegrenzen, damit gespeicherte Anpassungen im vorgesehenen Bereich bleiben.
/// </summary>
internal sealed class SpeechCaptureSettings(IJSRuntime jsRuntime) : ISpeechCaptureSilenceDebounceSettings, ISpeechCaptureAmplitudeThresholdSettings
{
    public int SilenceDebounceMsMin => SilenceDebounceMsMinValue;
    public int SilenceDebounceMsMax => SilenceDebounceMsMaxValue;
    public int SilenceDebounceMsStep => SilenceDebounceMsStepValue;
    public int SilenceDebounceMsMid => SilenceDebounceMsMidValue;

    public int SpeechAmplitudeThresholdMin => SpeechAmplitudeThresholdMinValue;
    public int SpeechAmplitudeThresholdMax => SpeechAmplitudeThresholdMaxValue;
    public int SpeechAmplitudeThresholdStep => SpeechAmplitudeThresholdStepValue;
    public int SpeechAmplitudeThresholdMid => SpeechAmplitudeThresholdMidValue;

    private const int SilenceDebounceMsMinValue = 500;
    private const int SilenceDebounceMsMaxValue = 6000;
    private const int SilenceDebounceMsStepValue = 100;
    private const int SilenceDebounceMsMidValue = 3000;

    private const int SpeechAmplitudeThresholdMinValue = 50;
    private const int SpeechAmplitudeThresholdMaxValue = 2000;
    private const int SpeechAmplitudeThresholdStepValue = 10;
    private const int SpeechAmplitudeThresholdMidValue = 350;

    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    public ValueTask<int> GetSilenceDebounceMsAsync(CancellationToken cancellationToken)
        => _jsRuntime.InvokeAsync<int>("speechCaptureSettings.getSilenceDebounceMs", cancellationToken);

    public ValueTask SetSilenceDebounceMsAsync(int silenceDebounceMs, CancellationToken cancellationToken)
    {
        if (silenceDebounceMs < SilenceDebounceMsMinValue || silenceDebounceMs > SilenceDebounceMsMaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(silenceDebounceMs),
                silenceDebounceMs,
                $"Silence debounce must be between {SilenceDebounceMsMinValue} and {SilenceDebounceMsMaxValue} ms.");
        }

        return _jsRuntime.InvokeVoidAsync("speechCaptureSettings.setSilenceDebounceMs", cancellationToken, silenceDebounceMs);
    }

    public ValueTask ResetSilenceDebounceMsAsync(CancellationToken cancellationToken)
        => _jsRuntime.InvokeVoidAsync("speechCaptureSettings.clearSilenceDebounceMs", cancellationToken);

    public ValueTask<int> GetSpeechAmplitudeThresholdAsync(CancellationToken cancellationToken)
        => _jsRuntime.InvokeAsync<int>("speechCaptureSettings.getSpeechAmplitudeThreshold", cancellationToken);

    public ValueTask SetSpeechAmplitudeThresholdAsync(int speechAmplitudeThreshold, CancellationToken cancellationToken)
    {
        if (speechAmplitudeThreshold < SpeechAmplitudeThresholdMinValue
            || speechAmplitudeThreshold > SpeechAmplitudeThresholdMaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(speechAmplitudeThreshold),
                speechAmplitudeThreshold,
                $"Speech amplitude threshold must be between {SpeechAmplitudeThresholdMinValue} and {SpeechAmplitudeThresholdMaxValue}.");
        }

        return _jsRuntime.InvokeVoidAsync(
            "speechCaptureSettings.setSpeechAmplitudeThreshold",
            cancellationToken,
            speechAmplitudeThreshold);
    }

    public ValueTask ResetSpeechAmplitudeThresholdAsync(CancellationToken cancellationToken)
        => _jsRuntime.InvokeVoidAsync("speechCaptureSettings.clearSpeechAmplitudeThreshold", cancellationToken);
}



