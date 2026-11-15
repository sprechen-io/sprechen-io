using Microsoft.AspNetCore.Http;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Liest die gespeicherte Anzeigesprache aus dem Cookie. Der Sprachresolver
/// berücksichtigt sie nach dem lang-Parameter und vor der Browsersprache.
/// </summary>
internal static class FormsDisplayLanguageCookie
{
    public const string Name = "SprechenLanguage";

    public static string? Read(HttpRequest? request)
        => request is not null && request.Cookies.TryGetValue(Name, out var value)
            ? value
            : null;
}
