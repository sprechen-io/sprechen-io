using System.Text.Json;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;

namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Lädt und prüft den gemeinsamen Katalog für Bedienelementtexte der Formularoberfläche:
/// Sprachanzahl, unterstützte Standardsprache, eindeutige Kennungen und vollständige Texte.
/// Der Lader liegt bei den Verträgen, damit der Generator für Navigationstexte die Formularanwendung
/// nicht bauen muss, deren Build wiederum seine erzeugten Dateien benötigt.
/// </summary>
public static class FormsChromeCopyLoader
{
    public const string ArtifactRelativePath =
        "navigationsmodell/reference-contracts/forms-chrome/forms-chrome-copy.reference.json";

    private static readonly Lazy<FormsChromeCopyCatalog> SharedCatalog = new(LoadCatalog);

    public static FormsChromeCopyCatalog Catalog => SharedCatalog.Value;

    public static Stream OpenCatalogStream()
        => new MemoryStream(MdaEmbeddedRuntimeArtifacts.ReadAllBytes(ArtifactRelativePath), writable: false);

    private static FormsChromeCopyCatalog LoadCatalog()
    {
        using var stream = OpenCatalogStream();
        var catalog = JsonSerializer.Deserialize<FormsChromeCopyCatalog>(stream)
            ?? throw new InvalidOperationException($"Chrome copy catalog '{ArtifactRelativePath}' could not be deserialized.");

        Validate(catalog);
        return catalog;
    }

    public static void Validate(FormsChromeCopyCatalog catalog)
    {
        // ChromeCopyText hat fünf Sprachpositionen. Hier wird die Anzahl der
        // deklarierten Sprachen geprüft, nicht deren Eindeutigkeit oder konkrete Reihenfolge.
        if (catalog.SupportedLanguages.Count != 5)
        {
            throw new InvalidOperationException(
                $"The chrome copy catalog must declare exactly five supported languages, found {catalog.SupportedLanguages.Count}.");
        }

        if (!catalog.SupportedLanguages.Contains(catalog.DefaultLanguage, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The chrome copy catalog's default language '{catalog.DefaultLanguage}' is not in its supported languages.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in catalog.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id))
            {
                throw new InvalidOperationException("The chrome copy catalog contains an entry without an id.");
            }

            if (!ids.Add(entry.Id))
            {
                throw new InvalidOperationException($"Duplicate chrome copy id '{entry.Id}' in the catalog.");
            }

            foreach (var language in catalog.SupportedLanguages)
            {
                if (!entry.Texts.TryGetValue(language, out var text) || string.IsNullOrWhiteSpace(text))
                {
                    throw new InvalidOperationException(
                        $"Chrome copy entry '{entry.Id}' is missing a text for language '{language}'.");
                }
            }
        }
    }
}
