using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Koordiniert Sprach- und Höflichkeitswahl und merkt beantwortete Einstiegsentscheidungen für spätere Sitzungen.
/// </summary>
public interface IUserLanguageSettingsWorkflow
{
    ValueTask<UserLanguageSettings> SaveSelectedLanguageAsync(
        string? selectedIsoCode,
        CancellationToken cancellationToken);

    ValueTask<UserLanguageSettings> SaveSelectedPolitenessAsync(
        PolitenessLevel? selectedPolitenessLevel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Merkt die Sprache einer beantworteten Spracheinstiegsentscheidung.
    /// </summary>
    /// <remarks>
    /// Der Aufrufer schreibt nach Zustimmung zur bisherigen Sprache oder nach akzeptiertem Wechsel.
    /// Eine vollständig unbeantwortete Sprachauswahl setzt diesen Marker nicht.
    /// </remarks>
    ValueTask SaveEntryOfferHeardAsync(string isoCode, CancellationToken cancellationToken);

    /// <summary>
    /// Liest die gemerkte Sprache der beantworteten Einstiegsentscheidung oder null.
    /// </summary>
    ValueTask<string?> ReadEntryOfferHeardIsoCodeAsync(CancellationToken cancellationToken);
}
