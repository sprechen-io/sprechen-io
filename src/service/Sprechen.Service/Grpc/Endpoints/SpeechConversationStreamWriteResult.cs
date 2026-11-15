namespace Sprechen.Service.Grpc;

/// <summary>
/// Enthält die Anzahl übertragener Gesprächsereignisse und nicht leerer Transkripte.
/// </summary>
internal readonly record struct SpeechConversationStreamWriteResult(
    int EventCount,
    int TranscriptCount);
