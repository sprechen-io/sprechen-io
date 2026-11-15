using System.Text.Json;

namespace Sprechen.Contracts.Tests;

/// <summary>
/// Liest die Wahrheitstabellen des Modells, die dieses Testprojekt als Ressource einbindet.
/// Die Tabellen sind unabhängige Prüforakel für die Laufzeit und werden nicht ausgeliefert;
/// die Tests lesen sie ohne Zugriff auf das Arbeitsverzeichnis.
/// </summary>
internal static class ReferenceTableResources
{
    public static JsonDocument Read(string fileName)
    {
        using var stream = typeof(ReferenceTableResources).Assembly.GetManifestResourceStream("ReferenceTables." + fileName)
            ?? throw new InvalidOperationException($"Die Wahrheitstabelle '{fileName}' ist nicht als Ressource eingebunden.");
        return JsonDocument.Parse(stream);
    }
}
