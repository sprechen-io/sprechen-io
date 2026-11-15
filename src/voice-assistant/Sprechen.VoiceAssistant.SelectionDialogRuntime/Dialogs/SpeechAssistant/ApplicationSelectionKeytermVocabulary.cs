using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet begrenzte Erkennungshinweise aus den bereits lokalisierten Anwendungsnamen ab
/// und erhält deren Reihenfolge. Die Auswahl verwendet dieselbe Namensquelle wie der
/// Kandidatenvergleich. Die Hinweise sollen bei ähnlichen Namen helfen, garantieren
/// aber keine korrekte Unterscheidung.
/// </summary>
internal sealed class ApplicationSelectionKeytermVocabulary
{
    private readonly SpeechKeytermProjectionPolicy _projectionPolicy = new();

    /// <summary>
    /// Liefert eine eindeutige, vertragskonforme Teilmenge verwendbarer Namen in
    /// Katalogreihenfolge. Ohne verwendbare Anzeigenamen bleibt die Liste leer.
    /// </summary>
    public IReadOnlyList<string> GetKeyterms(IReadOnlyList<ApplicationInfo> applications)
    {
        ArgumentNullException.ThrowIfNull(applications);
        return _projectionPolicy.Project(applications.Select(static application => application?.DisplayName));
    }
}
