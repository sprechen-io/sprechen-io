namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Hält Abtastrate und Samplebreite als PCM-Vorgaben zusammen,
/// damit Audioverarbeitung dieselben Ausgangswerte für Zeit- und Größenberechnung verwenden kann.
/// </summary>
public static class PcmAudioDefaults
{
    public const int DEFAULT_SAMPLE_RATE_HZ = 24_000;
    public const int PCM16LE_BYTES_PER_SAMPLE = 2;
}


