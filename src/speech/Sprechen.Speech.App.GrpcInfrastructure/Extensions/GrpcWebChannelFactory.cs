using Grpc.Net.Client;
using Grpc.Net.Client.Web;

namespace Sprechen.Speech.App.GrpcInfrastructure.Extensions;

/// <summary>
/// Erzeugt einen gRPC-Kanal zur angegebenen Basisadresse mit einem
/// HTTP-Handler für die binäre gRPC-Web-Übertragung.
/// </summary>
internal static class GrpcWebChannelFactory
{
    public static GrpcChannel Create(Uri baseAddress)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        return GrpcChannel.ForAddress(
            baseAddress,
            new GrpcChannelOptions
            {
                // Die binäre Übertragung soll Stream-Resets des Textmodus in Blazor WebAssembly vermeiden.
                HttpHandler = CreateHandler()
            });
    }

    internal static GrpcWebHandler CreateHandler()
    {
        return new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler());
    }
}

