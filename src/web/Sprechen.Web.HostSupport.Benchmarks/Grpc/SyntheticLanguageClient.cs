using Grpc.Core;
using Sprechen.Proto;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;

namespace Sprechen.Web.HostSupport.Benchmarks.Grpc;

internal sealed class SyntheticLanguageClient : SpeechGrpcClient
{
    private readonly IReadOnlyList<SpeechConversationEventMessage> _responseEvents;

    public SyntheticLanguageClient(IReadOnlyList<SpeechConversationEventMessage> responseEvents)
    {
        _responseEvents = responseEvents ?? throw new ArgumentNullException(nameof(responseEvents));
    }

    public override AsyncDuplexStreamingCall<AudioChunk, SpeechConversationEventMessage> SpeechToConversationStream(
        Metadata? headers = null,
        DateTime? deadline = null,
        CancellationToken cancellationToken = default)
    {
        _ = headers;
        _ = deadline;
        _ = cancellationToken;

        return new AsyncDuplexStreamingCall<AudioChunk, SpeechConversationEventMessage>(
            new SyntheticClientStreamWriter<AudioChunk>(),
            new SyntheticServerResponseStream<SpeechConversationEventMessage>(_responseEvents),
            Task.FromResult(new Metadata()),
            static () => new Status(StatusCode.OK, string.Empty),
            static () => new Metadata(),
            static () => { });
    }
}
