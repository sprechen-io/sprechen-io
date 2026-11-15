using Sprechen.Service.Abstractions;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Ordnet Erkennungsprofile zwischen internem Modell und Protobuf zu.
/// Unbekannte Werte werden als Gesprächsprofil behandelt.
/// </summary>
internal static class GrpcRecognitionProfileMapper
{
    public static RecognitionProfile ToCore(SprechenProto.RecognitionProfile proto) => proto switch
    {
        SprechenProto.RecognitionProfile.Spelling => RecognitionProfile.Spelling,
        SprechenProto.RecognitionProfile.Digits => RecognitionProfile.Digits,
        _ => RecognitionProfile.Conversational
    };

    public static SprechenProto.RecognitionProfile ToProto(RecognitionProfile core) => core switch
    {
        RecognitionProfile.Spelling => SprechenProto.RecognitionProfile.Spelling,
        RecognitionProfile.Digits => SprechenProto.RecognitionProfile.Digits,
        _ => SprechenProto.RecognitionProfile.Conversational
    };
}
