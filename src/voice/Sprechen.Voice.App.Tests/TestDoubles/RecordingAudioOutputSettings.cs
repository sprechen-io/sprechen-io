namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class RecordingAudioOutputSettings : IAudioOutputSettings
{
    public float Volume { get; set; } = 1.0f;

    public Exception? GetVolumeException { get; set; }

    public Exception? SetVolumeException { get; set; }

    public List<float> SetVolumes { get; } = [];

    public ValueTask<float> GetVolumeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (GetVolumeException is not null)
        {
            return ValueTask.FromException<float>(GetVolumeException);
        }

        return ValueTask.FromResult(Volume);
    }

    public ValueTask SetVolumeAsync(float volume, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (SetVolumeException is not null)
        {
            return ValueTask.FromException(SetVolumeException);
        }

        Volume = volume;
        SetVolumes.Add(volume);
        return ValueTask.CompletedTask;
    }
}
