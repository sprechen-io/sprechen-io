using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;
using Sprechen.Service.Grpc;
using Sprechen.Service.Options;
using System.Security.Cryptography;
using System.Text;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Security;

/// <summary>
/// Prüft den API-Schlüssel vor einfachen und streamenden gRPC-Aufrufen.
/// Schlüssel gleicher Bytelänge werden mit FixedTimeEquals verglichen.
/// </summary>
internal sealed class ServiceApiKeyInterceptor : Interceptor, IDisposable
{
    private const string ApiKeyHeaderName = "x-api-key";

    private readonly byte[] _expectedApiKeyBytes;

    public ServiceApiKeyInterceptor(IOptions<ServiceAuthOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var apiKey = options.Value.ApiKey;
        _expectedApiKeyBytes = Encoding.UTF8.GetBytes(apiKey);
    }

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_expectedApiKeyBytes);
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAuthorized(context);
        return await continuation(request, context);
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAuthorized(context);
        return await continuation(requestStream, context);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAuthorized(context);
        await continuation(request, responseStream, context);
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAuthorized(context);
        await continuation(requestStream, responseStream, context);
    }

    private void EnsureAuthorized(ServerCallContext context)
    {
        var providedApiKey = context.RequestHeaders.GetValue(ApiKeyHeaderName);
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            throw SprechenRpcExceptionFactory.Unauthenticated(SprechenProto.SprechenErrorCode.AuthMissingApiKey, "Missing API key.");
        }

        var providedApiKeyBytes = Encoding.UTF8.GetBytes(providedApiKey);
        try
        {
            if (providedApiKeyBytes.Length != _expectedApiKeyBytes.Length ||
                !CryptographicOperations.FixedTimeEquals(providedApiKeyBytes, _expectedApiKeyBytes))
            {
                throw SprechenRpcExceptionFactory.Unauthenticated(SprechenProto.SprechenErrorCode.AuthInvalidApiKey, "Invalid API key.");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(providedApiKeyBytes);
        }
    }
}
