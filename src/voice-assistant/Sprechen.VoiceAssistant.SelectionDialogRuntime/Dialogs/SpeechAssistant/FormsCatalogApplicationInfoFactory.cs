using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Projiziert eine Anwendung mit lokalisiertem Namen, Beschreibung, Formularzahl und
/// Standardformular für die Auswahl. Der Dialog erhält damit seine benötigten Kandidatendaten aus
/// einer gemeinsamen Modellableitung.
/// </summary>
internal static class FormsCatalogApplicationInfoFactory
{
    public static ApplicationInfo Create(
        ApplicationDescriptor application,
        string requestedLanguage,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedLanguage);

        var applicationId = string.IsNullOrWhiteSpace(application.ApplicationId)
            ? string.Empty
            : application.ApplicationId.Trim();

        var displayName = new FormsCatalogTextResolver().ResolveApplicationDisplayName(
            application,
            requestedLanguage,
            politenessLevel);
        var description = new FormsCatalogTextResolver().ResolveApplicationDescription(
            application,
            requestedLanguage,
            politenessLevel);
        var defaultFormId = new FormsCatalogFormOrder().GetDefaultFormId(application);

        var tags = application.Tags
            ?.Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? [];

        return new ApplicationInfo(
            applicationId,
            displayName,
            description,
            tags,
            application.Forms.Count,
            defaultFormId);
    }
}
