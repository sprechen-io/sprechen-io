namespace Sprechen.Service.Core.Tests;

internal sealed class ThrowingConversationalSpeechToTextProvider : IConversationalSpeechToTextProvider
{
    public IAsyncEnumerable<SpeechConversationEvent> StreamConversationAsync(
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        CancellationToken ct,
        string? languageCode,
        RecognitionProfile recognitionProfile = RecognitionProfile.Conversational) =>
        throw new InvalidOperationException("Conversational speech-to-text provider should not be used in this scenario.");
}
