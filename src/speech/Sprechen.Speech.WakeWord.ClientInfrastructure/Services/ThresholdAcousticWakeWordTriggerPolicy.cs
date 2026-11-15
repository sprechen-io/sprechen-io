using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Prüft Detektorwerte gegen modellierte Konfidenzschwelle und Aufwärmzeit,
/// damit ein ausreichender Wert erst nach der Aufwärmphase als Aktivierung gilt.
/// </summary>
internal sealed class ThresholdAcousticWakeWordTriggerPolicy : IAcousticWakeWordTriggerPolicy
{
    public async ValueTask<AcousticWakeWordEvidence> DetectAsync(
        AcousticWakeWordDetectorConfiguration configuration,
        IAsyncEnumerable<AcousticWakeWordScoreFrame> scoreFrames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(scoreFrames);

        var warmupDeadlineUtc = DateTimeOffset.UtcNow.AddMilliseconds(
            Math.Max(0, configuration.DetectorProfile.WarmupMs));

        await foreach (var frame in scoreFrames.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (frame.ConfidenceScore < configuration.DetectorProfile.ConfidenceThreshold)
            {
                continue;
            }

            if (frame.ObservedAtUtc < warmupDeadlineUtc)
            {
                throw new AcousticWakeWordDetectorWarmupException(
                    $"Detector '{configuration.DetectorProfile.DetectorId}' is still warming up.");
            }

            return new AcousticWakeWordEvidence(
                frame.DetectorId,
                frame.ConfidenceScore,
                frame.Cursor);
        }

        throw new AcousticWakeWordDetectionFailedException(
            $"Detector '{configuration.DetectorProfile.DetectorId}' completed without producing a wake-word activation.");
    }
}
