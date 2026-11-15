using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Verarbeitet die Einstellungsseite und speichert die gewählte Sprache
/// und das Farbschema in Cookies.
/// </summary>
internal sealed class SettingsModel : PageModel
{
    private readonly FormsCatalogLoader _catalogLoader;
    private readonly FormsPageLanguageResolver _languageResolver;
    private readonly FormsCatalogLanguageIndex _languageIndex = new();

    public SettingsModel(FormsCatalogLoader catalogLoader, FormsPageLanguageResolver languageResolver)
    {
        _catalogLoader = catalogLoader ?? throw new ArgumentNullException(nameof(catalogLoader));
        _languageResolver = languageResolver ?? throw new ArgumentNullException(nameof(languageResolver));
    }

    [BindProperty(SupportsGet = true, Name = "lang")]
    public string? Language { get; init; }

    public IReadOnlyList<string> SupportedLanguages { get; private set; } = Array.Empty<string>();

    public string SelectedLanguage { get; private set; } = "de";

    public IReadOnlyList<string> SupportedThemes { get; } = FormsThemePreference.SupportedValues;

    public string SelectedTheme { get; private set; } = FormsThemePreference.System;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var languages = await LoadSupportedLanguagesAsync(cancellationToken).ConfigureAwait(false);

        SupportedLanguages = languages;
        SelectedLanguage = ResolveDisplayLanguage(languages);
        SelectedTheme = FormsThemePreference.From(FormsThemePreferenceCookie.Read(Request)).Value;
    }

    public async Task<IActionResult> OnPostAsync(string? language, string? theme, CancellationToken cancellationToken)
    {
        var languages = await LoadSupportedLanguagesAsync(cancellationToken).ConfigureAwait(false);

        if (IsSupported(language, languages))
        {
            Response.Cookies.Append(
                FormsDisplayLanguageCookie.Name,
                language!.Trim(),
                PreferenceCookieOptions());
        }

        if (FormsThemePreference.IsSupported(theme))
        {
            Response.Cookies.Append(
                FormsThemePreferenceCookie.Name,
                FormsThemePreference.From(theme).Value,
                PreferenceCookieOptions());
        }

        return RedirectToPage("Index");
    }

    private static CookieOptions PreferenceCookieOptions() => new()
    {
        SameSite = SameSiteMode.Lax,
        MaxAge = TimeSpan.FromDays(365),
        Secure = false,
        HttpOnly = false
    };

    /// <summary>
    /// Liefert zum Sprachcode den Eigennamen über die gemeinsame
    /// <see cref="LanguageEndonyms"/>-Projektion. Fehlt die Abbildung, bleibt der
    /// Rohcode stehen, damit eine unerwartete Katalogsprache trotzdem erscheint.
    /// </summary>
    public static string Endonym(string code) => LanguageEndonyms.For(code);

    private async Task<IReadOnlyList<string>> LoadSupportedLanguagesAsync(CancellationToken cancellationToken)
    {
        var catalogLoadResult = await _catalogLoader.LoadAsync(cancellationToken).ConfigureAwait(false);
        return catalogLoadResult.IsSuccess && catalogLoadResult.Catalog is not null
            ? _languageIndex.GetSupportedLanguages(catalogLoadResult.Catalog)
            : Array.Empty<string>();
    }

    private string ResolveDisplayLanguage(IReadOnlyList<string> languages)
    {
        var cookieLanguage = FormsDisplayLanguageCookie.Read(Request);
        var browserLanguage = _languageResolver.ResolvePreferredBrowserLanguage(Request, languages);
        return _languageResolver.ResolveLanguage(Language, cookieLanguage, browserLanguage, languages);
    }

    private static bool IsSupported(string? language, IReadOnlyList<string> languages)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return false;
        }

        var normalized = language.Trim();
        return languages.Any(code => string.Equals(code, normalized, StringComparison.OrdinalIgnoreCase));
    }
}
