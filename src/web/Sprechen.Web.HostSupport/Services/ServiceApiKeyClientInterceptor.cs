using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;
using Sprechen.Web.HostSupport.Options;

namespace Sprechen.Web.HostSupport.Services;

/// <summary>
/// Ergänzt ausgehende gRPC-Aufrufe des Hosts um den Dienstschlüssel, damit die WASM-App ihn nicht mitsenden muss.
/// </summary>
internal sealed class ServiceApiKeyClientInterceptor : Interceptor, IDisposable
{
    private readonly ServiceApiKeyHeaderPolicy _headerPolicy;

    public ServiceApiKeyClientInterceptor(IOptions<ServiceAuthOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _headerPolicy = new ServiceApiKeyHeaderPolicy(options.Value.ApiKey);
    }

    public void Dispose()
    {
        _headerPolicy.Dispose();
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        return continuation(request, ApplyApiKey(context));
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        return continuation(request, ApplyApiKey(context));
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        return continuation(ApplyApiKey(context));
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        return continuation(ApplyApiKey(context));
    }

    private ClientInterceptorContext<TRequest, TResponse> ApplyApiKey<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        return new ClientInterceptorContext<TRequest, TResponse>(
            context.Method,
            context.Host,
            WithApiKey(context.Options));
    }

    private CallOptions WithApiKey(CallOptions options)
    {
        return _headerPolicy.Apply(options);
    }
}
