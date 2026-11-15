using Sprechen.Service.Abstractions.Conversation;

namespace Sprechen.Service.Abstractions;

/// <summary>
/// Anbieterschnittstelle für die Erkennung von Audio als Gesprächsereignisstrom.
/// </summary>
public interface IConversationalSpeechToTextProvider
{
    IAsyncEnumerable<SpeechConversationEvent> StreamConversationAsync(
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        CancellationToken ct,
        string? languageCode,
        RecognitionProfile recognitionProfile = RecognitionProfile.Conversational);
}
