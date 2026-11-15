using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Hält Sprachwahl, Höflichkeitsstufe und Einstiegsmarker zur Wiederverwendung persönlicher Einstellungen beim Sitzungsstart zusammen.
/// </summary>
/// <param name="EntryOfferHeardIsoCode">
/// Sprache der bereits beantworteten Einstiegsentscheidung. Ohne gemerkte Antwort noch leer.
/// </param>
/// <remarks>
/// Der Marker bleibt von der gespeicherten Sprachwahl unabhängig. Der Einstiegsablauf setzt ihn
/// nach Zustimmung zur bisherigen Sprache oder einem akzeptierten Wechsel, nicht bei durchgängiger
/// Stille. Die Sprachkennung ermöglicht die getrennte Beurteilung eines Einstiegs in anderer Sprache.
/// </remarks>
public sealed record UserLanguageSettings(
    string? SelectedIsoCode,
    PolitenessLevel? SelectedPolitenessLevel,
    string? EntryOfferHeardIsoCode = null);
