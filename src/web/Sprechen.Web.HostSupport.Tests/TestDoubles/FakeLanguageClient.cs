using Grpc.Core;
using Sprechen.Proto;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;

namespace Sprechen.Web.HostSupport.Tests.TestDoubles;

internal sealed class FakeLanguageClient : SpeechGrpcClient
{
    private readonly Func<TextRequest, CancellationToken, AsyncServerStreamingCall<AudioResponse>>? _textToSpeechToStream;
    private readonly Func<CancellationToken, AsyncClientStreamingCall<AudioChunk, SpeechTranscript>>? _speechToText;
    private readonly Func<CancellationToken, AsyncDuplexStreamingCall<AudioChunk, SpeechTranscript>>? _speechToTextStream;
    private readonly Func<CancellationToken, AsyncDuplexStreamingCall<AudioChunk, SpeechConversationEventMessage>>? _speechToConversationStream;
    private readonly Func<CancellationToken, AsyncUnaryCall<SupportedLanguagesResponse>>? _getSupportedLanguages;

    public Metadata? LastTextToSpeechHeaders { get; private set; }

    public Metadata? LastSpeechToTextHeaders { get; private set; }

    public Metadata? LastSpeechToTextStreamHeaders { get; private set; }

    public Metadata? LastSpeechToConversationStreamHeaders { get; private set; }

    public Metadata? LastSupportedLanguagesHeaders { get; private set; }

    public FakeLanguageClient(Func<TextRequest, CancellationToken, AsyncServerStreamingCall<AudioResponse>> textToSpeechToStream)
        : this(textToSpeechToStream, speechToText: null, speechToTextStream: null, speechToConversationStream: null)
    {
    }

    public FakeLanguageClient(
        Func<TextRequest, CancellationToken, AsyncServerStreamingCall<AudioResponse>>? textToSpeechToStream,
        Func<CancellationToken, AsyncClientStreamingCall<AudioChunk, SpeechTranscript>>? speechToText = null,
        Func<CancellationToken, AsyncDuplexStreamingCall<AudioChunk, SpeechTranscript>>? speechToTextStream = null,
        Func<CancellationToken, AsyncDuplexStreamingCall<AudioChunk, SpeechConversationEventMessage>>? speechToConversationStream = null,
        Func<CancellationToken, AsyncUnaryCall<SupportedLanguagesResponse>>? getSupportedLanguages = null)
    {
        _textToSpeechToStream = textToSpeechToStream;
        _speechToText = speechToText;
        _speechToTextStream = speechToTextStream;
        _speechToConversationStream = speechToConversationStream;
        _getSupportedLanguages = getSupportedLanguages;
    }

    public override AsyncServerStreamingCall<AudioResponse> TextToSpeechToStream(
        TextRequest request,
        Metadata? headers,
        DateTime? deadline,
        CancellationToken cancellationToken)
    {
        LastTextToSpeechHeaders = headers;
        _ = deadline;
        return _textToSpeechToStream?.Invoke(request, cancellationToken)
            ?? throw new NotSupportedException();
    }

    public override AsyncClientStreamingCall<AudioChunk, SpeechTranscript> SpeechToText(
        Metadata? headers = null,
        DateTime? deadline = null,
        CancellationToken cancellationToken = default)
    {
        LastSpeechToTextHeaders = headers;
        _ = deadline;
        return _speechToText?.Invoke(cancellationToken)
            ?? throw new NotSupportedException();
    }

    public override AsyncDuplexStreamingCall<AudioChunk, SpeechTranscript> SpeechToTextStream(
        Metadata? headers = null,
        DateTime? deadline = null,
        CancellationToken cancellationToken = default)
    {
        LastSpeechToTextStreamHeaders = headers;
        _ = deadline;
        return _speechToTextStream?.Invoke(cancellationToken)
            ?? throw new NotSupportedException();
    }

    public override AsyncDuplexStreamingCall<AudioChunk, SpeechConversationEventMessage> SpeechToConversationStream(
        Metadata? headers = null,
        DateTime? deadline = null,
        CancellationToken cancellationToken = default)
    {
        LastSpeechToConversationStreamHeaders = headers;
        _ = deadline;
        return _speechToConversationStream?.Invoke(cancellationToken)
            ?? throw new NotSupportedException();
    }

    public override AsyncUnaryCall<SupportedLanguagesResponse> GetSupportedLanguagesAsync(
        Google.Protobuf.WellKnownTypes.Empty request,
        Metadata? headers = null,
        DateTime? deadline = null,
        CancellationToken cancellationToken = default)
    {
        LastSupportedLanguagesHeaders = headers;
        _ = request;
        _ = deadline;
        return _getSupportedLanguages?.Invoke(cancellationToken)
            ?? throw new NotSupportedException();
    }
}
