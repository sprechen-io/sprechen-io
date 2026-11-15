using Grpc.Core;

namespace Sprechen.Proto.Transport;

/// <summary>
/// Überträgt eine Sitzungskennung in gRPC-Metadaten, um Ansagen und Erkennungen derselben Sitzung zuzuordnen.
/// </summary>
/// <remarks>Die Kennung unterscheidet Sitzungen, aber keine einzelnen Dialogschritte innerhalb einer Sitzung.</remarks>
public static class SprechenGrpcSessionMetadata
{
    public const string SessionIdHeaderName = "x-sprechen-session-id";

    public static string CreateSessionId() => Guid.NewGuid().ToString("N");

    public static void AddOrReplaceSessionId(Metadata headers, string sessionId)
    {
        ArgumentNullException.ThrowIfNull(headers);

        for (var index = headers.Count - 1; index >= 0; index--)
        {
            if (string.Equals(headers[index].Key, SessionIdHeaderName, StringComparison.OrdinalIgnoreCase))
            {
                headers.RemoveAt(index);
            }
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            headers.Add(SessionIdHeaderName, sessionId.Trim());
        }
    }

    /// <summary>
    /// Liest die Sitzungskennung und liefert bei einem fehlenden oder leeren Header eine leere Zeichenkette.
    /// </summary>
    /// <remarks>Eine fehlende Kennung wird nicht ergänzt, damit keine unbelegte Sitzungszuordnung entsteht.</remarks>
    public static string ReadSessionId(Metadata? headers)
    {
        var declared = headers?.GetValue(SessionIdHeaderName);
        return string.IsNullOrWhiteSpace(declared) ? string.Empty : declared.Trim();
    }
}
