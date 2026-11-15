namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Liest und speichert den gewählten <see cref="ThemeMode"/> über den Browserzugang
/// <c>themeSettings</c>. Ist kein gespeicherter Wert verfügbar, gilt <see cref="ThemeMode.System"/>.
/// </summary>
public interface IThemeSettings
{
    ValueTask<ThemeMode> GetAsync(CancellationToken cancellationToken);

    ValueTask SetAsync(ThemeMode mode, CancellationToken cancellationToken);
}
