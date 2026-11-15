using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Stellt die ausgewählte Höflichkeitsstufe zur Anpassung von Dialogeinstellungen und Ansagen bereit.
/// </summary>
public interface IUserLanguagePreferences
{
    PolitenessLevel? SelectedPolitenessLevel { get; }
}

