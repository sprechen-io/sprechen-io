

using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Liefert die Oberflächentexte der Forms-App in der Anzeigesprache
/// aus dem gemeinsamen Katalog.
/// </summary>
internal static class FormsChromeCopy
{
    private static readonly Lazy<IReadOnlyDictionary<string, ChromeCopyText>> CatalogEntries = new(BuildEntries);

    internal static IReadOnlyDictionary<string, ChromeCopyText> Entries => CatalogEntries.Value;

    internal static string Select(string copyId, string? language)
        => Entries[copyId].For(language);

    private static IReadOnlyDictionary<string, ChromeCopyText> BuildEntries()
    {
        var entries = new Dictionary<string, ChromeCopyText>(StringComparer.Ordinal);
        var languages = FormsChromeCopyLoader.Catalog.SupportedLanguages;
        foreach (var entry in FormsChromeCopyLoader.Catalog.Entries)
        {
            entries.Add(entry.Id, ToChromeCopyText(entry, languages));
        }

        return entries;
    }

    // Die Positionsargumente folgen der Sprachreihenfolge des Katalogs.
    // FormsChromeCopyLoader prüft, dass genau fünf Sprachen vorliegen.
    private static ChromeCopyText ToChromeCopyText(FormsChromeCopyEntry entry, IReadOnlyList<string> languages)
    {
        string Text(int index) => entry.Texts[languages[index]];
        return new ChromeCopyText(Text(0), Text(1), Text(2), Text(3), Text(4));
    }
}
