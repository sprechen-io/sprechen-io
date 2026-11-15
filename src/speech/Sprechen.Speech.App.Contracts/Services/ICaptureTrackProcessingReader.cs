namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Liest die angewendeten Verarbeitungsschalter des Erfassungspfads aus dem Browser.</summary>
public interface ICaptureTrackProcessingReader
{
    ValueTask<CaptureTrackProcessing> ReadAsync(CancellationToken cancellationToken);
}
