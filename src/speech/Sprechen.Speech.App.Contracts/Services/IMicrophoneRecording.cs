namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Stellt Audioblöcke, Format und Abschluss einer Mikrofonaufnahme zur asynchronen Verarbeitung bereit.
/// </summary>
public interface IMicrophoneRecording : IAsyncDisposable
{
    string MimeType { get; }

    IAsyncEnumerable<byte[]> Chunks { get; }

    Task Completion { get; }
}
