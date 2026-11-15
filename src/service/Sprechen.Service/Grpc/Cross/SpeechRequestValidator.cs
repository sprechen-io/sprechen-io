using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Validiert Audio- und Syntheseanfragen und liefert normalisierte Werte oder gRPC-Eingabefehler.
/// </summary>
internal sealed class SpeechRequestValidator(LanguageResolver languageResolver)
{
    private readonly LanguageResolver _languageResolver = languageResolver ?? throw new ArgumentNullException(nameof(languageResolver));

    public (AudioMimeType MimeType, string LanguageCode) ValidateRequiredSpeechStart(GrpcAudioChunkStreamStart start)
    {
        ValidateAudioStreamStart(start);

        var isoLanguageCode = _languageResolver.ResolveRequiredIsoLanguageCode(
            start.LanguageCode,
            "AudioChunk.languageCode must be set (ISO 639-1, e.g. 'de') on the first streamed message.");

        return (start.MimeType, isoLanguageCode);
    }

    public (AudioMimeType MimeType, string? LanguageCode) ValidateOptionalSpeechStart(GrpcAudioChunkStreamStart start)
    {
        ValidateAudioStreamStart(start);
        return (start.MimeType, _languageResolver.ResolveOptionalIsoLanguageCode(start.LanguageCode));
    }

    public (string Text, string LanguageCode, AudioMimeType DesiredMimeType, TextToSpeechPolitenessLevel PolitenessLevel, float SpeechRate) ValidateTextToSpeechRequest(
        SprechenProto.TextRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw SprechenRpcExceptionFactory.InvalidArgument(
                SprechenProto.SprechenErrorCode.TextRequired,
                "TextRequest.text must be set.");
        }

        var text = request.Text.Trim();
        var languageCode = _languageResolver.ResolveRequiredIsoLanguageCode(
            request.LanguageCode,
            "TextRequest.languageCode must be set (ISO 639-1, e.g. 'de').");

        if (request.DesiredMimeType == SprechenProto.AudioMimeType.AudioMimeUnspecified)
        {
            throw SprechenRpcExceptionFactory.InvalidArgument(
                SprechenProto.SprechenErrorCode.AudioOutputMimeTypeRequired,
                "TextRequest.desiredMimeType must be set (e.g. AUDIO_PCM).");
        }

        var desiredCoreMimeType = GrpcAudioMimeTypeMapper.ToCore(request.DesiredMimeType);
        if (desiredCoreMimeType == AudioMimeType.Unspecified)
        {
            throw SprechenRpcExceptionFactory.InvalidArgument(
                SprechenProto.SprechenErrorCode.AudioOutputMimeTypeUnsupported,
                $"TextRequest.desiredMimeType '{request.DesiredMimeType}' is not supported.");
        }

        return (
            text,
            languageCode,
            desiredCoreMimeType,
            MapPolitenessLevel(request.PolitenessLevel),
            request.HasSpeechRate ? request.SpeechRate : 1.0f);
    }

    public static void ValidateAudioStreamStart(GrpcAudioChunkStreamStart start)
    {
        ArgumentNullException.ThrowIfNull(start);

        // Auch ein gültiger MIME-Typ ersetzt kein nicht leeres Audiosegment.
        if (!start.HasAnyChunk)
        {
            throw SprechenRpcExceptionFactory.InvalidArgument(
                SprechenProto.SprechenErrorCode.AudioInputMimeTypeRequired,
                "Audio stream must contain at least one non-empty AudioChunk with mimeType set on the first streamed message.");
        }

        if (start.MimeType != AudioMimeType.Unspecified)
        {
            return;
        }

        throw start.MimeTypeUnsupported
            ? SprechenRpcExceptionFactory.InvalidArgument(
                SprechenProto.SprechenErrorCode.AudioInputMimeTypeUnsupported,
                $"AudioChunk.mimeType '{start.ProvidedMimeType}' is not supported.")
            : SprechenRpcExceptionFactory.InvalidArgument(
                SprechenProto.SprechenErrorCode.AudioInputMimeTypeRequired,
                "AudioChunk.mimeType must be set (e.g. AUDIO_PCM) on the first streamed message.");
    }

    public static void ValidateUnarySpeechToTextStartRequiresPcm(GrpcAudioChunkStreamStart start)
    {
        ValidateAudioStreamStart(start);

        if (start.MimeType != AudioMimeType.Pcm)
        {
            throw SprechenRpcExceptionFactory.InvalidArgument(
                SprechenProto.SprechenErrorCode.AudioInputMimeTypeUnsupported,
                $"AudioChunk.mimeType '{start.MimeType}' is not supported by the unary SpeechToText endpoint; only AUDIO_PCM is accepted.");
        }
    }

    private static TextToSpeechPolitenessLevel MapPolitenessLevel(SprechenProto.TextToSpeechPolitenessLevel politenessLevel)
    {
        return politenessLevel switch
        {
            SprechenProto.TextToSpeechPolitenessLevel.TtsPolitenessFormal => TextToSpeechPolitenessLevel.Formal,
            SprechenProto.TextToSpeechPolitenessLevel.TtsPolitenessInformal => TextToSpeechPolitenessLevel.Informal,
            _ => TextToSpeechPolitenessLevel.Unspecified
        };
    }
}
