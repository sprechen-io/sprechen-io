using Sprechen.Service.Abstractions;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Überträgt Transkripttext und optionale Konfidenz in die Protobuf-Antwort.
/// Konfidenzwerte werden auf 0 bis 100 begrenzt.
/// </summary>
internal static class GrpcSpeechTranscriptMapper
{
    public static SprechenProto.SpeechTranscript Map(SpeechTranscript transcription)
    {
        ArgumentNullException.ThrowIfNull(transcription);

        var mapped = new SprechenProto.SpeechTranscript
        {
            Text = transcription.Text ?? string.Empty
        };

        if (transcription.ConfidencePercent.HasValue)
        {
            mapped.Confidence = Math.Clamp(transcription.ConfidencePercent.Value, 0, 100);
        }

        return mapped;
    }
}
