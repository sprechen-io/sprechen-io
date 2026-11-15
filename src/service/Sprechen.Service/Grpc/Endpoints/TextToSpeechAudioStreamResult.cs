namespace Sprechen.Service.Grpc;

/// <summary>
/// Anzahl und Byteumfang nicht leerer Anbietersegmente sowie die Abtastrate des ersten nicht leeren Segments.
/// </summary>
internal readonly record struct TextToSpeechAudioStreamResult(
    int ChunkCount,
    long ByteCount,
    int? FirstSampleRateHz);
