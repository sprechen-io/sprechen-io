using Sprechen.Service.Core.Services;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Ordnet Audioformate zwischen internem Modell und Protobuf zu.
/// Unbekannte Werte werden in beiden Richtungen als nicht angegeben behandelt.
/// </summary>
internal static class GrpcAudioMimeTypeMapper
{
    public static AudioMimeType ToCore(SprechenProto.AudioMimeType proto) => proto switch
    {
        SprechenProto.AudioMimeType.AudioWav => AudioMimeType.Wav,
        SprechenProto.AudioMimeType.AudioOggOpus => AudioMimeType.OggOpus,
        SprechenProto.AudioMimeType.AudioFlac => AudioMimeType.Flac,
        SprechenProto.AudioMimeType.AudioWebmOpus => AudioMimeType.WebmOpus,
        SprechenProto.AudioMimeType.AudioMp3 => AudioMimeType.Mp3,
        SprechenProto.AudioMimeType.AudioPcm => AudioMimeType.Pcm,
        _ => AudioMimeType.Unspecified
    };

    public static SprechenProto.AudioMimeType ToProto(AudioMimeType core) => core switch
    {
        AudioMimeType.Wav => SprechenProto.AudioMimeType.AudioWav,
        AudioMimeType.OggOpus => SprechenProto.AudioMimeType.AudioOggOpus,
        AudioMimeType.Flac => SprechenProto.AudioMimeType.AudioFlac,
        AudioMimeType.WebmOpus => SprechenProto.AudioMimeType.AudioWebmOpus,
        AudioMimeType.Mp3 => SprechenProto.AudioMimeType.AudioMp3,
        AudioMimeType.Pcm => SprechenProto.AudioMimeType.AudioPcm,
        _ => SprechenProto.AudioMimeType.AudioMimeUnspecified
    };
}

