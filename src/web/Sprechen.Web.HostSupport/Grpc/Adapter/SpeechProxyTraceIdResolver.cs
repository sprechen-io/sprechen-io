using System.Diagnostics;

namespace Sprechen.Web.HostSupport.Grpc.Adapter;

/// <summary>
/// Liest die aktuelle Activity-Trace-ID oder liefert einen Marker für den fehlenden Trace-Kontext.
/// </summary>
internal sealed class SpeechProxyTraceIdResolver
{
    public string Resolve()
    {
        return Activity.Current?.TraceId.ToString() ?? "<none>";
    }
}
