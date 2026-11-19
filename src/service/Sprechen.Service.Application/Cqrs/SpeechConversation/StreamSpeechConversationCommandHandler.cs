using Monstertacle.SharpQrs;

namespace Sprechen.Service.Application.Cqrs.SpeechConversation;

/// <summary>
/// Leitet Audio und Erkennungsparameter an den Gesprächserkennungsdienst weiter.
/// </summary>
internal sealed class StreamSpeechConversationCommandHandler
    : ICommandHandler<StreamSpeechConversationCommand, IAsyncEnumerable<SpeechConversationEvent>>
{
    private readonly IConversationalSpeechToTextService _conversationalSpeechToText;

    public StreamSpeechConversationCommandHandler(IConversationalSpeechToTextService conversationalSpeechToText)
    {
        _conversationalSpeechToText = conversationalSpeechToText
            ?? throw new ArgumentNullException(nameof(conversationalSpeechToText));
    }

    public Task<IAsyncEnumerable<SpeechConversationEvent>> HandleAsync(
        StreamSpeechConversationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Chunks);

        var stream = _conversationalSpeechToText
            .StreamConversationAsync(command.Chunks, command.MimeType, cancellationToken, command.LanguageCode, command.RecognitionProfile);
        return Task.FromResult(stream);
    }
}
