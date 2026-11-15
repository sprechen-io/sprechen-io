using Grpc.Core;

namespace Sprechen.Web.HostSupport.Grpc.Adapter;

/// <summary>
/// Bündelt Korrelationskennung, Trace-ID, Backendheader und optionale Sitzungskennung eines Proxyaufrufs.
/// </summary>
internal sealed record SpeechProxyCallContext(
    string ClientCorrelationId,
    string TraceId,
    Metadata BackendHeaders,
    // Leer, wenn keine Sitzungskennung übergeben wurde. Dann ist darüber keine Sitzungszuordnung möglich.
    string SessionId = "");
