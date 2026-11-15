using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Erzeugt sprachabhängige Ja/Nein-Optionen und verwendet bei unbekannter Sprache Deutsch, damit auch dann Beschriftungen verfügbar sind.
/// </summary>
internal sealed class FormBooleanOptionProjector
{
    public IReadOnlyList<FormSurfaceOption> Project(string requestedLanguage)
    {
        if (Iso6391LanguageCode.TryNormalize(requestedLanguage, out var normalized))
        {
            return normalized.ToLowerInvariant() switch
            {
                "en" => [new FormSurfaceOption("true", "Yes"), new FormSurfaceOption("false", "No")],
                "fr" => [new FormSurfaceOption("true", "Oui"), new FormSurfaceOption("false", "Non")],
                "es" => [new FormSurfaceOption("true", "S\u00ed"), new FormSurfaceOption("false", "No")],
                "it" => [new FormSurfaceOption("true", "S\u00ec"), new FormSurfaceOption("false", "No")],
                _ => [new FormSurfaceOption("true", "Ja"), new FormSurfaceOption("false", "Nein")]
            };
        }

        return [new FormSurfaceOption("true", "Ja"), new FormSurfaceOption("false", "Nein")];
    }
}
