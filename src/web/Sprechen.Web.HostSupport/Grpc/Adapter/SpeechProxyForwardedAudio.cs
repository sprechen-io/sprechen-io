namespace Sprechen.Web.HostSupport.Grpc.Adapter;

/// <summary>
/// Hält Anzahl und Bytegröße der vor der Weiterleitung gelesenen Audiochunks.
/// Der Aufrufer behält die Zähler auch bei Fehlern.
/// </summary>
internal sealed class SpeechProxyForwardedAudio
{
    public int Chunks { get; private set; }

    public long Bytes { get; private set; }

    internal void Add(int byteCount)
    {
        Chunks++;
        Bytes += byteCount;
    }
}
