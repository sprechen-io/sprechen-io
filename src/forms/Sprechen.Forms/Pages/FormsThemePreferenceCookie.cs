using Microsoft.AspNetCore.Http;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Liest die gespeicherte Farbschema-Auswahl aus dem benannten Cookie.
/// Die Normalisierung übernimmt FormsThemePreference.
/// </summary>
internal static class FormsThemePreferenceCookie
{
    public const string Name = "SprechenTheme";

    public static string? Read(HttpRequest? request)
        => request is not null && request.Cookies.TryGetValue(Name, out var value)
            ? value
            : null;
}
