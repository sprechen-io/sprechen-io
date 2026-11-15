using Sprechen.Service.Abstractions;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Ordnet die Zusammensetzung von Erkennungshinweisen zwischen internem Modell und Protobuf zu.
/// </summary>
internal static class GrpcKeytermCompositionMapper
{
    // Unspecified und unbekannte Werte verwenden wie WithServerBase das Servervokabular.
    public static KeytermComposition ToCore(SprechenProto.KeytermComposition proto) => proto switch
    {
        SprechenProto.KeytermComposition.WithoutServerBase => KeytermComposition.WithoutServerBase,
        _ => KeytermComposition.WithServerBase
    };

    public static SprechenProto.KeytermComposition ToProto(KeytermComposition core) => core switch
    {
        KeytermComposition.WithoutServerBase => SprechenProto.KeytermComposition.WithoutServerBase,
        _ => SprechenProto.KeytermComposition.WithServerBase
    };
}
