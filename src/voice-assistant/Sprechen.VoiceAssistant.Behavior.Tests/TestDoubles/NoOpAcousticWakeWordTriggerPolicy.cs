namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal sealed class NoOpAcousticWakeWordTriggerPolicy : IAcousticWakeWordTriggerPolicy
{
    public ValueTask<AcousticWakeWordEvidence> DetectAsync(
        AcousticWakeWordDetectorConfiguration configuration,
        IAsyncEnumerable<AcousticWakeWordScoreFrame> scoreFrames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(scoreFrames);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromException<AcousticWakeWordEvidence>(
            new AcousticWakeWordDetectionFailedException(
                $"Detector '{configuration.DetectorProfile.DetectorId}' did not produce test evidence."));
    }
}
