using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Hält Sprache und Höflichkeitsstufe für laufende Dialoge bereit und informiert Beobachter über Sprachänderungen.
/// </summary>
public interface IUserLanguageState : IUserLanguagePreferences
{
    event Action<string>? SelectedLanguageChanged;

    string? SelectedIsoCode { get; }

    bool HasSelection { get; }

    void SetSelectedIsoCode(string isoCode);

    void SetSelectedPolitenessLevel(PolitenessLevel? politenessLevel);
}
