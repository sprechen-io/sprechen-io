namespace Sprechen.Web.HostSupport.Grpc.Proxy;

/// <summary>
/// Bündelt Chunk- und Bytezahlen sowie die optionale Abtastrate einer Audio-Weiterleitung.
/// </summary>
internal readonly record struct SpeechProxyAudioForwardingSummary(
    int ChunkCount,
    long ByteCount,
    int? FirstSampleRateHz);
