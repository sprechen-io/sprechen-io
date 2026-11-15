using Microsoft.AspNetCore.Builder;
using Sprechen.Web.HostSupport.Grpc.Adapter;

namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Registriert den Sprachproxy mit gRPC-Web-Unterstützung in der Anwendung.
/// </summary>
public static class HostedVoiceGrpcWebApplicationExtensions
{
    public static WebApplication UseHostedVoiceGrpc(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseGrpcWeb();
        app.MapGrpcService<SpeechProxyService>().EnableGrpcWeb();

        return app;
    }
}
