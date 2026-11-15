using Sprechen.Common.Cqrs;

namespace Sprechen.Service.Application.Cqrs.SpeechTranscription;

/// <summary>
/// Fordert Gesprächsereignisse für die anschließende Transkriptprojektion an.
/// </summary>
public sealed record StreamSpeechTranscriptCommand(
    IAsyncEnumerable<byte[]> Chunks,
    AudioMimeType MimeType,
    string LanguageCode,
    RecognitionProfile RecognitionProfile = RecognitionProfile.Conversational) : ICommand<IAsyncEnumerable<SpeechConversationEvent>>;
