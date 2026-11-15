namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verbindet Blockverarbeitung mit Fehler- und Abbruchsignalen einer Aufnahme,
/// damit der Verteiler denselben Verbraucher über Daten und deren Ende informieren kann.
/// </summary>
internal interface ISpeechCaptureChunkConsumer
{
    Task ProcessChunkAsync(byte[] chunk, CancellationToken cancellationToken);

    void FailActiveCapture(Exception ex);

    void CancelActiveCapture(CaptureCancellationKind kind, CancellationToken cancellationToken);
}
