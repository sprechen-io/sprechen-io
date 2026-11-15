using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class ThemeModeTests
{
    [Theory]
    [InlineData(ThemeMode.System, "system")]
    [InlineData(ThemeMode.Light, "light")]
    [InlineData(ThemeMode.Dark, "dark")]
    public void ToStorageValue_MapsEachModeToItsBridgeString(ThemeMode mode, string expected)
        => Assert.Equal(expected, mode.ToStorageValue());

    [Theory]
    [InlineData("light", ThemeMode.Light)]
    [InlineData("dark", ThemeMode.Dark)]
    [InlineData("system", ThemeMode.System)]
    [InlineData("", ThemeMode.System)]
    [InlineData(null, ThemeMode.System)]
    [InlineData("LIGHT", ThemeMode.System)]
    [InlineData("bogus", ThemeMode.System)]
    public void ToThemeMode_NormalizesUnknownOrAbsentToSystem(string? storedValue, ThemeMode expected)
        => Assert.Equal(expected, storedValue.ToThemeMode());
}
