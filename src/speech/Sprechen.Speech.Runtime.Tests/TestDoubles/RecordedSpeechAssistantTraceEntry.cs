namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed record RecordedSpeechAssistantTraceEntry(
    string CorrelationId,
    SpeechAssistantTraceScope Scope,
    SpeechAssistantTraceLevel Level,
    string Message);
