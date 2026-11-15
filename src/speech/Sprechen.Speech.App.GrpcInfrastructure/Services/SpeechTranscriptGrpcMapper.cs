using AppSpeechTranscript = Sprechen.Speech.App.Contracts.Services.SpeechTranscript;
using GrpcSpeechTranscript = Sprechen.Proto.SpeechTranscript;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Überträgt das gRPC-Transkript in den Anwendungsvertrag, entfernt äußere
/// Leerzeichen und begrenzt vorhandene Konfidenzwerte auf 0 bis 100 Prozent.
/// </summary>
internal static class SpeechTranscriptGrpcMapper
{
    public static AppSpeechTranscript FromProto(GrpcSpeechTranscript transcript)
    {
        ArgumentNullException.ThrowIfNull(transcript);

        return new AppSpeechTranscript(
            transcript.Text?.Trim() ?? string.Empty,
            transcript.HasConfidence
                ? Math.Clamp(transcript.Confidence, 0, 100)
                : null);
    }
}
