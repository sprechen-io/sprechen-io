using System.Linq;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Sprechen.Web.HostSupport.Options;
using Sprechen.Web.HostSupport.Services;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Services;

public sealed class ServiceApiKeyClientInterceptorTests
{
    [Fact]
    public void AsyncUnaryCall_ExistingApiKeyHeader_IsReplaced()
    {
        var interceptor = new ServiceApiKeyClientInterceptor(
            Microsoft.Extensions.Options.Options.Create(new ServiceAuthOptions { ApiKey = "expected" }));

        var method = new Method<string, string>(
            MethodType.Unary,
            "sprechen.Speech",
            "GetSupportedLanguages",
            Marshallers.StringMarshaller,
            Marshallers.StringMarshaller);

        ClientInterceptorContext<string, string>? capturedContext = null;
        var originalHeaders = new Metadata
        {
            { "x-api-key", "old-value" },
            { "x-other", "kept" }
        };

        _ = interceptor.AsyncUnaryCall(
            "request",
            new ClientInterceptorContext<string, string>(method, "localhost", new CallOptions(originalHeaders)),
            (_, context) =>
            {
                capturedContext = context;
                return new AsyncUnaryCall<string>(
                    Task.FromResult("response"),
                    Task.FromResult(new Metadata()),
                    () => new Status(StatusCode.OK, string.Empty),
                    () => new Metadata(),
                    () => { });
            });

        var headers = Assert.NotNull(capturedContext).Options.Headers;
        Assert.NotNull(headers);

        Assert.Null(headers!.FirstOrDefault(e => e.Key == "x-api-key" && e.Value == "old-value"));
        Assert.NotNull(headers.FirstOrDefault(e => e.Key == "x-api-key" && e.Value == "expected"));
        Assert.NotNull(headers.FirstOrDefault(e => e.Key == "x-other" && e.Value == "kept"));
    }
}


