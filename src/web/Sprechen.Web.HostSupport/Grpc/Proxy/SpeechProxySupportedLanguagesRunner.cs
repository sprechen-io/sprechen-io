using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Sprechen.Proto;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;

namespace Sprechen.Web.HostSupport.Grpc.Proxy;

/// <summary>
/// Leitet die Abfrage unterstützter Sprachen an den Sprachdienst weiter.
/// Backendfehler werden weitergegeben. Nur ein angeforderter Aufruferabbruch liefert eine leere Ersatzantwort.
/// </summary>
internal sealed class SpeechProxySupportedLanguagesRunner
{
    private readonly SpeechGrpcClient _backendClient;

    public SpeechProxySupportedLanguagesRunner(SpeechGrpcClient backendClient)
    {
        _backendClient = backendClient ?? throw new ArgumentNullException(nameof(backendClient));
    }

    public async Task<SupportedLanguagesResponse> GetSupportedLanguagesAsync(
        Empty request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _backendClient
                .GetSupportedLanguagesAsync(request, cancellationToken: cancellationToken)
                .ResponseAsync
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (IsCallerGone(exception, cancellationToken))
        {

            return new SupportedLanguagesResponse();
        }
    }

    private static bool IsCallerGone(Exception exception, CancellationToken cancellationToken)
        => cancellationToken.IsCancellationRequested
            && exception is OperationCanceledException or RpcException { StatusCode: StatusCode.Cancelled };
}
