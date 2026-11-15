using Sprechen.Proto;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Überträgt Standardsprache und Sprachliste aus der gRPC-Antwort in den
/// Anwendungsvertrag und entfernt äußere Leerzeichen aus Codes und Anzeigenamen.
/// </summary>
internal static class SupportedLanguagesGrpcMapper
{
    public static SupportedLanguages FromProto(SupportedLanguagesResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new SupportedLanguages(
            response.DefaultIsoCode?.Trim() ?? string.Empty,
            response.Languages
                .Select(language => new Sprechen.Speech.LanguageContracts.SupportedLanguage(
                    language.IsoCode?.Trim() ?? string.Empty,
                    language.DisplayName?.Trim() ?? string.Empty))
                .ToArray());
    }
}
