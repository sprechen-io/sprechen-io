using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Erstellt den Kontext einer Formularauswahl aus Anwendung, Katalog und Benutzervorgaben.
/// Geordnete Formulare und bekannte Vorgängernamen stehen damit für Ansagen und
/// Voraussetzungskontrolle gemeinsam bereit.
/// </summary>
internal sealed class FormSelectionDialogSessionFactory
{
    private readonly IUserLanguageState _userLanguageState;

    public FormSelectionDialogSessionFactory(IUserLanguageState userLanguageState)
    {
        _userLanguageState = userLanguageState ?? throw new ArgumentNullException(nameof(userLanguageState));
    }

    public FormSelectionDialogSession Create(
        FormsCatalog catalog,
        string applicationId,
        string normalizedIsoCode,
        IReadOnlyCollection<string> completedFormIds)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedIsoCode);
        ArgumentNullException.ThrowIfNull(completedFormIds);

        var application = catalog.Applications.FirstOrDefault(a =>
            string.Equals(a.ApplicationId, applicationId, StringComparison.OrdinalIgnoreCase));

        if (application is null || application.Forms.Count == 0)
        {
            throw new InvalidOperationException(
                $"Application '{applicationId}' was not found in the forms catalog or does not expose any forms.");
        }

        var promptContext = CatalogPromptContext.CreateForApplication(catalog, application);
        var settings = CreateRuntimeSettings(catalog, application);
        var politenessLevel = settings.GetPolitenessLevel();
        var interactionSettings = new SelectionDialogInteractionSettings(
            settings.GetSpeechRate(),
            settings.GetPauseBetweenItemsMs(),
            settings.GetAllowBargeIn(),
            settings.GetSttMinConfidenceToAccept(),
            settings.GetSttMinConfidenceToConfirm());
        var forms = CreateForms(application, normalizedIsoCode, politenessLevel);
        var transitionContext = CreateTransitionContext(
            settings.GetConfirmationPolicy(),
            completedFormIds,
            forms,
            promptContext,
            politenessLevel,
            normalizedIsoCode);

        return new FormSelectionDialogSession(
            catalog,
            forms,
            promptContext,
            interactionSettings,
            politenessLevel,
            normalizedIsoCode,
            settings.GetNoInputTimeoutMs(),
            settings.GetMaxRetries(),
            transitionContext);
    }

    private VoiceDialogSettings CreateRuntimeSettings(FormsCatalog catalog, ApplicationDescriptor application)
        => VoiceDialogSettingsRuntimeOverrides.ApplySelectedPoliteness(
            application.VoiceDialogSettings?.MergeWith(catalog.VoiceDialogSettings)
            ?? catalog.VoiceDialogSettings
            ?? new VoiceDialogSettings(),
            _userLanguageState);

    private static List<FormInfo> CreateForms(
        ApplicationDescriptor application,
        string normalizedIsoCode,
        PolitenessLevel politenessLevel)
    {
        var textResolver = new FormsCatalogTextResolver();
        return new FormsCatalogFormOrder().OrderForms(application)
            .Select(form =>
                new FormInfo(
                    form.FormId,
                    textResolver.ResolveFormDisplayName(application, form, normalizedIsoCode, politenessLevel),
                    form.PrerequisiteForms ?? []))
            .ToList();
    }

    private static FormSelectionTransitionContext CreateTransitionContext(
        ConfirmationPolicy confirmationPolicy,
        IReadOnlyCollection<string> completedFormIds,
        IEnumerable<FormInfo> forms,
        CatalogPromptContext promptContext,
        PolitenessLevel politenessLevel,
        string normalizedIsoCode)
    {
        var completedFormIdsSet = new HashSet<string>(completedFormIds, StringComparer.OrdinalIgnoreCase);
        var formNamesById = forms.ToDictionary(
            form => form.Id,
            form => form.DisplayName,
            StringComparer.OrdinalIgnoreCase);

        return new FormSelectionTransitionContext(
            confirmationPolicy,
            completedFormIdsSet,
            formNamesById,
            promptContext,
            politenessLevel,
            normalizedIsoCode);
    }
}
