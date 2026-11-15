using Sprechen.Service.Abstractions.Conversation;

namespace Sprechen.Service.Abstractions;

/// <summary>
/// Anwendungsschnittstelle für die Erkennung von Audio als Gesprächsereignisstrom.
/// </summary>
public interface IConversationalSpeechToTextService
{
    IAsyncEnumerable<SpeechConversationEvent> StreamConversationAsync(
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        CancellationToken ct,
        string? languageCode,
        RecognitionProfile recognitionProfile = RecognitionProfile.Conversational);
}
