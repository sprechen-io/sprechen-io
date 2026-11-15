namespace Sprechen.Forms.Pages.Models;

/// <summary>
/// Ein Eintrag der Formularliste: ID, sprachaufgelöster Anzeigename, neueste Vertragsversion und Katalogreihenfolge.
/// </summary>
public sealed record FormOption(string Id, string DisplayName, string ContractVersion, int? Sequence);

