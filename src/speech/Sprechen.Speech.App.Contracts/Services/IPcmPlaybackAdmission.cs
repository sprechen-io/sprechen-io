namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Meldet die erste bestätigte Aufnahme von Samples in den Wiedergabepuffer.</summary>
public interface IPcmPlaybackAdmission
{
    ValueTask EnqueuePcm16LeAsync(byte[] pcm16Le, Action onAccepted, CancellationToken cancellationToken);

    ValueTask DrainAsync(Action onAccepted, CancellationToken cancellationToken);
}
