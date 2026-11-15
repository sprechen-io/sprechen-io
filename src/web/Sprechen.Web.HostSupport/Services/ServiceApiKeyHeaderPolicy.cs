using System.Security.Cryptography;
using System.Text;
using Grpc.Core;

namespace Sprechen.Web.HostSupport.Services;

/// <summary>
/// Ersetzt API-Schlüssel in ausgehenden gRPC-Metadaten durch den konfigurierten Dienstschlüssel.
/// </summary>
internal sealed class ServiceApiKeyHeaderPolicy : IDisposable
{
    public const string HeaderName = "x-api-key";

    private readonly byte[] _apiKeyBytes;

    public ServiceApiKeyHeaderPolicy(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        _apiKeyBytes = Encoding.UTF8.GetBytes(apiKey);
    }

    public CallOptions Apply(CallOptions options)
    {
        var headers = CloneHeadersWithoutApiKey(options.Headers);
        headers.Add(HeaderName, Encoding.UTF8.GetString(_apiKeyBytes));
        return options.WithHeaders(headers);
    }

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_apiKeyBytes);
    }

    private static Metadata CloneHeadersWithoutApiKey(Metadata? headers)
    {
        if (headers is null)
        {
            return new Metadata();
        }

        var clonedHeaders = new Metadata();
        foreach (var entry in headers)
        {
            if (!string.Equals(entry.Key, HeaderName, StringComparison.OrdinalIgnoreCase))
            {
                clonedHeaders.Add(entry);
            }
        }

        return clonedHeaders;
    }
}
