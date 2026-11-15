using AppKeytermComposition = Sprechen.Speech.App.Contracts.Services.KeytermComposition;
using ProtoKeytermComposition = Sprechen.Proto.KeytermComposition;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Überträgt die Regel zur Einbeziehung des Serverwortschatzes zwischen
/// Anwendungsvertrag und gRPC. Nicht gesondert behandelte Werte beziehen ihn ein.
/// </summary>
internal static class KeytermCompositionGrpcMapper
{
    public static AppKeytermComposition FromProto(ProtoKeytermComposition composition)
    {
        return composition switch
        {
            ProtoKeytermComposition.WithoutServerBase => AppKeytermComposition.WithoutServerBase,
            _ => AppKeytermComposition.WithServerBase
        };
    }

    public static ProtoKeytermComposition ToProto(AppKeytermComposition composition)
    {
        return composition switch
        {
            AppKeytermComposition.WithoutServerBase => ProtoKeytermComposition.WithoutServerBase,
            _ => ProtoKeytermComposition.WithServerBase
        };
    }
}
