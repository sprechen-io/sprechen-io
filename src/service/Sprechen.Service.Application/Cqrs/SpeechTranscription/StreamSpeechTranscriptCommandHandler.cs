using Sprechen.Common.Cqrs;

namespace Sprechen.Service.Application.Cqrs.SpeechTranscription;

/// <summary>
/// Prüft den Sprachcode und leitet die Anfrage an den Gesprächserkennungsdienst weiter.
/// </summary>
internal sealed class StreamSpeechTranscriptCommandHandler
    : ICommandHandler<StreamSpeechTranscriptCommand, IAsyncEnumerable<SpeechConversationEvent>>
{
    private readonly IConversationalSpeechToTextService _conversationalSpeechToText;

    public StreamSpeechTranscriptCommandHandler(IConversationalSpeechToTextService conversationalSpeechToText)
    {
        _conversationalSpeechToText = conversationalSpeechToText
            ?? throw new ArgumentNullException(nameof(conversationalSpeechToText));
    }

    public Task<IAsyncEnumerable<SpeechConversationEvent>> HandleAsync(
        StreamSpeechTranscriptCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Chunks);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.LanguageCode);

        var stream = _conversationalSpeechToText
            .StreamConversationAsync(command.Chunks, command.MimeType, cancellationToken, command.LanguageCode, command.RecognitionProfile);
        return Task.FromResult(stream);
    }
}
