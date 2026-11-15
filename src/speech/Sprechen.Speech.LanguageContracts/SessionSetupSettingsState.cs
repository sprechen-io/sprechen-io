namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Bündelt die Sprach- und Höflichkeitsauswahl zur Vorbereitung einer Sitzung.
/// </summary>
public sealed record SessionSetupSettingsState(
    string SelectedIsoCode,
    string SelectedPolitenessValue,
    bool HasPolitenessProfile);
