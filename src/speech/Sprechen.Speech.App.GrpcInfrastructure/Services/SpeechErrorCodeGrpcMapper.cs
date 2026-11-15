using Sprechen.Proto;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Ordnet bekannte gRPC-Sprachfehlercodes den Fehlercodes des
/// Anwendungsvertrags zu und lässt unbekannte Codes unbestimmt.
/// </summary>
internal static class SpeechErrorCodeGrpcMapper
{
    private static readonly IReadOnlyDictionary<SprechenErrorCode, SpeechErrorCode> ErrorCodes =
        new Dictionary<SprechenErrorCode, SpeechErrorCode>
        {
            [SprechenErrorCode.AuthMissingApiKey] = SpeechErrorCode.AuthMissingApiKey,
            [SprechenErrorCode.AuthInvalidApiKey] = SpeechErrorCode.AuthInvalidApiKey,
            [SprechenErrorCode.TextRequired] = SpeechErrorCode.TextRequired,
            [SprechenErrorCode.LanguageCodeRequired] = SpeechErrorCode.LanguageCodeRequired,
            [SprechenErrorCode.LanguageCodeInvalid] = SpeechErrorCode.LanguageCodeInvalid,
            [SprechenErrorCode.LanguageCodeUnsupported] = SpeechErrorCode.LanguageCodeUnsupported,
            [SprechenErrorCode.AudioInputMimeTypeRequired] = SpeechErrorCode.AudioInputMimeTypeRequired,
            [SprechenErrorCode.AudioInputMimeTypeUnsupported] = SpeechErrorCode.AudioInputMimeTypeUnsupported,
            [SprechenErrorCode.AudioOutputMimeTypeRequired] = SpeechErrorCode.AudioOutputMimeTypeRequired,
            [SprechenErrorCode.AudioOutputMimeTypeUnsupported] = SpeechErrorCode.AudioOutputMimeTypeUnsupported,
            [SprechenErrorCode.ProviderFailure] = SpeechErrorCode.ProviderFailure,
            [SprechenErrorCode.ProviderRateLimited] = SpeechErrorCode.ProviderRateLimited,
            [SprechenErrorCode.VoiceConfigurationInvalidRequest] = SpeechErrorCode.VoiceConfigurationInvalidRequest,
            [SprechenErrorCode.VoiceConfigurationUnsupportedVersion] = SpeechErrorCode.VoiceConfigurationUnsupportedVersion,
            [SprechenErrorCode.VoiceConfigurationInvalidConfiguration] = SpeechErrorCode.VoiceConfigurationInvalidConfiguration,
            [SprechenErrorCode.SpeechKeytermsInvalid] = SpeechErrorCode.SpeechKeytermsInvalid,
            [SprechenErrorCode.Unspecified] = SpeechErrorCode.Unspecified
        };

    public static SpeechErrorCode FromProto(SprechenErrorCode errorCode)
        => ErrorCodes.TryGetValue(errorCode, out var mapped)
            ? mapped
            : SpeechErrorCode.Unspecified;

    internal static IReadOnlyCollection<SprechenErrorCode> MappedProtoCodes => ErrorCodes.Keys.ToArray();
}
