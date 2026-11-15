using Microsoft.AspNetCore.Http;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Liest die Accept-Language-Header der Anfrage und gibt die erste darin genannte verfügbare Sprache zurück, sonst leer.
/// </summary>
internal sealed class FormsPageBrowserLanguageResolver
{
    private readonly FormsPageAcceptedLanguageHeaderResolver _acceptedLanguageHeaderResolver;

    public FormsPageBrowserLanguageResolver(FormsPageAcceptedLanguageHeaderResolver acceptedLanguageHeaderResolver)
    {
        _acceptedLanguageHeaderResolver = acceptedLanguageHeaderResolver
            ?? throw new ArgumentNullException(nameof(acceptedLanguageHeaderResolver));
    }

    public string ResolvePreferredBrowserLanguage(HttpRequest? request, IReadOnlyList<string> availableLanguages)
    {
        if (request is null || !request.Headers.TryGetValue("Accept-Language", out var acceptedLanguages))
        {
            return string.Empty;
        }

        foreach (var acceptedLanguageHeader in acceptedLanguages)
        {
            var resolved = _acceptedLanguageHeaderResolver.Resolve(acceptedLanguageHeader, availableLanguages);
            if (resolved.Length > 0)
            {
                return resolved;
            }
        }

        return string.Empty;
    }
}
