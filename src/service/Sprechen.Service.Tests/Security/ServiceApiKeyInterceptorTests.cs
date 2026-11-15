using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.Options;
using Sprechen.Proto.Transport;
using Sprechen.Service.Options;
using Sprechen.Service.Security;
using Sprechen.Service.Tests.TestDoubles;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Security;

public sealed class ServiceApiKeyInterceptorTests
{
    [Fact]
    public async Task UnaryServerHandler_MissingApiKey_ReturnsCodedUnauthenticated()
    {
        var interceptor = new ServiceApiKeyInterceptor(
            Microsoft.Extensions.Options.Options.Create(new ServiceAuthOptions { ApiKey = "expected" }));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.UnaryServerHandler(
                request: new object(),
                context: CreateContext("Unary", requestHeaders: new Metadata()),
                continuation: static (_, _) => Task.FromResult(new Empty())));

        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var code));
        Assert.Equal(SprechenProto.SprechenErrorCode.AuthMissingApiKey, code);
    }

    [Fact]
    public async Task UnaryServerHandler_InvalidApiKey_ReturnsCodedUnauthenticated()
    {
        var interceptor = new ServiceApiKeyInterceptor(
            Microsoft.Extensions.Options.Options.Create(new ServiceAuthOptions { ApiKey = "expected" }));

        var headers = new Metadata { { "x-api-key", "wrong" } };

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.UnaryServerHandler(
                request: new object(),
                context: CreateContext("Unary", requestHeaders: headers),
                continuation: static (_, _) => Task.FromResult(new Empty())));

        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var code));
        Assert.Equal(SprechenProto.SprechenErrorCode.AuthInvalidApiKey, code);
    }

    [Fact]
    public async Task UnaryServerHandler_ValidApiKey_PassesThrough()
    {
        var interceptor = new ServiceApiKeyInterceptor(
            Microsoft.Extensions.Options.Options.Create(new ServiceAuthOptions { ApiKey = "expected" }));

        var called = false;

        await interceptor.UnaryServerHandler(
            request: new object(),
            context: CreateContext(
                "Unary",
                requestHeaders: new Metadata { { "x-api-key", "expected" } }),
            continuation: (_, _) =>
            {
                called = true;
                return Task.FromResult(new Empty());
            });

        Assert.True(called);
    }

    // Auch Client-, Server- und Duplex-Streaming müssen einen fehlenden API-Schlüssel ablehnen.
    [Fact]
    public async Task ClientStreamingServerHandler_MissingApiKey_ReturnsCodedUnauthenticated()
    {
        var interceptor = new ServiceApiKeyInterceptor(
            Microsoft.Extensions.Options.Options.Create(new ServiceAuthOptions { ApiKey = "expected" }));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.ClientStreamingServerHandler(
                new NoOpAsyncStreamReader<object>(),
                CreateContext("ClientStreaming", new Metadata()),
                static (_, _) => Task.FromResult(new Empty())));

        AssertMissingApiKey(ex);
    }

    [Fact]
    public async Task ServerStreamingServerHandler_MissingApiKey_ReturnsCodedUnauthenticated()
    {
        var interceptor = new ServiceApiKeyInterceptor(
            Microsoft.Extensions.Options.Options.Create(new ServiceAuthOptions { ApiKey = "expected" }));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.ServerStreamingServerHandler(
                new object(),
                new NoOpServerStreamWriter<Empty>(),
                CreateContext("ServerStreaming", new Metadata()),
                static (_, _, _) => Task.CompletedTask));

        AssertMissingApiKey(ex);
    }

    [Fact]
    public async Task DuplexStreamingServerHandler_MissingApiKey_ReturnsCodedUnauthenticated()
    {
        var interceptor = new ServiceApiKeyInterceptor(
            Microsoft.Extensions.Options.Options.Create(new ServiceAuthOptions { ApiKey = "expected" }));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.DuplexStreamingServerHandler(
                new NoOpAsyncStreamReader<object>(),
                new NoOpServerStreamWriter<Empty>(),
                CreateContext("DuplexStreaming", new Metadata()),
                static (_, _, _) => Task.CompletedTask));

        AssertMissingApiKey(ex);
    }

    private static void AssertMissingApiKey(RpcException ex)
    {
        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var code));
        Assert.Equal(SprechenProto.SprechenErrorCode.AuthMissingApiKey, code);
    }

    private static ServerCallContext CreateContext(string method, Metadata requestHeaders) =>
        TestServerCallContext.Create(
            method,
            null,
            DateTime.UtcNow.AddMinutes(1),
            requestHeaders,
            CancellationToken.None,
            "127.0.0.1",
            null,
            null,
            _ => Task.CompletedTask,
            static () => new WriteOptions(),
            static _ => { });
}
