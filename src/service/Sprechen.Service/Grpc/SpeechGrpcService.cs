using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Sprechen.Common.Cqrs;
using Sprechen.Service.Application.Cqrs.LanguageCatalog;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Stellt den gRPC-Sprachdienst bereit und delegiert Erkennung, Gesprächsströme und Synthese an die jeweiligen Endpunkte.
/// </summary>
internal sealed class SpeechGrpcService(
    IQueryDispatcher queryDispatcher,
    SpeechToTextEndpoint speechToTextEndpoint,
    SpeechToTextStreamEndpoint speechToTextStreamEndpoint,
    SpeechToConversationStreamEndpoint speechToConversationStreamEndpoint,
    TextToSpeechToStreamEndpoint textToSpeechToStreamEndpoint) : SprechenProto.Speech.SpeechBase
{
    private readonly IQueryDispatcher _queryDispatcher = queryDispatcher ?? throw new ArgumentNullException(nameof(queryDispatcher));
    private readonly SpeechToTextEndpoint _speechToTextEndpoint = speechToTextEndpoint ?? throw new ArgumentNullException(nameof(speechToTextEndpoint));
    private readonly SpeechToTextStreamEndpoint _speechToTextStreamEndpoint = speechToTextStreamEndpoint ?? throw new ArgumentNullException(nameof(speechToTextStreamEndpoint));
    private readonly SpeechToConversationStreamEndpoint _speechToConversationStreamEndpoint = speechToConversationStreamEndpoint ?? throw new ArgumentNullException(nameof(speechToConversationStreamEndpoint));
    private readonly TextToSpeechToStreamEndpoint _textToSpeechToStreamEndpoint = textToSpeechToStreamEndpoint ?? throw new ArgumentNullException(nameof(textToSpeechToStreamEndpoint));

    public override async Task<SprechenProto.SupportedLanguagesResponse> GetSupportedLanguages(Empty request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var result = await _queryDispatcher
            .DispatchAsync(new ListSupportedLanguagesQuery(), context.CancellationToken)
            .ConfigureAwait(false);

        var response = new SprechenProto.SupportedLanguagesResponse
        {
            DefaultIsoCode = result.DefaultIsoCode
        };

        foreach (var language in result.Languages)
        {
            response.Languages.Add(new SprechenProto.SupportedLanguage
            {
                IsoCode = language.IsoCode,
                DisplayName = language.DisplayName
            });
        }

        return response;
    }

    public override Task<SprechenProto.SpeechTranscript> SpeechToText(
        IAsyncStreamReader<SprechenProto.AudioChunk> requestStream,
        ServerCallContext context)
        => _speechToTextEndpoint.HandleAsync(requestStream, context);

    public override Task SpeechToTextStream(
        IAsyncStreamReader<SprechenProto.AudioChunk> requestStream,
        IServerStreamWriter<SprechenProto.SpeechTranscript> responseStream,
        ServerCallContext context)
        => _speechToTextStreamEndpoint.HandleAsync(requestStream, responseStream, context);

    public override Task SpeechToConversationStream(
        IAsyncStreamReader<SprechenProto.AudioChunk> requestStream,
        IServerStreamWriter<SprechenProto.SpeechConversationEventMessage> responseStream,
        ServerCallContext context)
        => _speechToConversationStreamEndpoint.HandleAsync(requestStream, responseStream, context);

    public override Task TextToSpeechToStream(
        SprechenProto.TextRequest request,
        IServerStreamWriter<SprechenProto.AudioResponse> responseStream,
        ServerCallContext context)
        => _textToSpeechToStreamEndpoint.HandleAsync(request, responseStream, context);
}
