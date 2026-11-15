namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Stellt Stille-Entprellung und Einstellbereich getrennt von der Sprachpegelschwelle bereit.
/// </summary>
public interface ISpeechCaptureSilenceDebounceSettings
{
    int SilenceDebounceMsMin { get; }

    int SilenceDebounceMsMax { get; }

    int SilenceDebounceMsStep { get; }

    int SilenceDebounceMsMid { get; }

    ValueTask<int> GetSilenceDebounceMsAsync(CancellationToken cancellationToken);

    ValueTask SetSilenceDebounceMsAsync(int silenceDebounceMs, CancellationToken cancellationToken);

    ValueTask ResetSilenceDebounceMsAsync(CancellationToken cancellationToken);
}
