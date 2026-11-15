namespace Sprechen.Forms.Pages.Models;

/// <summary>
/// Ein Eintrag der Anwendungsliste auf der Index-Seite: Katalog-ID und sprachaufgelöster Anzeigename.
/// </summary>
public sealed record ApplicationOption(string Id, string DisplayName);

