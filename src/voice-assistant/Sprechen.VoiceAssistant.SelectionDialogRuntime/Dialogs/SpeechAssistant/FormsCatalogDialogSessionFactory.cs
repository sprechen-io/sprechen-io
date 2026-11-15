using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Erstellt die Katalogsitzung mit der ausgewählten Anrede und den Anwendungen,
/// die die Sitzungssprache unterstützen. Die sortierte Kandidatenliste bildet
/// die gemeinsame Grundlage für Auswahl und Ansagen.
/// </summary>
internal sealed class FormsCatalogDialogSessionFactory
{
    private readonly IUserLanguageState _userLanguageState;

    public FormsCatalogDialogSessionFactory(IUserLanguageState userLanguageState)
    {
        _userLanguageState = userLanguageState ?? throw new ArgumentNullException(nameof(userLanguageState));
    }

    public FormsCatalogDialogSession Create(FormsCatalog catalog, string normalizedIsoCode)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedIsoCode);

        var settings = VoiceDialogSettingsRuntimeOverrides.ApplySelectedPoliteness(
            catalog.VoiceDialogSettings ?? new VoiceDialogSettings(),
            _userLanguageState);
        var politenessLevel = settings.GetPolitenessLevel();
        var interactionSettings = new SelectionDialogInteractionSettings(
            settings.GetSpeechRate(),
            settings.GetPauseBetweenItemsMs(),
            settings.GetAllowBargeIn(),
            settings.GetSttMinConfidenceToAccept(),
            settings.GetSttMinConfidenceToConfirm());
        var promptContext = CatalogPromptContext.CreateForCatalog(catalog);
        var promptRuntime = new FormsCatalogDialogPromptRuntime(
            promptContext,
            interactionSettings,
            politenessLevel,
            normalizedIsoCode,
            settings.GetNoInputTimeoutMs(),
            settings.GetMaxRetries());

        return new FormsCatalogDialogSession(
            catalog,
            CreateApplications(catalog, normalizedIsoCode, politenessLevel),
            promptContext,
            promptRuntime,
            settings.GetConfirmationPolicy());
    }

    private static List<ApplicationInfo> CreateApplications(
        FormsCatalog catalog,
        string normalizedIsoCode,
        PolitenessLevel politenessLevel)
        => catalog.Applications
            .Where(application => new FormsCatalogLanguageIndex().IsLanguageSupported(application, normalizedIsoCode))
            .Select(application => FormsCatalogApplicationInfoFactory.Create(application, normalizedIsoCode, politenessLevel))
            .OrderBy(application => application.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(application => application.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
