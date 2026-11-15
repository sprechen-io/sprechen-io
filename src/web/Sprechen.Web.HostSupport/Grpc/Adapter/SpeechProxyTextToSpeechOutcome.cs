namespace Sprechen.Web.HostSupport.Grpc.Adapter;

/// <summary>
/// Beschreibt das Ergebnis einer Audio-Weiterleitung mit Abbruchmarker, Chunk- und Bytezahlen sowie optionaler Abtastrate.
/// Der explizite Marker erlaubt dem Aufrufer, auch abgebrochene Ausgaben zu protokollieren.
/// </summary>
internal readonly record struct SpeechProxyTextToSpeechOutcome(
    bool Aborted,
    int ChunkCount,
    long ByteCount,
    int? FirstSampleRateHz);
