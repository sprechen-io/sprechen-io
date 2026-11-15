using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Sprechen.Service.Core.Services;

/// <summary>
/// Leitet Gesprächsereignisse des Anbieters weiter und ergänzt Ereignisse für Abschluss, Abbruch oder Fehler.
/// </summary>
internal sealed class SpeechConversationStreamOrchestrator
{
    private readonly IConversationalSpeechToTextProvider _conversationalSttProvider;
    private readonly ISupportedLanguagesService _supportedLanguages;

    public SpeechConversationStreamOrchestrator(
        IConversationalSpeechToTextProvider conversationalSttProvider,
        ISupportedLanguagesService supportedLanguages)
    {
        _conversationalSttProvider = conversationalSttProvider ?? throw new ArgumentNullException(nameof(conversationalSttProvider));
        _supportedLanguages = supportedLanguages ?? throw new ArgumentNullException(nameof(supportedLanguages));
    }

    public async IAsyncEnumerable<SpeechConversationEvent> StreamAsync(
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        string? languageCode,
        RecognitionProfile recognitionProfile = RecognitionProfile.Conversational)
    {
        var channel = Channel.CreateUnbounded<SpeechConversationEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        _ = RunAsync(channel.Writer, chunks, mime, cancellationToken, languageCode, recognitionProfile);

        await foreach (var conversationEvent in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return conversationEvent;
        }
    }

    private async Task RunAsync(
        ChannelWriter<SpeechConversationEvent> writer,
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        CancellationToken cancellationToken,
        string? languageCode,
        RecognitionProfile recognitionProfile)
    {
        try
        {
            await foreach (var conversationEvent in _conversationalSttProvider.StreamConversationAsync(
                chunks,
                mime,
                cancellationToken,
                _supportedLanguages.GetEffectiveIsoCode(languageCode),
                recognitionProfile).WithCancellation(cancellationToken))
            {
                await writer.WriteAsync(conversationEvent, cancellationToken).ConfigureAwait(false);
            }

            writer.TryWrite(new SpeechConversationCompletedEvent());
            writer.TryComplete();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            writer.TryWrite(new SpeechConversationCancelledEvent());
            writer.TryComplete();
        }
        catch (Exception ex)
        {
            writer.TryWrite(new SpeechConversationFailedEvent(
                ErrorCode: "provider_failure",
                Detail: ex.Message));
            writer.TryComplete();
        }
    }
}
