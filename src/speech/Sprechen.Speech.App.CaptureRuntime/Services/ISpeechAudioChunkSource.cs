namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verbindet asynchron lesbare Audioblöcke mit Format und Beendigung ihrer Quelle,
/// damit ein Verbraucher Datenstrom und Nutzungsdauer gemeinsam verwalten kann.
/// </summary>
public interface ISpeechAudioChunkSource : IAsyncDisposable
{
    SpeechAudioMimeType MimeType { get; }

    IAsyncEnumerable<byte[]> Chunks { get; }

    Task StopAsync(CancellationToken cancellationToken);
}
