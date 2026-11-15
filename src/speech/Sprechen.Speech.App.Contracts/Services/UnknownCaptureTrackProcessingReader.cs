namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Einsatz wenn noch kein Erfassungspfad existiert oder der Browser nicht gefragt werden kann.</summary>
public sealed class UnknownCaptureTrackProcessingReader : ICaptureTrackProcessingReader
{
    public static UnknownCaptureTrackProcessingReader Instance { get; } = new();

    public ValueTask<CaptureTrackProcessing> ReadAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult(CaptureTrackProcessing.Unknown);
}
