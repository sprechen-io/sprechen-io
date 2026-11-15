using AppRecognitionProfile = Sprechen.Speech.App.Contracts.Services.RecognitionProfile;
using ProtoRecognitionProfile = Sprechen.Proto.RecognitionProfile;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Überträgt Erkennungsprofile zwischen Anwendungsvertrag und gRPC. Andere
/// Werte als Buchstabieren und Ziffern werden dem Gesprächsprofil zugeordnet.
/// </summary>
internal static class RecognitionProfileGrpcMapper
{
    public static AppRecognitionProfile FromProto(ProtoRecognitionProfile profile)
    {
        return profile switch
        {
            ProtoRecognitionProfile.Spelling => AppRecognitionProfile.Spelling,
            ProtoRecognitionProfile.Digits => AppRecognitionProfile.Digits,
            _ => AppRecognitionProfile.Conversational
        };
    }

    public static ProtoRecognitionProfile ToProto(AppRecognitionProfile profile)
    {
        return profile switch
        {
            AppRecognitionProfile.Spelling => ProtoRecognitionProfile.Spelling,
            AppRecognitionProfile.Digits => ProtoRecognitionProfile.Digits,
            _ => ProtoRecognitionProfile.Conversational
        };
    }
}
