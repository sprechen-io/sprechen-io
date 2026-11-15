using Microsoft.AspNetCore.Http;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Baut die Endpunkt-Antwort: Sprache auflösen, Anwendung wählen, deren Formulare listen. Ohne Treffer bleibt die Liste leer.
/// </summary>
internal sealed class IndexPageFormsEndpointProjection
{
    private readonly IndexPageApplicationOptionsResolver _applicationOptionsResolver;
    private readonly IndexPageFormOptionResolver _formOptionResolver;
    private readonly FormsCatalogLanguageIndex _languageIndex = new();
    private readonly FormsPageLanguageResolver _languageResolver;
    private readonly IndexPageSelectedApplicationResolver _selectedApplicationResolver;

    public IndexPageFormsEndpointProjection(
        FormsPageLanguageResolver languageResolver,
        IndexPageApplicationOptionsResolver applicationOptionsResolver,
        IndexPageSelectedApplicationResolver selectedApplicationResolver,
        IndexPageFormOptionResolver formOptionResolver)
    {
        ArgumentNullException.ThrowIfNull(languageResolver);
        ArgumentNullException.ThrowIfNull(applicationOptionsResolver);
        ArgumentNullException.ThrowIfNull(selectedApplicationResolver);
        ArgumentNullException.ThrowIfNull(formOptionResolver);

        _languageResolver = languageResolver;
        _applicationOptionsResolver = applicationOptionsResolver;
        _selectedApplicationResolver = selectedApplicationResolver;
        _formOptionResolver = formOptionResolver;
    }

    public IndexPageFormsEndpointResponse Project(
        FormsCatalog catalog,
        HttpRequest request,
        string? applicationId,
        string? language)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var selectedApplicationId = Normalize(applicationId);
        var languages = _languageIndex.GetSupportedLanguages(catalog);
        var cookieLanguage = FormsDisplayLanguageCookie.Read(request);
        var browserLanguage = _languageResolver.ResolvePreferredBrowserLanguage(request, languages);
        var resolvedLanguage = _languageResolver.ResolveLanguage(language, cookieLanguage, browserLanguage, languages);

        if (selectedApplicationId.Length == 0)
        {
            return EmptyResponse(string.Empty, resolvedLanguage);
        }

        var applications = _applicationOptionsResolver.Resolve(catalog, resolvedLanguage);
        var applicationSelection = _selectedApplicationResolver.Resolve(catalog, selectedApplicationId, applications);
        if (applicationSelection.SelectedApplication is null)
        {
            return EmptyResponse(selectedApplicationId, resolvedLanguage);
        }

        return new IndexPageFormsEndpointResponse
        {
            ApplicationId = selectedApplicationId,
            Language = resolvedLanguage,
            Forms = _formOptionResolver.Resolve(applicationSelection.SelectedApplication, resolvedLanguage)
                .Select(form => new IndexPageFormsEndpointForm
                {
                    FormId = form.Id,
                    DisplayName = form.DisplayName
                })
                .ToArray()
        };
    }

    private IndexPageFormsEndpointResponse EmptyResponse(string applicationId, string language)
    {
        return new IndexPageFormsEndpointResponse
        {
            ApplicationId = applicationId,
            Language = language
        };
    }

    private string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
