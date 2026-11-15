using Sprechen.Common.Cqrs;

namespace Sprechen.Service.Application.Cqrs.SpeechSynthesis;

/// <summary>
/// Bindet Syntheseaufträge an den Sprachausgabedienst, damit Audioabschnitte nach der
/// Eingabeprüfung als Strom weitergereicht werden.
/// </summary>
internal sealed class SynthesizeSpeechCommandHandler
    : ICommandHandler<SynthesizeSpeechCommand, IAsyncEnumerable<AudioSynthesisChunk>>
{
    private readonly ITextToSpeechService _textToSpeech;

    public SynthesizeSpeechCommandHandler(ITextToSpeechService textToSpeech)
    {
        _textToSpeech = textToSpeech ?? throw new ArgumentNullException(nameof(textToSpeech));
    }

    public Task<IAsyncEnumerable<AudioSynthesisChunk>> HandleAsync(
        SynthesizeSpeechCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Input);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Input.Text);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Input.LanguageCode);

        var stream = _textToSpeech.SynthesizeAsync(
            command.Input.Text,
            command.Input.DesiredMimeType,
            command.Input.SpeechRate,
            command.Input.LanguageCode,
            command.Input.PolitenessLevel,
            cancellationToken);
        return Task.FromResult(stream);
    }
}
