namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Bildet <see cref="ThemeMode"/> auf <c>system</c>, <c>light</c> oder <c>dark</c>
/// für den Browserspeicher ab. Fehlende oder unbekannte Speicherwerte ergeben <see cref="ThemeMode.System"/>.
/// </summary>
public static class ThemeModeExtensions
{
    private const string SystemStorageValue = "system";
    private const string LightStorageValue = "light";
    private const string DarkStorageValue = "dark";

    public static string ToStorageValue(this ThemeMode mode)
        => mode switch
        {
            ThemeMode.Light => LightStorageValue,
            ThemeMode.Dark => DarkStorageValue,
            _ => SystemStorageValue,
        };

    public static ThemeMode ToThemeMode(this string? storageValue)
        => storageValue switch
        {
            LightStorageValue => ThemeMode.Light,
            DarkStorageValue => ThemeMode.Dark,
            _ => ThemeMode.System,
        };
}
