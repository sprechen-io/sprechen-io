namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Stellt Gesprächsereignisse einer laufenden Spracherkennung als asynchronen Ereignisstrom bereit.
/// </summary>
public interface ISpeechToTextStreamingSession : IStreamingSession
{
    IAsyncEnumerable<SpeechConversationEvent> Events { get; }
}
