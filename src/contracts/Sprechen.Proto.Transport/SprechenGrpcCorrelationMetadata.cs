using Grpc.Core;

namespace Sprechen.Proto.Transport;

/// <summary>
/// Überträgt die Korrelationskennung als gRPC-Metadatum, um Ansagen und Erkennungen zuordnen zu können.
/// </summary>
public static class SprechenGrpcCorrelationMetadata
{
    public const string CorrelationIdHeaderName = "x-sprechen-correlation-id";

    public static string CreateCorrelationId()
        => Guid.NewGuid().ToString("N");

    public static Metadata CreateHeaders(string correlationId)
    {
        var headers = new Metadata();
        AddOrReplaceCorrelationId(headers, correlationId);
        return headers;
    }

    public static void AddOrReplaceCorrelationId(Metadata headers, string correlationId)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var normalizedCorrelationId = NormalizeCorrelationId(correlationId);
        RemoveCorrelationId(headers);
        headers.Add(CorrelationIdHeaderName, normalizedCorrelationId);
    }

    public static string GetOrCreateCorrelationId(Metadata? headers)
    {
        return TryGetCorrelationId(headers, out var correlationId)
            ? correlationId
            : CreateCorrelationId();
    }

    public static bool TryGetCorrelationId(Metadata? headers, out string correlationId)
    {
        if (headers is not null)
        {
            foreach (var entry in headers)
            {
                if (!string.Equals(entry.Key, CorrelationIdHeaderName, StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(entry.Value))
                {
                    continue;
                }

                correlationId = entry.Value.Trim();
                return true;
            }
        }

        correlationId = string.Empty;
        return false;
    }

    private static string NormalizeCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        return correlationId.Trim();
    }

    private static void RemoveCorrelationId(Metadata headers)
    {
        for (var index = headers.Count - 1; index >= 0; index--)
        {
            if (string.Equals(headers[index].Key, CorrelationIdHeaderName, StringComparison.OrdinalIgnoreCase))
            {
                headers.RemoveAt(index);
            }
        }
    }
}
