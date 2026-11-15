namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Stellt Sprachpegelschwelle und Einstellbereich getrennt von den Stilleeinstellungen bereit.
/// </summary>
public interface ISpeechCaptureAmplitudeThresholdSettings
{
    int SpeechAmplitudeThresholdMin { get; }

    int SpeechAmplitudeThresholdMax { get; }

    int SpeechAmplitudeThresholdStep { get; }

    int SpeechAmplitudeThresholdMid { get; }

    ValueTask<int> GetSpeechAmplitudeThresholdAsync(CancellationToken cancellationToken);

    ValueTask SetSpeechAmplitudeThresholdAsync(int speechAmplitudeThreshold, CancellationToken cancellationToken);

    ValueTask ResetSpeechAmplitudeThresholdAsync(CancellationToken cancellationToken);
}
