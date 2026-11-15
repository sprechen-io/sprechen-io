using Sprechen.Proto;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Überträgt Audioformate zwischen Anwendungsvertrag und gRPC und bildet
/// unbekannte Werte auf das jeweils nicht festgelegte Format ab.
/// </summary>
internal static class SpeechAudioMimeTypeGrpcMapper
{
    public static SpeechAudioMimeType FromProto(AudioMimeType mimeType)
    {
        return mimeType switch
        {
            AudioMimeType.AudioMp3 => SpeechAudioMimeType.Mp3,
            AudioMimeType.AudioWav => SpeechAudioMimeType.Wav,
            AudioMimeType.AudioOggOpus => SpeechAudioMimeType.OggOpus,
            AudioMimeType.AudioFlac => SpeechAudioMimeType.Flac,
            AudioMimeType.AudioWebmOpus => SpeechAudioMimeType.WebmOpus,
            AudioMimeType.AudioPcm => SpeechAudioMimeType.Pcm,
            _ => SpeechAudioMimeType.Unspecified
        };
    }

    public static AudioMimeType ToProto(SpeechAudioMimeType mimeType)
    {
        return mimeType switch
        {
            SpeechAudioMimeType.Mp3 => AudioMimeType.AudioMp3,
            SpeechAudioMimeType.Wav => AudioMimeType.AudioWav,
            SpeechAudioMimeType.OggOpus => AudioMimeType.AudioOggOpus,
            SpeechAudioMimeType.Flac => AudioMimeType.AudioFlac,
            SpeechAudioMimeType.WebmOpus => AudioMimeType.AudioWebmOpus,
            SpeechAudioMimeType.Pcm => AudioMimeType.AudioPcm,
            _ => AudioMimeType.AudioMimeUnspecified
        };
    }
}

