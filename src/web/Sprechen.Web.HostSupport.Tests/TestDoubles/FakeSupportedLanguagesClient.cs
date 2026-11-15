using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Sprechen.Proto;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;

namespace Sprechen.Web.HostSupport.Tests.TestDoubles;

internal sealed class FakeSupportedLanguagesClient : SpeechGrpcClient
{
    private readonly Func<Empty, CancellationToken, SupportedLanguagesResponse> _handler;

    public FakeSupportedLanguagesClient(Func<Empty, CancellationToken, SupportedLanguagesResponse> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public override AsyncUnaryCall<SupportedLanguagesResponse> GetSupportedLanguagesAsync(
        Empty request,
        Metadata? headers,
        DateTime? deadline,
        CancellationToken cancellationToken)
    {
        _ = headers;
        _ = deadline;

        var response = _handler(request, cancellationToken);

        return new AsyncUnaryCall<SupportedLanguagesResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            static () => new Status(StatusCode.OK, string.Empty),
            static () => new Metadata(),
            static () => { });
    }
}


