using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Testing;
using Sprechen.Web.HostSupport.Grpc.Adapter;
using Sprechen.Web.HostSupport.Tests.TestDoubles;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

/// <summary>
/// Unterscheidet eine fehlgeschlagene Sprachabfrage von einer erfolgreich gelieferten leeren Liste.
/// Verbraucher können dadurch Fehler erneut abfragen, ohne sie mit einem gültigen leeren Ergebnis zu verwechseln.
/// </summary>
public sealed class SpeechProxySupportedLanguagesTerminalTests
{
    [Fact]
    public async Task BackendCancellation_WithTheCallerStillConnected_IsNotAnsweredWithAnEmptyLanguageList()
    {
        var proxy = CreateProxy(Task.FromException<SprechenProto.SupportedLanguagesResponse>(
            new RpcException(new Status(StatusCode.Cancelled, "backend cancelled"))));

        var thrown = await Record.ExceptionAsync(() =>
            proxy.GetSupportedLanguages(new Empty(), CreateContext(CancellationToken.None)));


        Assert.NotNull(thrown);
        Assert.IsType<RpcException>(thrown);
    }

    [Fact]
    public async Task CompletedLookup_WithAnEmptyList_IsPassedThroughUnchanged()
    {
        // Erfolgreiche Aufrufe mit leerer Liste bleiben gültig.
        var proxy = CreateProxy(Task.FromResult(new SprechenProto.SupportedLanguagesResponse()));

        var response = await proxy.GetSupportedLanguages(new Empty(), CreateContext(CancellationToken.None));

        Assert.NotNull(response);
        Assert.Empty(response.Languages);
    }

    [Fact]
    public async Task CompletedLookup_PassesTheLanguagesThrough()
    {
        var backendResponse = new SprechenProto.SupportedLanguagesResponse { DefaultIsoCode = "de" };
        backendResponse.Languages.Add(new SprechenProto.SupportedLanguage { IsoCode = "de", DisplayName = "Deutsch" });
        backendResponse.Languages.Add(new SprechenProto.SupportedLanguage { IsoCode = "it", DisplayName = "Italiano" });
        var proxy = CreateProxy(Task.FromResult(backendResponse));

        var response = await proxy.GetSupportedLanguages(new Empty(), CreateContext(CancellationToken.None));

        Assert.Equal("de", response.DefaultIsoCode);
        Assert.Equal(["de", "it"], response.Languages.Select(language => language.IsoCode));
    }

    private static SpeechProxyService CreateProxy(Task<SprechenProto.SupportedLanguagesResponse> response)
        => new(
            new FakeLanguageClient(
                textToSpeechToStream: null,
                getSupportedLanguages: _ => new AsyncUnaryCall<SprechenProto.SupportedLanguagesResponse>(
                    response,
                    Task.FromResult(new Metadata()),
                    static () => new Status(StatusCode.OK, string.Empty),
                    static () => new Metadata(),
                    static () => { })),
            new RecordingLogger<SpeechProxyService>());

    private static ServerCallContext CreateContext(CancellationToken cancellationToken)
        => TestServerCallContext.Create(
            "GetSupportedLanguages",
            null,
            DateTime.UtcNow.AddMinutes(1),
            new Metadata(),
            cancellationToken,
            "127.0.0.1",
            null,
            null,
            _ => Task.CompletedTask,
            () => new WriteOptions(),
            _ => { });
}
