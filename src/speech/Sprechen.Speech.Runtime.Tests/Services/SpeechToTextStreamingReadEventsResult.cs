namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed record SpeechToTextStreamingReadEventsResult(IReadOnlyList<SpeechConversationEvent> Events, Exception? Exception);
