using Grpc.Core;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class NoOpCallInvoker : CallInvoker
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options,
        TRequest request)
        where TRequest : class
        where TResponse : class
    {
        throw new NotSupportedException();
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options,
        TRequest request)
        where TRequest : class
        where TResponse : class
    {
        throw new NotSupportedException();
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options)
        where TRequest : class
        where TResponse : class
    {
        throw new NotSupportedException();
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options)
        where TRequest : class
        where TResponse : class
    {
        throw new NotSupportedException();
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options,
        TRequest request)
        where TRequest : class
        where TResponse : class
    {
        throw new NotSupportedException();
    }
}

