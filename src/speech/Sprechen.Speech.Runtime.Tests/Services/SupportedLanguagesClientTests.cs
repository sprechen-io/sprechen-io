using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Moq;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;
using ProtoSupportedLanguagesResponse = Sprechen.Proto.SupportedLanguagesResponse;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SupportedLanguagesClientTests
{
    [Fact]
    public async Task GetAsync_WhenCalledMultipleTimes_CachesResponseAndReturnsClone()
    {
        var response = new ProtoSupportedLanguagesResponse { DefaultIsoCode = "de" };
        response.Languages.Add(new Sprechen.Proto.SupportedLanguage { IsoCode = "de", DisplayName = "Deutsch" });

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.GetSupportedLanguagesAsync(
                It.IsAny<Empty>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(CreateUnaryCall(response));

        var sut = new SupportedLanguagesClient(grpcClient.Object);

        var first = await sut.GetAsync(CancellationToken.None);
        var second = await sut.GetAsync(CancellationToken.None);

        Assert.NotSame(first, second);
        Assert.NotSame(first.Languages, second.Languages);
        Assert.Equal(first.DefaultIsoCode, second.DefaultIsoCode);
        Assert.Single(second.Languages);
        Assert.Equal("de", second.Languages[0].IsoCode);
        Assert.Equal("Deutsch", second.Languages[0].DisplayName);

        grpcClient.Verify(
            client => client.GetSupportedLanguagesAsync(
                It.IsAny<Empty>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AsyncUnaryCall<T> CreateUnaryCall<T>(T response) where T : class
    {
        return new AsyncUnaryCall<T>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }
}
