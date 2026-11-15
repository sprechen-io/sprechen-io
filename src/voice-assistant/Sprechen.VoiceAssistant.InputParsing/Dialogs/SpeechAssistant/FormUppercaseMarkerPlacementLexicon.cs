namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Bewahrt die modellierte Stellung von Großschreibungsmarkern je Sprache.
/// Die Abfrage erlaubt dem Buchstabierparser nachgestellte Marker gezielt.
/// Vorangestellte Marker bleiben dort unabhängig davon zulässig.
/// </summary>
internal sealed class FormUppercaseMarkerPlacementLexicon
{
    private const string AfterLetter = "afterLetter";

    private readonly IReadOnlyDictionary<string, string> _placementByLanguage;

    public FormUppercaseMarkerPlacementLexicon(IReadOnlyDictionary<string, string> placementByLanguage)
    {
        _placementByLanguage = placementByLanguage ?? throw new ArgumentNullException(nameof(placementByLanguage));
    }

    /// <summary>
    /// Meldet die modellierte nachgestellte Position. Fehlende oder unbekannte
    /// Sprachen liefern <c>false</c>. Der Parser prüft weiterhin vorangestellte Marker.
    /// </summary>
    public bool FollowsLetter(string? isoCode)
        => !string.IsNullOrWhiteSpace(isoCode)
            && _placementByLanguage.TryGetValue(isoCode.Trim(), out var placement)
            && string.Equals(placement, AfterLetter, StringComparison.Ordinal);
}
