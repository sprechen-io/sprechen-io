using Sprechen.Common.Cqrs;

namespace Sprechen.Service.Application.Cqrs.SpeechConversation;

/// <summary>
/// Fordert die Erkennung eines Audiostroms als Folge von Gesprächsereignissen an.
/// </summary>
public sealed record StreamSpeechConversationCommand(
    IAsyncEnumerable<byte[]> Chunks,
    AudioMimeType MimeType,
    string? LanguageCode,
    RecognitionProfile RecognitionProfile = RecognitionProfile.Conversational) : ICommand<IAsyncEnumerable<SpeechConversationEvent>>;
