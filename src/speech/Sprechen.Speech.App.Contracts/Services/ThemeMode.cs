namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Erscheinungsbild der Web-App. <see cref="System"/> folgt der Betriebssystemvorgabe
/// über <c>prefers-color-scheme</c>. <see cref="Light"/> und <see cref="Dark"/> wählen es ausdrücklich.
/// </summary>
public enum ThemeMode
{
    System,
    Light,
    Dark
}
