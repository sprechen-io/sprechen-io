using Sprechen.Service.Core.Services;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Enthält eingelesene Startmetadaten und gepufferte Audiodaten eines gRPC-Stroms.
/// </summary>
internal sealed class GrpcAudioChunkStreamStart
{
    public GrpcAudioChunkStreamStart(
        AudioMimeType mimeType,
        SprechenProto.AudioMimeType providedMimeType,
        string? languageCode,
        IReadOnlyList<byte[]> bufferedChunks,
        RecognitionProfile recognitionProfile = RecognitionProfile.Conversational,
        SpeechKeyterms? keyterms = null,
        bool diagnosticSession = false)
    {
        MimeType = mimeType;
        ProvidedMimeType = providedMimeType;
        LanguageCode = languageCode;
        BufferedChunks = bufferedChunks ?? throw new ArgumentNullException(nameof(bufferedChunks));
        RecognitionProfile = recognitionProfile;
        Keyterms = keyterms;
        DiagnosticSession = diagnosticSession;
    }

    public AudioMimeType MimeType { get; }
    public SprechenProto.AudioMimeType ProvidedMimeType { get; }
    public string? LanguageCode { get; }
    public IReadOnlyList<byte[]> BufferedChunks { get; }
    public RecognitionProfile RecognitionProfile { get; }

    /// <summary>
    /// Diagnosekennzeichnung des eingelesenen Startabschnitts. Standardmäßig false.
    /// </summary>
    public bool DiagnosticSession { get; }

    /// <summary>
    /// Erkennungshinweise samt Zusammensetzung oder null, wenn keine Hinweise vorliegen.
    /// Eine leere Liste mit WithoutServerBase unterdrückt zusätzlich das Servervokabular.
    /// </summary>
    public SpeechKeyterms? Keyterms { get; }

    public bool HasAnyChunk => BufferedChunks.Count > 0;

    public bool MimeTypeWasProvided => ProvidedMimeType != SprechenProto.AudioMimeType.AudioMimeUnspecified;

    public bool MimeTypeUnsupported => MimeTypeWasProvided && MimeType == AudioMimeType.Unspecified;
}

