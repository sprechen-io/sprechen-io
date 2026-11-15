using System.Runtime.CompilerServices;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal sealed class ThrowingAcousticWakeWordScorer : IAcousticWakeWordScorer
{
    public IAsyncEnumerable<AcousticWakeWordScoreFrame> ScoreAsync(
        AcousticWakeWordDetectorConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return ScoreUnavailableAsync(configuration, cancellationToken);
    }

    private static async IAsyncEnumerable<AcousticWakeWordScoreFrame> ScoreUnavailableAsync(
        AcousticWakeWordDetectorConfiguration configuration,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();

        throw new AcousticWakeWordDetectorUnavailableException(
            $"No acoustic wake-word scorer runtime is available for detector '{configuration.DetectorProfile.DetectorId}'.");

#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}
