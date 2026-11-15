using AppSpeechTranscript = Sprechen.Speech.App.Contracts.Services.SpeechTranscript;
using GrpcSpeechTranscript = Sprechen.Proto.SpeechTranscript;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Prüft die einzelne gRPC-Transkriptantwort auf Vorhandensein und übergibt
/// sie der gemeinsamen Abbildung in den Anwendungsvertrag.
/// </summary>
internal sealed class SpeechRecognitionUnaryResponseMapper
{
    public AppSpeechTranscript MapSpeechTranscript(GrpcSpeechTranscript response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return SpeechTranscriptGrpcMapper.FromProto(response);
    }
}
