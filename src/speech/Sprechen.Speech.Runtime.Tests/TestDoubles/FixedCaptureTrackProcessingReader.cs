using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class FixedCaptureTrackProcessingReader(CaptureTrackProcessing processing)
    : ICaptureTrackProcessingReader
{
    public ValueTask<CaptureTrackProcessing> ReadAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult(processing);
}
