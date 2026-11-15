using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Normalisiert erforderliche und optionale Sprachcodes und prüft ihre Unterstützung.
/// Löst außerdem Anzeigenamen aus dem Sprachkatalog auf.
/// </summary>
internal sealed class LanguageResolver(ISupportedLanguagesService supportedLanguages)
{
    private readonly ISupportedLanguagesService _supportedLanguages = supportedLanguages ?? throw new ArgumentNullException(nameof(supportedLanguages));

    public string ResolveRequiredIsoLanguageCode(string? rawLanguageCode, string missingMessage)
    {
        if (string.IsNullOrWhiteSpace(rawLanguageCode))
        {
            throw SprechenRpcExceptionFactory.InvalidArgument(SprechenProto.SprechenErrorCode.LanguageCodeRequired, missingMessage);
        }

        return ResolveNormalizedSupportedIsoCode(rawLanguageCode);
    }

    public string? ResolveOptionalIsoLanguageCode(string? rawLanguageCode)
    {
        if (string.IsNullOrWhiteSpace(rawLanguageCode))
        {
            return null;
        }

        return ResolveNormalizedSupportedIsoCode(rawLanguageCode);
    }

    public string ResolveLanguageDisplayName(string isoCode)
    {
        var normalized = LanguageCodeNormalizer.NormalizeIso6391(isoCode) ?? isoCode;
        var match = _supportedLanguages.Languages.FirstOrDefault(language =>
            string.Equals(language.IsoCode, normalized, StringComparison.OrdinalIgnoreCase));

        return match?.DisplayName ?? normalized;
    }

    private string ResolveNormalizedSupportedIsoCode(string rawLanguageCode)
    {
        var normalized = LanguageCodeNormalizer.NormalizeIso6391(rawLanguageCode);
        if (normalized is null)
        {
            throw SprechenRpcExceptionFactory.InvalidArgument(
                SprechenProto.SprechenErrorCode.LanguageCodeInvalid,
                $"LanguageCode must be ISO 639-1 (two letters). Supported: {SupportedIsoCodesDisplay()}.");
        }

        if (!_supportedLanguages.IsSupportedIsoCode(normalized))
        {
            throw SprechenRpcExceptionFactory.InvalidArgument(
                SprechenProto.SprechenErrorCode.LanguageCodeUnsupported,
                $"LanguageCode '{normalized}' is not supported. Supported: {SupportedIsoCodesDisplay()}.");
        }

        return normalized;
    }

    private string SupportedIsoCodesDisplay()
        => string.Join(", ", _supportedLanguages.Languages.Select(language => language.IsoCode));
}
