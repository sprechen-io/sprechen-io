
namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet die Katalogsuche mit der anwendungsspezifischen Projektion des gemeinsamen
/// Kandidatenvergleichs.
/// </summary>
/// <remarks>
/// Die Suche vergleicht kompakt normalisierte Namen, Kennungen, Beschreibungen und Tags als
/// ordinale Teilzeichenfolgen. Sie verwendet keine Editierdistanz. Dieselben Eingaben bleiben
/// dadurch reproduzierbar, mögliche Fehlzuordnungen sind damit aber nicht ausgeschlossen.
/// Normalisierung und modellierte Tags tragen zusätzliche Suchformen bei. Die Vergleichsmechanik
/// bleibt beim Interpreter.
/// </remarks>
internal sealed class ApplicationMatcher
{
    public ApplicationMatcher()
    {
        CandidateMatcher = new SelectionCandidateMatcher<ApplicationInfo>(
            static application => application.Id,
            static application => application.DisplayName,
            TextNormalizer.NormalizeForCompactMatching);
    }

    public SelectionCandidateMatcher<ApplicationInfo> CandidateMatcher { get; }

    /// <summary>
    /// Sucht in Namen, Kennungen, Beschreibungen und Tags der Anwendungen. Eine fehlende oder nur
    /// aus Leerraum bestehende Anfrage liefert den gesamten Bestand.
    /// </summary>
    /// <param name="applications">Zu durchsuchender Anwendungsbestand.</param>
    /// <param name="query">Suchtext. Null oder Leerraum bedeutet keine Eingrenzung.</param>
    /// <returns>Treffer beziehungsweise der gesamte Bestand ohne Suchtext.</returns>
    public List<ApplicationInfo> Search(IReadOnlyList<ApplicationInfo> applications, string? query)
    {
        ArgumentNullException.ThrowIfNull(applications);

        if (string.IsNullOrWhiteSpace(query))
        {
            return applications.ToList();
        }

        var normalizedQuery = TextNormalizer.NormalizeForCompactMatching(query);
        if (normalizedQuery.Length == 0)
        {
            return [];
        }

        return applications
            .Where(app =>
                TextNormalizer.NormalizeForCompactMatching(app.DisplayName).Contains(normalizedQuery, StringComparison.Ordinal) ||
                TextNormalizer.NormalizeForCompactMatching(app.Id).Contains(normalizedQuery, StringComparison.Ordinal) ||
                (app.Description is not null && TextNormalizer.NormalizeForCompactMatching(app.Description).Contains(normalizedQuery, StringComparison.Ordinal)) ||
                app.Tags.Any(tag => TextNormalizer.NormalizeForCompactMatching(tag).Contains(normalizedQuery, StringComparison.Ordinal)))
            .ToList();
    }

}
