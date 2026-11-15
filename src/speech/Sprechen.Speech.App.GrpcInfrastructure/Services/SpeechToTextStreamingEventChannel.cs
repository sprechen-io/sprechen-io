using System.Threading.Channels;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Puffert ein Gesprächsereignis für einen Leser und einen Schreiber. Ein voller Kanal
/// hält Schreibvorgänge bis zum Lesen oder Abbruch zurück, damit Überlast Rückstau erzeugt
/// und keine gepufferten Gesprächsereignisse verdrängt.
/// </summary>
internal sealed class SpeechToTextStreamingEventChannel
{
    private static readonly BoundedChannelOptions EventChannelOptions = new(1)
    {
        SingleReader = true,
        SingleWriter = true,
        FullMode = BoundedChannelFullMode.Wait
    };

    private readonly Channel<SpeechConversationEvent> _channel = Channel.CreateBounded<SpeechConversationEvent>(EventChannelOptions);

    internal IAsyncEnumerable<SpeechConversationEvent> Events => _channel.Reader.ReadAllAsync();

    internal ValueTask WriteAsync(SpeechConversationEvent conversationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(conversationEvent);
        return _channel.Writer.WriteAsync(conversationEvent, cancellationToken);
    }

    internal void Complete()
    {
        _channel.Writer.TryComplete();
    }

    internal void Complete(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);

        _channel.Writer.TryComplete(error);
    }
}
