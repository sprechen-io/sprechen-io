using Sprechen.Service.Abstractions;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Führt aktivierte Befehls-, Aktivierungs- und Bestätigungsbegriffe für Gesprächsanfragen
/// in dieser Reihenfolge zusammen und entfernt Duplikate.
/// </summary>
internal static class ConversationalKeytermSet
{
    public static IReadOnlyList<string> Compose(
        DeepgramOptions options,
        CommandKeytermVocabulary commandKeyterms,
        WakeWordKeytermVocabulary wakeWordKeyterms,
        ConfirmationKeytermVocabulary confirmationKeyterms,
        string? languageCode)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(commandKeyterms);
        ArgumentNullException.ThrowIfNull(wakeWordKeyterms);
        ArgumentNullException.ThrowIfNull(confirmationKeyterms);

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        if (options.EnableCommandKeyterms)
        {
            AddDistinct(result, seen, commandKeyterms.GetKeyterms(languageCode));
        }

        if (options.EnableWakeWordKeyterms)
        {
            AddDistinct(result, seen, wakeWordKeyterms.GetKeyterms(languageCode));
        }

        if (options.EnableConfirmationKeyterms)
        {
            AddDistinct(result, seen, confirmationKeyterms.GetKeyterms(languageCode));
        }

        return result;
    }

    /// <summary>
    /// Kombiniert Servervokabular und Anfragebegriffe entsprechend dem angegebenen Modus.
    /// Bei <c>keyterms == null</c> bleibt die Basis unverändert. WithoutServerBase verwendet nur
    /// die Anfragebegriffe, einschließlich einer ausdrücklich leeren Liste.
    /// </summary>
    public static IReadOnlyList<string> Compose(
        IReadOnlyList<string> serverBase,
        SpeechKeyterms? keyterms)
    {
        ArgumentNullException.ThrowIfNull(serverBase);

        if (keyterms is null)
        {
            return serverBase;
        }

        if (keyterms.Composition == KeytermComposition.WithoutServerBase)
        {
            return Union([], keyterms.Terms);
        }

        return Union(serverBase, keyterms.Terms);
    }

    /// <summary>
    /// Führt zusätzliche Begriffe mit ordinalem Vergleich, ohne Duplikate und unter Erhalt der
    /// Reihenfolge zusammen. Dabei entfallen Nullwerte und leere Einträge. Bei fehlender oder
    /// leerer Zusatzliste bleibt die Basis unverändert.
    /// </summary>
    public static IReadOnlyList<string> Union(
        IReadOnlyList<string> baseKeyterms,
        IReadOnlyList<string>? keyterms)
    {
        ArgumentNullException.ThrowIfNull(baseKeyterms);

        if (keyterms is null || keyterms.Count == 0)
        {
            return baseKeyterms;
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        AddDistinct(result, seen, baseKeyterms);
        AddDistinct(result, seen, keyterms);
        return result;
    }

    private static void AddDistinct(List<string> result, HashSet<string> seen, IReadOnlyList<string> items)
    {
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item) && seen.Add(item))
            {
                result.Add(item);
            }
        }
    }
}
