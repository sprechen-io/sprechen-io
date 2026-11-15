using Sprechen.Forms.Pages.Models;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Ermittelt aus Katalog und Anfrage Sprache, Optionslisten und aktuelle Auswahl der Index-Seite samt Navigationsmanifest.
/// </summary>
internal sealed class IndexPageSelectionResolver
{
    private readonly IndexPageApplicationOptionsResolver _applicationOptionsResolver;
    private readonly IndexPageFormOptionResolver _formOptionResolver;
    private readonly FormsCatalogLanguageIndex _languageIndex = new();
    private readonly FormsPageLanguageResolver _languageResolver;
    private readonly FormsNavigationManifestJsonBuilder _navigationManifestJsonBuilder;
    private readonly IndexPageSelectedApplicationResolver _selectedApplicationResolver;
    private readonly IndexPageSelectedFormResolver _selectedFormResolver;

    public IndexPageSelectionResolver(
        FormsPageLanguageResolver languageResolver,
        IndexPageApplicationOptionsResolver applicationOptionsResolver,
        IndexPageSelectedApplicationResolver selectedApplicationResolver,
        IndexPageFormOptionResolver formOptionResolver,
        IndexPageSelectedFormResolver selectedFormResolver,
        FormsNavigationManifestJsonBuilder navigationManifestJsonBuilder)
    {
        ArgumentNullException.ThrowIfNull(languageResolver);
        ArgumentNullException.ThrowIfNull(applicationOptionsResolver);
        ArgumentNullException.ThrowIfNull(selectedApplicationResolver);
        ArgumentNullException.ThrowIfNull(formOptionResolver);
        ArgumentNullException.ThrowIfNull(selectedFormResolver);
        ArgumentNullException.ThrowIfNull(navigationManifestJsonBuilder);

        _languageResolver = languageResolver;
        _applicationOptionsResolver = applicationOptionsResolver;
        _selectedApplicationResolver = selectedApplicationResolver;
        _formOptionResolver = formOptionResolver;
        _selectedFormResolver = selectedFormResolver;
        _navigationManifestJsonBuilder = navigationManifestJsonBuilder;
    }

    public IndexPageSelectionResult Resolve(FormsCatalog catalog, IndexPageSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(request);

        var languages = _languageIndex.GetSupportedLanguages(catalog);
        var cookieLanguage = FormsDisplayLanguageCookie.Read(request.Request);
        var browserLanguage = _languageResolver.ResolvePreferredBrowserLanguage(request.Request, languages);
        var selectedLanguage = _languageResolver.ResolveLanguage(request.Language, cookieLanguage, browserLanguage, languages);
        var applications = _applicationOptionsResolver.Resolve(catalog, selectedLanguage);
        var applicationSelection = _selectedApplicationResolver.Resolve(catalog, request.ApplicationId, applications);

        if (applicationSelection.SelectedApplication is null)
        {
            return WithoutFormSelection(languages, selectedLanguage, applications, string.Empty);
        }

        return ResolveFormSelection(request, languages, selectedLanguage, applications, applicationSelection.SelectedApplication);
    }

    private IndexPageSelectionResult ResolveFormSelection(
        IndexPageSelectionRequest request,
        IReadOnlyList<string> languages,
        string selectedLanguage,
        IReadOnlyList<ApplicationOption> applications,
        ApplicationDescriptor selectedApplication)
    {
        var forms = _formOptionResolver.Resolve(selectedApplication, selectedLanguage);
        var navigationManifestJson = _navigationManifestJsonBuilder.Build(selectedApplication, forms);
        var formSelection = _selectedFormResolver.Resolve(request, selectedApplication.ApplicationId, forms);

        return new IndexPageSelectionResult
        {
            Languages = languages,
            SelectedLanguage = selectedLanguage,
            Applications = applications,
            SelectedApplicationId = selectedApplication.ApplicationId,
            SelectedApplication = selectedApplication,
            Forms = forms,
            SelectedFormId = formSelection.SelectedFormId,
            SelectedForm = formSelection.SelectedForm,
            NavigationManifestJson = navigationManifestJson,
            CurrentFormIndex = formSelection.CurrentFormIndex,
            TotalFormsCount = forms.Count
        };
    }

    private IndexPageSelectionResult WithoutFormSelection(
        IReadOnlyList<string> languages,
        string selectedLanguage,
        IReadOnlyList<ApplicationOption> applications,
        string selectedApplicationId)
    {
        return new IndexPageSelectionResult
        {
            Languages = languages,
            SelectedLanguage = selectedLanguage,
            Applications = applications,
            SelectedApplicationId = selectedApplicationId
        };
    }
}
